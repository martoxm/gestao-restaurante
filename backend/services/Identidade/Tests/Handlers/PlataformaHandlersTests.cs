using GestaoRestaurante.Core.Application.Exceptions;
using GestaoRestaurante.Core.Domain;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Core.Seguranca;
using GestaoRestaurante.Identidade.Application.Abstractions.Services;
using GestaoRestaurante.Identidade.Application.UseCases.Plataforma;
using GestaoRestaurante.Identidade.Domain.Entities;
using GestaoRestaurante.Identidade.Domain.Enums;
using GestaoRestaurante.Identidade.Tests.Common;

namespace GestaoRestaurante.Identidade.Tests.Handlers;

public class PlataformaHandlersTests
{
    private static readonly SuperAdminAtual QuemFaz = new(Guid.NewGuid(), "dono@plataforma.com");

    private readonly FakeUsuarioAtualService _usuarioAtual = new();
    private readonly FakeRelogio _relogio = new();
    private readonly FakeIdentityService _identityService = new();
    private readonly FakeOutboxMensagemRepository _outbox = new();
    private readonly FakeGeradorRefreshToken _gerador = new();
    private readonly TestDbContext _context;
    private readonly Restaurante _restaurante;

    public PlataformaHandlersTests()
    {
        _context = TestDbContextFactory.Create(_usuarioAtual);
        _restaurante = Restaurante.Criar("Cantina", _relogio.AgoraUtc);
        _context.Restaurantes.Add(_restaurante);
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
        _usuarioAtual.RestauranteId = null;
    }

    private Restaurante RestauranteSalvo() =>
        _context.Restaurantes.IgnoreQueryFiltersParaTeste().Single(r => r.Id == _restaurante.Id);

    [Fact]
    public async Task MudarPlano_DeveTrocarOPlanoEGravarAuditoria()
    {
        await new MudarPlanoRestauranteHandler(new FakeRestauranteRepository(_context), new FakeAuditoriaPlataformaRepository(_context), _context, _relogio)
            .Handle(new MudarPlanoRestauranteCommand(QuemFaz, _restaurante.Id, "pro"), CancellationToken.None);

        var restaurante = RestauranteSalvo();
        Assert.Equal(PlanoRestaurante.Pro, restaurante.Plano);
        Assert.Null(restaurante.TesteTerminaEm);

        var auditoria = Assert.Single(_context.AuditoriasPlataforma);
        Assert.Equal(AcoesPlataforma.MudarPlano, auditoria.Acao);
        Assert.Equal(QuemFaz.Id, auditoria.SuperAdminId);
        Assert.Equal(_restaurante.Id, auditoria.RestauranteId);
        Assert.Equal("Teste", auditoria.ValorAntes);
        Assert.Equal("Pro", auditoria.ValorDepois);
    }

    [Fact]
    public async Task MudarPlano_RestauranteInexistente_DeveLancarNaoEncontrado()
    {
        await Assert.ThrowsAsync<NaoEncontradoException>(() =>
            new MudarPlanoRestauranteHandler(new FakeRestauranteRepository(_context), new FakeAuditoriaPlataformaRepository(_context), _context, _relogio)
                .Handle(new MudarPlanoRestauranteCommand(QuemFaz, Guid.NewGuid(), "Pro"), CancellationToken.None));

        Assert.Empty(_context.AuditoriasPlataforma);
    }

    [Fact]
    public async Task EstenderTeste_DeveSomarOsDiasNoFimAtual()
    {
        await new EstenderTesteRestauranteHandler(new FakeRestauranteRepository(_context), new FakeAuditoriaPlataformaRepository(_context), _context, _relogio)
            .Handle(new EstenderTesteRestauranteCommand(QuemFaz, _restaurante.Id, 5), CancellationToken.None);

        Assert.Equal(_relogio.AgoraUtc.AddDays(Restaurante.DiasTeste + 5), RestauranteSalvo().TesteTerminaEm);
        Assert.Equal(AcoesPlataforma.EstenderTeste, Assert.Single(_context.AuditoriasPlataforma).Acao);
    }

    [Fact]
    public async Task Desativar_DeveDesligarORestauranteEGravarAuditoria()
    {
        await new AlterarAtivoRestauranteHandler(new FakeRestauranteRepository(_context), new FakeAuditoriaPlataformaRepository(_context), _context, _relogio)
            .Handle(new AlterarAtivoRestauranteCommand(QuemFaz, _restaurante.Id, false), CancellationToken.None);

        Assert.False(RestauranteSalvo().Ativo);
        Assert.Equal(AcoesPlataforma.DesativarRestaurante, Assert.Single(_context.AuditoriasPlataforma).Acao);
    }

    [Fact]
    public async Task ReenviarConvite_DeveCancelarOAntigoECriarUmNovoComEmail()
    {
        var antigo = Convite.Criar(_restaurante.Id, "Cantina", "bruno@teste.com", RoleNames.Funcionario, "hash-velho", _relogio.AgoraUtc);
        _context.Convites.Add(antigo);
        _context.SaveChanges();
        _context.ChangeTracker.Clear();

        var resposta = await new ReenviarConviteHandler(new FakeConviteRepository(_context), new FakeAuditoriaPlataformaRepository(_context),
                _outbox, _gerador, new FakeLinksFront(), _context, _relogio)
            .Handle(new ReenviarConviteCommand(QuemFaz, antigo.Id), CancellationToken.None);

        var convites = _context.Convites.IgnoreQueryFiltersParaTeste().ToList();
        Assert.Equal(2, convites.Count);
        Assert.NotNull(convites.Single(c => c.Id == antigo.Id).CanceladoEm);
        Assert.True(convites.Single(c => c.Id == resposta.Id).EstaPendente(_relogio.AgoraUtc));

        Assert.Equal(Filas.Email, Assert.Single(_outbox.Mensagens).Fila);
        Assert.Equal(AcoesPlataforma.ReenviarConvite, Assert.Single(_context.AuditoriasPlataforma).Acao);
    }

    [Fact]
    public async Task ForcarLogout_DeveApagarAsSessoesDoUsuario()
    {
        var anaId = await _identityService.CriarUsuarioAsync("Ana", "ana@teste.com", "senha123", _restaurante.Id, RoleNames.Funcionario, true, default);
        _context.RefreshTokens.Add(RefreshToken.Criar(anaId, "hash-x", _relogio.AgoraUtc, _relogio.AgoraUtc.AddDays(14)));
        _context.SaveChanges();
        _context.ChangeTracker.Clear();

        await new ForcarLogoutUsuarioHandler(_identityService, new FakeRefreshTokenRepository(_context),
                new FakeAuditoriaPlataformaRepository(_context), _context, _relogio)
            .Handle(new ForcarLogoutUsuarioCommand(QuemFaz, anaId), CancellationToken.None);

        Assert.Empty(_context.RefreshTokens);
        var auditoria = Assert.Single(_context.AuditoriasPlataforma);
        Assert.Equal(anaId, auditoria.UsuarioAlvoId);
    }

    [Fact]
    public async Task ForcarLogout_EmUsuarioSemRestaurante_DeveRecusar()
    {
        var id = Guid.NewGuid();
        _identityService.Usuarios[id] = new UsuarioAutenticado(id, "Outro", "outro@plataforma.com", null);
        _identityService.Roles[id] = RoleNames.SuperAdmin;

        await Assert.ThrowsAsync<DomainException>(() =>
            new ForcarLogoutUsuarioHandler(_identityService, new FakeRefreshTokenRepository(_context),
                    new FakeAuditoriaPlataformaRepository(_context), _context, _relogio)
                .Handle(new ForcarLogoutUsuarioCommand(QuemFaz, id), CancellationToken.None));
    }

    [Fact]
    public async Task DesativarUsuario_UnicoAdminAtivo_DeveRecusar()
    {
        var anaId = await _identityService.CriarUsuarioAsync("Ana", "ana@teste.com", "senha123", _restaurante.Id, RoleNames.Admin, true, default);
        _identityService.UsuariosPorRestaurante[_restaurante.Id] = [new UsuarioComRole(anaId, "Ana", "ana@teste.com", RoleNames.Admin)];

        await Assert.ThrowsAsync<DomainException>(() => DesativarUsuario(anaId));

        Assert.DoesNotContain(anaId, _identityService.Desativados);
        Assert.Empty(_context.AuditoriasPlataforma);
    }

    [Fact]
    public async Task DesativarUsuario_Funcionario_DeveDesativarEGravarAuditoria()
    {
        var brunoId = await _identityService.CriarUsuarioAsync("Bruno", "bruno@teste.com", "senha123", _restaurante.Id, RoleNames.Funcionario, true, default);

        await DesativarUsuario(brunoId);

        Assert.Contains(brunoId, _identityService.Desativados);
        Assert.Equal(AcoesPlataforma.DesativarUsuario, Assert.Single(_context.AuditoriasPlataforma).Acao);
    }

    private Task DesativarUsuario(Guid id) =>
        new AlterarAtivoUsuarioPlataformaHandler(_identityService, new FakeRefreshTokenRepository(_context),
                new FakeAuditoriaPlataformaRepository(_context), _context, _relogio)
            .Handle(new AlterarAtivoUsuarioPlataformaCommand(QuemFaz, id, false), CancellationToken.None);
}

file static class ConsultaSemFiltro
{
    public static IQueryable<T> IgnoreQueryFiltersParaTeste<T>(this Microsoft.EntityFrameworkCore.DbSet<T> set) where T : class =>
        Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.IgnoreQueryFilters(set);
}
