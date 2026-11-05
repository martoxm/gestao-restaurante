using MediatR;
using GestaoRestaurante.Core.Application.Abstractions;
using GestaoRestaurante.Core.Application.Exceptions;
using GestaoRestaurante.Core.Application.Models;
using GestaoRestaurante.Core.Domain;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Core.Seguranca;
using GestaoRestaurante.Identidade.Application.Abstractions.Repositories;
using GestaoRestaurante.Identidade.Application.Abstractions.Services;
using GestaoRestaurante.Identidade.Application.UseCases.Auth;
using GestaoRestaurante.Identidade.Application.UseCases.Convites;
using GestaoRestaurante.Identidade.Contracts.Convites;
using GestaoRestaurante.Identidade.Contracts.Plataforma;
using GestaoRestaurante.Identidade.Domain.Entities;
using GestaoRestaurante.Identidade.Domain.Enums;

namespace GestaoRestaurante.Identidade.Application.UseCases.Plataforma;



public class ObterResumoPlataformaHandler : IRequestHandler<ObterResumoPlataformaQuery, ResumoPlataformaDto>
{
    private readonly IConsultasPlataforma _consultas;
    private readonly IRelogio _relogio;

    public ObterResumoPlataformaHandler(IConsultasPlataforma consultas, IRelogio relogio)
    {
        _consultas = consultas;
        _relogio = relogio;
    }

    public Task<ResumoPlataformaDto> Handle(ObterResumoPlataformaQuery request, CancellationToken cancellationToken) =>
        _consultas.ObterResumoAsync(_relogio.AgoraUtc, cancellationToken);
}

public class ListarRestaurantesPlataformaHandler
    : IRequestHandler<ListarRestaurantesPlataformaQuery, ResultadoPaginado<RestaurantePlataformaItemDto>>
{
    private readonly IConsultasPlataforma _consultas;
    private readonly IRelogio _relogio;

    public ListarRestaurantesPlataformaHandler(IConsultasPlataforma consultas, IRelogio relogio)
    {
        _consultas = consultas;
        _relogio = relogio;
    }

    public Task<ResultadoPaginado<RestaurantePlataformaItemDto>> Handle(ListarRestaurantesPlataformaQuery request, CancellationToken cancellationToken) =>
        _consultas.ListarRestaurantesAsync(request.Filtro, _relogio.AgoraUtc, cancellationToken);
}

public class ObterRestaurantePlataformaHandler : IRequestHandler<ObterRestaurantePlataformaQuery, RestaurantePlataformaDetalhesDto>
{
    private readonly IConsultasPlataforma _consultas;
    private readonly IRelogio _relogio;

    public ObterRestaurantePlataformaHandler(IConsultasPlataforma consultas, IRelogio relogio)
    {
        _consultas = consultas;
        _relogio = relogio;
    }

    public async Task<RestaurantePlataformaDetalhesDto> Handle(ObterRestaurantePlataformaQuery request, CancellationToken cancellationToken) =>
        await _consultas.ObterRestauranteAsync(request.Id, _relogio.AgoraUtc, cancellationToken)
            ?? throw new NaoEncontradoException($"Restaurante com Id '{request.Id}' não encontrado.");
}

public class ListarAuditoriaHandler : IRequestHandler<ListarAuditoriaQuery, ResultadoPaginado<AuditoriaItemDto>>
{
    private readonly IConsultasPlataforma _consultas;

    public ListarAuditoriaHandler(IConsultasPlataforma consultas)
    {
        _consultas = consultas;
    }

    public Task<ResultadoPaginado<AuditoriaItemDto>> Handle(ListarAuditoriaQuery request, CancellationToken cancellationToken) =>
        _consultas.ListarAuditoriaAsync(request.Filtro, cancellationToken);
}


public class MudarPlanoRestauranteHandler : IRequestHandler<MudarPlanoRestauranteCommand>
{
    private readonly IRestauranteRepository _restauranteRepository;
    private readonly IAuditoriaPlataformaRepository _auditoriaRepository;
    private readonly IApplicationDbContext _context;
    private readonly IRelogio _relogio;

    public MudarPlanoRestauranteHandler(
        IRestauranteRepository restauranteRepository,
        IAuditoriaPlataformaRepository auditoriaRepository,
        IApplicationDbContext context,
        IRelogio relogio)
    {
        _restauranteRepository = restauranteRepository;
        _auditoriaRepository = auditoriaRepository;
        _context = context;
        _relogio = relogio;
    }

    public async Task Handle(MudarPlanoRestauranteCommand request, CancellationToken cancellationToken)
    {
        var agora = _relogio.AgoraUtc;
        var restaurante = await ObterRestaurante.SemFiltroAsync(_restauranteRepository, request.RestauranteId, cancellationToken);

        var novoPlano = Enum.Parse<PlanoRestaurante>(request.Plano, ignoreCase: true);
        var antes = restaurante.Plano.ToString();

        restaurante.MudarPlano(novoPlano, agora);
        _restauranteRepository.Atualizar(restaurante);

        _auditoriaRepository.Adicionar(AuditoriaPlataforma.Registrar(
            request.QuemFaz.Id, request.QuemFaz.Email, AcoesPlataforma.MudarPlano, agora,
            restauranteId: restaurante.Id, valorAntes: antes, valorDepois: restaurante.Plano.ToString()));

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class EstenderTesteRestauranteHandler : IRequestHandler<EstenderTesteRestauranteCommand>
{
    private readonly IRestauranteRepository _restauranteRepository;
    private readonly IAuditoriaPlataformaRepository _auditoriaRepository;
    private readonly IApplicationDbContext _context;
    private readonly IRelogio _relogio;

    public EstenderTesteRestauranteHandler(
        IRestauranteRepository restauranteRepository,
        IAuditoriaPlataformaRepository auditoriaRepository,
        IApplicationDbContext context,
        IRelogio relogio)
    {
        _restauranteRepository = restauranteRepository;
        _auditoriaRepository = auditoriaRepository;
        _context = context;
        _relogio = relogio;
    }

    public async Task Handle(EstenderTesteRestauranteCommand request, CancellationToken cancellationToken)
    {
        var agora = _relogio.AgoraUtc;
        var restaurante = await ObterRestaurante.SemFiltroAsync(_restauranteRepository, request.RestauranteId, cancellationToken);
        var antes = restaurante.TesteTerminaEm;

        restaurante.EstenderTeste(request.Dias, agora);
        _restauranteRepository.Atualizar(restaurante);

        _auditoriaRepository.Adicionar(AuditoriaPlataforma.Registrar(
            request.QuemFaz.Id, request.QuemFaz.Email, AcoesPlataforma.EstenderTeste, agora,
            restauranteId: restaurante.Id, valorAntes: antes?.ToString("O"), valorDepois: restaurante.TesteTerminaEm?.ToString("O")));

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class AlterarAtivoRestauranteHandler : IRequestHandler<AlterarAtivoRestauranteCommand>
{
    private readonly IRestauranteRepository _restauranteRepository;
    private readonly IAuditoriaPlataformaRepository _auditoriaRepository;
    private readonly IApplicationDbContext _context;
    private readonly IRelogio _relogio;

    public AlterarAtivoRestauranteHandler(
        IRestauranteRepository restauranteRepository,
        IAuditoriaPlataformaRepository auditoriaRepository,
        IApplicationDbContext context,
        IRelogio relogio)
    {
        _restauranteRepository = restauranteRepository;
        _auditoriaRepository = auditoriaRepository;
        _context = context;
        _relogio = relogio;
    }

    public async Task Handle(AlterarAtivoRestauranteCommand request, CancellationToken cancellationToken)
    {
        var agora = _relogio.AgoraUtc;
        var restaurante = await ObterRestaurante.SemFiltroAsync(_restauranteRepository, request.RestauranteId, cancellationToken);

        restaurante.DefinirAtivo(request.Ativo);
        _restauranteRepository.Atualizar(restaurante);

        _auditoriaRepository.Adicionar(AuditoriaPlataforma.Registrar(
            request.QuemFaz.Id, request.QuemFaz.Email,
            request.Ativo ? AcoesPlataforma.AtivarRestaurante : AcoesPlataforma.DesativarRestaurante, agora,
            restauranteId: restaurante.Id, valorAntes: (!request.Ativo).ToString(), valorDepois: request.Ativo.ToString()));

        await _context.SaveChangesAsync(cancellationToken);
    }
}


public class ReenviarConviteHandler : IRequestHandler<ReenviarConviteCommand, ConviteCriadoResponse>
{
    private readonly IConviteRepository _conviteRepository;
    private readonly IAuditoriaPlataformaRepository _auditoriaRepository;
    private readonly IOutboxMensagemRepository _outboxRepository;
    private readonly IGeradorRefreshToken _geradorToken;
    private readonly ILinksFront _linksFront;
    private readonly IApplicationDbContext _context;
    private readonly IRelogio _relogio;

    public ReenviarConviteHandler(
        IConviteRepository conviteRepository,
        IAuditoriaPlataformaRepository auditoriaRepository,
        IOutboxMensagemRepository outboxRepository,
        IGeradorRefreshToken geradorToken,
        ILinksFront linksFront,
        IApplicationDbContext context,
        IRelogio relogio)
    {
        _conviteRepository = conviteRepository;
        _auditoriaRepository = auditoriaRepository;
        _outboxRepository = outboxRepository;
        _geradorToken = geradorToken;
        _linksFront = linksFront;
        _context = context;
        _relogio = relogio;
    }

    public async Task<ConviteCriadoResponse> Handle(ReenviarConviteCommand request, CancellationToken cancellationToken)
    {
        var agora = _relogio.AgoraUtc;
        var antigo = await _conviteRepository.ObterPorIdSemFiltroAsync(request.ConviteId, cancellationToken)
            ?? throw new NaoEncontradoException($"Convite com Id '{request.ConviteId}' não encontrado.");

        antigo.Cancelar(agora);
        _conviteRepository.Atualizar(antigo);

        var token = _geradorToken.Gerar();
        var novo = Convite.Criar(antigo.RestauranteId, antigo.NomeRestaurante, antigo.Email, antigo.Role, token.Hash, agora);
        _conviteRepository.Adicionar(novo);

        _outboxRepository.Adicionar(OutboxMensagem.Criar(
            Filas.Email, EmailsConvite.Montar(novo, _linksFront.AceitarConvite(token.Token)), agora));

        _auditoriaRepository.Adicionar(AuditoriaPlataforma.Registrar(
            request.QuemFaz.Id, request.QuemFaz.Email, AcoesPlataforma.ReenviarConvite, agora,
            restauranteId: antigo.RestauranteId, valorAntes: antigo.Id.ToString(), valorDepois: novo.Id.ToString()));

        await _context.SaveChangesAsync(cancellationToken);

        return new ConviteCriadoResponse(novo.Id, novo.ExpiraEm);
    }
}

public class ForcarLogoutUsuarioHandler : IRequestHandler<ForcarLogoutUsuarioCommand>
{
    private readonly IIdentityService _identityService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuditoriaPlataformaRepository _auditoriaRepository;
    private readonly IApplicationDbContext _context;
    private readonly IRelogio _relogio;

    public ForcarLogoutUsuarioHandler(
        IIdentityService identityService,
        IRefreshTokenRepository refreshTokenRepository,
        IAuditoriaPlataformaRepository auditoriaRepository,
        IApplicationDbContext context,
        IRelogio relogio)
    {
        _identityService = identityService;
        _refreshTokenRepository = refreshTokenRepository;
        _auditoriaRepository = auditoriaRepository;
        _context = context;
        _relogio = relogio;
    }

    public async Task Handle(ForcarLogoutUsuarioCommand request, CancellationToken cancellationToken)
    {
        var agora = _relogio.AgoraUtc;
        var usuario = await ObterUsuario.DeRestauranteAsync(_identityService, request.UsuarioId, cancellationToken);

        await SessoesUsuario.EncerrarTodasAsync(_refreshTokenRepository, usuario.Id, agora, cancellationToken);

        _auditoriaRepository.Adicionar(AuditoriaPlataforma.Registrar(
            request.QuemFaz.Id, request.QuemFaz.Email, AcoesPlataforma.ForcarLogout, agora,
            restauranteId: usuario.RestauranteId, usuarioAlvoId: usuario.Id, valorDepois: usuario.Email));

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class AlterarAtivoUsuarioPlataformaHandler : IRequestHandler<AlterarAtivoUsuarioPlataformaCommand>
{
    private readonly IIdentityService _identityService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuditoriaPlataformaRepository _auditoriaRepository;
    private readonly IApplicationDbContext _context;
    private readonly IRelogio _relogio;

    public AlterarAtivoUsuarioPlataformaHandler(
        IIdentityService identityService,
        IRefreshTokenRepository refreshTokenRepository,
        IAuditoriaPlataformaRepository auditoriaRepository,
        IApplicationDbContext context,
        IRelogio relogio)
    {
        _identityService = identityService;
        _refreshTokenRepository = refreshTokenRepository;
        _auditoriaRepository = auditoriaRepository;
        _context = context;
        _relogio = relogio;
    }

    public async Task Handle(AlterarAtivoUsuarioPlataformaCommand request, CancellationToken cancellationToken)
    {
        var agora = _relogio.AgoraUtc;
        var usuario = await ObterUsuario.DeRestauranteAsync(_identityService, request.UsuarioId, cancellationToken);

        if (usuario.Ativo == request.Ativo)
            throw new DomainException(request.Ativo ? "Este usuário já está ativo." : "Este usuário já está desativado.");

        if (!request.Ativo && usuario.Roles.Contains(RoleNames.Admin))
        {
            var equipe = await _identityService.ListarUsuariosDoRestauranteAsync(usuario.RestauranteId!.Value, cancellationToken);
            if (!equipe.Any(u => u.Id != usuario.Id && u.Ativo && u.Role == RoleNames.Admin))
                throw new DomainException("Este é o único Admin ativo do restaurante. Promova outra pessoa antes ou desative o restaurante inteiro.");
        }

        await _identityService.DefinirAtivoAsync(usuario.Id, request.Ativo, cancellationToken);

        if (!request.Ativo)
            await SessoesUsuario.EncerrarTodasAsync(_refreshTokenRepository, usuario.Id, agora, cancellationToken);

        _auditoriaRepository.Adicionar(AuditoriaPlataforma.Registrar(
            request.QuemFaz.Id, request.QuemFaz.Email,
            request.Ativo ? AcoesPlataforma.AtivarUsuario : AcoesPlataforma.DesativarUsuario, agora,
            restauranteId: usuario.RestauranteId, usuarioAlvoId: usuario.Id,
            valorAntes: usuario.Ativo.ToString(), valorDepois: request.Ativo.ToString()));

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public static class AcoesPlataforma
{
    public const string MudarPlano = "MudarPlano";
    public const string EstenderTeste = "EstenderTeste";
    public const string AtivarRestaurante = "AtivarRestaurante";
    public const string DesativarRestaurante = "DesativarRestaurante";
    public const string ReenviarConvite = "ReenviarConvite";
    public const string ForcarLogout = "ForcarLogout";
    public const string AtivarUsuario = "AtivarUsuario";
    public const string DesativarUsuario = "DesativarUsuario";
}

file static class ObterRestaurante
{
    public static async Task<Restaurante> SemFiltroAsync(IRestauranteRepository repository, Guid id, CancellationToken cancellationToken) =>
        await repository.ObterPorIdSemFiltroAsync(id, cancellationToken)
            ?? throw new NaoEncontradoException($"Restaurante com Id '{id}' não encontrado.");
}

file static class ObterUsuario
{
    public static async Task<UsuarioDaPlataforma> DeRestauranteAsync(IIdentityService identityService, Guid id, CancellationToken cancellationToken)
    {
        var usuario = await identityService.ObterUsuarioParaPlataformaAsync(id, cancellationToken)
            ?? throw new NaoEncontradoException($"Usuário com Id '{id}' não encontrado.");

        if (usuario.RestauranteId is null || usuario.Roles.Contains(RoleNames.SuperAdmin))
            throw new DomainException("Essa ação só vale para usuários de restaurante.");

        return usuario;
    }
}


public class ListarNotificacoesPlataformaHandler : IRequestHandler<ListarNotificacoesPlataformaQuery, ResultadoPaginado<NotificacaoPlataformaDto>>
{
    private readonly INotificacaoPlataformaRepository _notificacoes;

    public ListarNotificacoesPlataformaHandler(INotificacaoPlataformaRepository notificacoes)
    {
        _notificacoes = notificacoes;
    }

    public async Task<ResultadoPaginado<NotificacaoPlataformaDto>> Handle(ListarNotificacoesPlataformaQuery request, CancellationToken cancellationToken)
    {
        var filtro = request.Filtro;
        var (itens, total) = await _notificacoes.ListarAsync(filtro.Pagina, filtro.TamanhoPagina, filtro.SoNaoLidas, cancellationToken);

        var dtos = itens
            .Select(notificacao => new NotificacaoPlataformaDto(
                notificacao.Id, notificacao.Tipo, notificacao.Titulo, notificacao.Mensagem,
                notificacao.RestauranteId, notificacao.OcorridoEm, notificacao.LidaEm is not null))
            .ToList();

        return new ResultadoPaginado<NotificacaoPlataformaDto>(dtos, filtro.Pagina, filtro.TamanhoPagina, total);
    }
}

public class ContarNotificacoesNaoLidasHandler : IRequestHandler<ContarNotificacoesNaoLidasQuery, NotificacoesNaoLidasDto>
{
    private readonly INotificacaoPlataformaRepository _notificacoes;

    public ContarNotificacoesNaoLidasHandler(INotificacaoPlataformaRepository notificacoes)
    {
        _notificacoes = notificacoes;
    }

    public async Task<NotificacoesNaoLidasDto> Handle(ContarNotificacoesNaoLidasQuery request, CancellationToken cancellationToken) =>
        new(await _notificacoes.ContarNaoLidasAsync(cancellationToken));
}

public class MarcarNotificacoesLidasHandler : IRequestHandler<MarcarNotificacoesLidasCommand, int>
{
    private readonly INotificacaoPlataformaRepository _notificacoes;
    private readonly IRelogio _relogio;

    public MarcarNotificacoesLidasHandler(INotificacaoPlataformaRepository notificacoes, IRelogio relogio)
    {
        _notificacoes = notificacoes;
        _relogio = relogio;
    }

    public Task<int> Handle(MarcarNotificacoesLidasCommand request, CancellationToken cancellationToken) =>
        _notificacoes.MarcarComoLidasAsync(request.Id, _relogio.AgoraUtc, cancellationToken);
}

public class NotificarVencimentosHandler : IRequestHandler<NotificarVencimentosCommand, int>
{
    public const int DiasParaTras = 7;

    private readonly IRestauranteRepository _restauranteRepository;
    private readonly INotificacaoPlataformaRepository _notificacoes;
    private readonly IApplicationDbContext _context;
    private readonly IRelogio _relogio;

    public NotificarVencimentosHandler(
        IRestauranteRepository restauranteRepository,
        INotificacaoPlataformaRepository notificacoes,
        IApplicationDbContext context,
        IRelogio relogio)
    {
        _restauranteRepository = restauranteRepository;
        _notificacoes = notificacoes;
        _context = context;
        _relogio = relogio;
    }

    public async Task<int> Handle(NotificarVencimentosCommand request, CancellationToken cancellationToken)
    {
        var agora = _relogio.AgoraUtc;
        var vencidos = await _restauranteRepository.ListarVencidosEntreAsync(agora.AddDays(-DiasParaTras), agora, cancellationToken);

        var criadas = 0;
        foreach (var restaurante in vencidos)
        {
            var notificacao = NotificacaoPlataforma.Vencimento(restaurante, agora);
            if (notificacao?.Chave is null || await _notificacoes.ExisteChaveAsync(notificacao.Chave, cancellationToken))
                continue;

            _notificacoes.Adicionar(notificacao);
            criadas++;
        }

        if (criadas > 0)
            await _context.SaveChangesAsync(cancellationToken);

        return criadas;
    }
}
