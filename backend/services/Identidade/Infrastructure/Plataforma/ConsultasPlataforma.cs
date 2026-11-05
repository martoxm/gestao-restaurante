using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using GestaoRestaurante.Core.Application.Models;
using GestaoRestaurante.Core.Seguranca;
using GestaoRestaurante.Identidade.Application.Abstractions.Services;
using GestaoRestaurante.Identidade.Contracts.Convites;
using GestaoRestaurante.Identidade.Contracts.Plataforma;
using GestaoRestaurante.Identidade.Domain.Entities;
using GestaoRestaurante.Identidade.Domain.Enums;
using GestaoRestaurante.Identidade.Infrastructure.Persistence;

namespace GestaoRestaurante.Identidade.Infrastructure.Plataforma;

public class PlanosSettings
{
    public const string SectionName = "Planos";

    public decimal PrecoBasico { get; set; } = 99m;
    public decimal PrecoPro { get; set; } = 199m;
}

public class ConsultasPlataforma : IConsultasPlataforma
{
    private readonly AppDbContext _context;
    private readonly PlanosSettings _planos;

    public ConsultasPlataforma(AppDbContext context, IOptions<PlanosSettings> planos)
    {
        _context = context;
        _planos = planos.Value;
    }

    private IQueryable<Restaurante> Restaurantes => _context.Restaurantes.IgnoreQueryFilters();

    public async Task<ResumoPlataformaDto> ObterResumoAsync(DateTime agora, CancellationToken cancellationToken)
    {
        var avisoAte = agora.AddDays(Restaurante.DiasAvisoVencimento);

        var r = await Restaurantes
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Ativos = g.Count(x => x.Ativo),
                PlanoTeste = g.Count(x => x.Plano == PlanoRestaurante.Teste),
                PlanoBasico = g.Count(x => x.Plano == PlanoRestaurante.Basico),
                PlanoPro = g.Count(x => x.Plano == PlanoRestaurante.Pro),
                BasicoAtivos = g.Count(x => x.Ativo && x.Plano == PlanoRestaurante.Basico),
                ProAtivos = g.Count(x => x.Ativo && x.Plano == PlanoRestaurante.Pro),
                EmTeste = g.Count(x => x.Ativo && x.Plano == PlanoRestaurante.Teste && x.TesteTerminaEm > agora),
                TesteVencido = g.Count(x => x.Ativo && x.Plano == PlanoRestaurante.Teste && x.TesteTerminaEm <= agora),
                Vencendo = g.Count(x => x.Ativo && x.Plano == PlanoRestaurante.Teste && x.TesteTerminaEm > agora && x.TesteTerminaEm <= avisoAte),
                VencidosSemConverter = g.Count(x => x.Plano == PlanoRestaurante.Teste && x.TesteTerminaEm <= agora)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var usuarios = _context.Users.Where(u => u.RestauranteId != null);
        var totalUsuarios = await usuarios.CountAsync(cancellationToken);
        var desativados = await usuarios.CountAsync(u => u.LockoutEnd != null && u.LockoutEnd > agora, cancellationToken);
        var admins = await IdsDaRole(RoleNames.Admin)
            .CountAsync(id => usuarios.Any(u => u.Id == id), cancellationToken);

        var convites = _context.Convites.IgnoreQueryFilters();
        var pendentes = await convites.CountAsync(c => c.AceitoEm == null && c.CanceladoEm == null && c.ExpiraEm > agora, cancellationToken);
        var aceitos = await convites.CountAsync(c => c.AceitoEm != null, cancellationToken);

        var total = r?.Total ?? 0;
        var ativos = r?.Ativos ?? 0;
        var emTeste = r?.EmTeste ?? 0;
        var pagantes = (r?.BasicoAtivos ?? 0) + (r?.ProAtivos ?? 0);
        var mrr = (r?.BasicoAtivos ?? 0) * _planos.PrecoBasico + (r?.ProAtivos ?? 0) * _planos.PrecoPro;

        return new ResumoPlataformaDto(
            new RestaurantesResumoDto(total, ativos, total - ativos, r?.PlanoTeste ?? 0, r?.PlanoBasico ?? 0, r?.PlanoPro ?? 0,
                emTeste, r?.TesteVencido ?? 0, r?.Vencendo ?? 0),
            new UsuariosResumoDto(totalUsuarios, admins, totalUsuarios - admins, desativados),
            new ConvitesResumoDto(pendentes, aceitos),
            new FunilConversaoDto(total, emTeste, pagantes, r?.VencidosSemConverter ?? 0),
            mrr,
            await CadastrosPorDiaAsync(agora, cancellationToken));
    }

    private async Task<IReadOnlyList<CadastrosDiaDto>> CadastrosPorDiaAsync(DateTime agora, CancellationToken cancellationToken)
    {
        const int dias = 30;
        var inicio = agora.Date.AddDays(-(dias - 1));

        var porDia = await Restaurantes
            .Where(x => x.CriadoEm >= inicio)
            .GroupBy(x => x.CriadoEm.Date)
            .Select(g => new { Dia = g.Key, Quantidade = g.Count() })
            .ToDictionaryAsync(x => x.Dia, x => x.Quantidade, cancellationToken);

        return Enumerable.Range(0, dias)
            .Select(i => inicio.AddDays(i))
            .Select(dia => new CadastrosDiaDto(dia, porDia.GetValueOrDefault(dia)))
            .ToList();
    }

    public async Task<ResultadoPaginado<RestaurantePlataformaItemDto>> ListarRestaurantesAsync(
        ListarRestaurantesPlataformaRequest filtro, DateTime agora, CancellationToken cancellationToken)
    {
        var query = Restaurantes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var busca = filtro.Busca.Trim();
            query = query.Where(x => x.Nome.Contains(busca)
                || _context.Users.Any(u => u.RestauranteId == x.Id && u.Email!.Contains(busca)));
        }

        if (Enum.TryParse<PlanoRestaurante>(filtro.Plano, ignoreCase: true, out var plano))
            query = query.Where(x => x.Plano == plano);

        query = AplicarStatus(query, filtro.Status, agora);

        var totalItens = await query.CountAsync(cancellationToken);

        var pagina = await query
            .OrderByDescending(x => x.CriadoEm)
            .Skip((filtro.Pagina - 1) * filtro.TamanhoPagina)
            .Take(filtro.TamanhoPagina)
            .ToListAsync(cancellationToken);

        var ids = pagina.Select(x => x.Id).ToList();
        var idsAdmins = IdsDaRole(RoleNames.Admin);

        var usuariosDaPagina = await _context.Users
            .Where(u => u.RestauranteId != null && ids.Contains(u.RestauranteId.Value))
            .Select(u => new { RestauranteId = u.RestauranteId!.Value, u.Email, EhAdmin = idsAdmins.Contains(u.Id) })
            .ToListAsync(cancellationToken);

        var itens = pagina.Select(x =>
        {
            var daqui = usuariosDaPagina.Where(u => u.RestauranteId == x.Id).ToList();
            var dono = daqui.Where(u => u.EhAdmin).Select(u => u.Email).OrderBy(e => e).FirstOrDefault();
            return new RestaurantePlataformaItemDto(x.Id, x.Nome, x.Plano.ToString(), x.Ativo, x.ObterSituacao(agora),
                x.TesteTerminaEm, x.CriadoEm, dono, daqui.Count, x.PlanoValidoAte, x.AssinadoEm, x.UltimoPagamentoEm);
        }).ToList();

        return new ResultadoPaginado<RestaurantePlataformaItemDto>(itens, filtro.Pagina, filtro.TamanhoPagina, totalItens);
    }

    private static IQueryable<Restaurante> AplicarStatus(IQueryable<Restaurante> query, string? status, DateTime agora)
    {
        var avisoAte = agora.AddDays(Restaurante.DiasAvisoVencimento);
        var teste = PlanoRestaurante.Teste;

        return status?.Trim().ToLowerInvariant() switch
        {
            "ativo" => query.Where(x => x.Ativo),
            "inativo" => query.Where(x => !x.Ativo),
            "pago" => query.Where(x => x.Ativo && x.Plano != teste),
            "assinante" => query.Where(x => x.AssinadoEm != null),
            "planovencido" => query.Where(x => x.Ativo && x.Plano != teste && x.PlanoValidoAte <= agora),
            "emteste" => query.Where(x => x.Ativo && x.Plano == teste && x.TesteTerminaEm > agora),
            "vencendoembreve" => query.Where(x => x.Ativo && x.Plano == teste && x.TesteTerminaEm > agora && x.TesteTerminaEm <= avisoAte),
            "testevencido" => query.Where(x => x.Ativo && x.Plano == teste && x.TesteTerminaEm <= agora),
            _ => query
        };
    }

    public async Task<RestaurantePlataformaDetalhesDto?> ObterRestauranteAsync(Guid id, DateTime agora, CancellationToken cancellationToken)
    {
        var restaurante = await Restaurantes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (restaurante is null)
            return null;

        var idsAdmins = IdsDaRole(RoleNames.Admin);
        var usuarios = await _context.Users
            .Where(u => u.RestauranteId == id)
            .OrderBy(u => u.Nome)
            .Select(u => new UsuarioPlataformaDto(
                u.Id,
                u.Nome,
                u.Email!,
                idsAdmins.Contains(u.Id) ? RoleNames.Admin : RoleNames.Funcionario,
                u.LockoutEnd == null || u.LockoutEnd <= agora,
                u.EmailConfirmed))
            .ToListAsync(cancellationToken);

        var convites = (await _context.Convites.IgnoreQueryFilters()
                .Where(c => c.RestauranteId == id)
                .OrderByDescending(c => c.CriadoEm)
                .ToListAsync(cancellationToken))
            .Select(c => new ConviteItemListaDto(c.Id, c.Email, c.Role, c.ObterStatus(agora), c.CriadoEm, c.ExpiraEm))
            .ToList();

        return new RestaurantePlataformaDetalhesDto(restaurante.Id, restaurante.Nome, restaurante.Plano.ToString(),
            restaurante.Ativo, restaurante.ObterSituacao(agora), restaurante.TesteTerminaEm, restaurante.CriadoEm,
            restaurante.LimiteUsuarios, restaurante.PlanoValidoAte, restaurante.AssinadoEm, restaurante.UltimoPagamentoEm,
            usuarios, convites);
    }

    public async Task<ResultadoPaginado<AuditoriaItemDto>> ListarAuditoriaAsync(ListarAuditoriaRequest filtro, CancellationToken cancellationToken)
    {
        var query = _context.AuditoriasPlataforma.AsQueryable();

        if (filtro.RestauranteId is Guid restauranteId)
            query = query.Where(a => a.RestauranteId == restauranteId);

        var totalItens = await query.CountAsync(cancellationToken);

        var itens = await query
            .OrderByDescending(a => a.OcorridoEm)
            .Skip((filtro.Pagina - 1) * filtro.TamanhoPagina)
            .Take(filtro.TamanhoPagina)
            .Select(a => new AuditoriaItemDto(
                a.Id,
                a.OcorridoEm,
                a.EmailSuperAdmin,
                a.Acao,
                a.RestauranteId,
                Restaurantes.Where(x => x.Id == a.RestauranteId).Select(x => x.Nome).FirstOrDefault(),
                a.UsuarioAlvoId,
                a.ValorAntes,
                a.ValorDepois))
            .ToListAsync(cancellationToken);

        return new ResultadoPaginado<AuditoriaItemDto>(itens, filtro.Pagina, filtro.TamanhoPagina, totalItens);
    }

    private IQueryable<Guid> IdsDaRole(string role) =>
        from ur in _context.UserRoles
        join r in _context.Roles on ur.RoleId equals r.Id
        where r.Name == role
        select ur.UserId;
}
