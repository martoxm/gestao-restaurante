using System.Net;
using MediatR;
using GestaoRestaurante.Core.Application.Abstractions;
using GestaoRestaurante.Core.Application.Exceptions;
using GestaoRestaurante.Core.Domain;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Core.Mensageria.Contratos;
using GestaoRestaurante.Identidade.Application.Abstractions.Repositories;
using GestaoRestaurante.Identidade.Application.Abstractions.Services;
using GestaoRestaurante.Identidade.Application.UseCases.Auth;
using GestaoRestaurante.Identidade.Contracts.Auth;
using GestaoRestaurante.Identidade.Contracts.Convites;
using GestaoRestaurante.Identidade.Domain.Entities;

namespace GestaoRestaurante.Identidade.Application.UseCases.Convites;

public class CriarConviteHandler : IRequestHandler<CriarConviteCommand, ConviteCriadoResponse>
{
    private readonly IConviteRepository _conviteRepository;
    private readonly IRestauranteRepository _restauranteRepository;
    private readonly IOutboxMensagemRepository _outboxRepository;
    private readonly IGeradorRefreshToken _geradorToken;
    private readonly ILinksFront _linksFront;
    private readonly IUsuarioAtualService _usuarioAtualService;
    private readonly IApplicationDbContext _context;
    private readonly IRelogio _relogio;
    private readonly IIdentityService _identityService;

    public CriarConviteHandler(
        IIdentityService identityService,
        IConviteRepository conviteRepository,
        IRestauranteRepository restauranteRepository,
        IOutboxMensagemRepository outboxRepository,
        IGeradorRefreshToken geradorToken,
        ILinksFront linksFront,
        IUsuarioAtualService usuarioAtualService,
        IApplicationDbContext context,
        IRelogio relogio)
    {
        _conviteRepository = conviteRepository;
        _restauranteRepository = restauranteRepository;
        _outboxRepository = outboxRepository;
        _geradorToken = geradorToken;
        _linksFront = linksFront;
        _usuarioAtualService = usuarioAtualService;
        _context = context;
        _relogio = relogio;
        _identityService = identityService;
    }

    public async Task<ConviteCriadoResponse> Handle(CriarConviteCommand request, CancellationToken cancellationToken)
    {
        var restauranteId = _usuarioAtualService.ObterRestauranteIdObrigatorio();
        var agora = _relogio.AgoraUtc;

        var restaurante = await _restauranteRepository.ObterPorIdAsync(restauranteId, cancellationToken)
            ?? throw new NaoEncontradoException("Restaurante não encontrado.");

        var email = request.Email.Trim().ToLowerInvariant();

        var usuarios = await _identityService.ContarUsuariosDoRestauranteAsync(restauranteId, cancellationToken);
        var pendentes = await _conviteRepository.ContarPendentesAsync(agora, email, cancellationToken);
        restaurante.GarantirQueCabeMaisUmUsuario(usuarios + pendentes);

        foreach (var antigo in await _conviteRepository.ListarPendentesPorEmailAsync(email, agora, cancellationToken))
        {
            antigo.Cancelar(agora);
            _conviteRepository.Atualizar(antigo);
        }

        var token = _geradorToken.Gerar();
        var convite = Convite.Criar(restauranteId, restaurante.Nome, email, request.Role, token.Hash, agora);
        _conviteRepository.Adicionar(convite);

        _outboxRepository.Adicionar(OutboxMensagem.Criar(
            Filas.Email,
            EmailsConvite.Montar(convite, _linksFront.AceitarConvite(token.Token)),
            agora));

        await _context.SaveChangesAsync(cancellationToken);

        return new ConviteCriadoResponse(convite.Id, convite.ExpiraEm);
    }
}

internal static class EmailsConvite
{
    public static EnviarEmailMensagem Montar(Convite convite, string link)
    {
        var nome = WebUtility.HtmlEncode(convite.NomeRestaurante);
        var corpo = $"""
            <p>Olá!</p>
            <p>Você foi convidado para entrar no restaurante <strong>{nome}</strong> no Gestão Restaurante.</p>
            <p><a href="{link}">Clique aqui para aceitar o convite e criar sua senha</a>.</p>
            <p>O link vale por {Convite.DiasValidade} dias e só pode ser usado uma vez.</p>
            """;

        return new EnviarEmailMensagem([convite.Email], $"Convite para {convite.NomeRestaurante}", corpo);
    }
}

public class CancelarConviteHandler : IRequestHandler<CancelarConviteCommand>
{
    private readonly IConviteRepository _conviteRepository;
    private readonly IApplicationDbContext _context;
    private readonly IRelogio _relogio;

    public CancelarConviteHandler(IConviteRepository conviteRepository, IApplicationDbContext context, IRelogio relogio)
    {
        _conviteRepository = conviteRepository;
        _context = context;
        _relogio = relogio;
    }

    public async Task Handle(CancelarConviteCommand request, CancellationToken cancellationToken)
    {
        var convite = await _conviteRepository.ObterPorIdAsync(request.Id, cancellationToken)
            ?? throw new NaoEncontradoException($"Convite com Id '{request.Id}' não encontrado.");

        convite.Cancelar(_relogio.AgoraUtc);
        _conviteRepository.Atualizar(convite);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class ListarConvitesHandler : IRequestHandler<ListarConvitesQuery, IReadOnlyList<ConviteItemListaDto>>
{
    private readonly IConviteRepository _conviteRepository;
    private readonly IRelogio _relogio;

    public ListarConvitesHandler(IConviteRepository conviteRepository, IRelogio relogio)
    {
        _conviteRepository = conviteRepository;
        _relogio = relogio;
    }

    public async Task<IReadOnlyList<ConviteItemListaDto>> Handle(ListarConvitesQuery request, CancellationToken cancellationToken)
    {
        var agora = _relogio.AgoraUtc;
        var convites = await _conviteRepository.ListarAsync(cancellationToken);

        return convites.Select(convite => convite.ParaItemListaDto(agora)).ToList();
    }
}

public class ObterConvitePorTokenHandler : IRequestHandler<ObterConvitePorTokenQuery, ConvitePublicoDto>
{
    private readonly IConviteRepository _conviteRepository;
    private readonly IGeradorRefreshToken _geradorToken;
    private readonly IRelogio _relogio;

    public ObterConvitePorTokenHandler(IConviteRepository conviteRepository, IGeradorRefreshToken geradorToken, IRelogio relogio)
    {
        _conviteRepository = conviteRepository;
        _geradorToken = geradorToken;
        _relogio = relogio;
    }

    public async Task<ConvitePublicoDto> Handle(ObterConvitePorTokenQuery request, CancellationToken cancellationToken)
    {
        var convite = await _conviteRepository.ObterPorHashAsync(_geradorToken.CalcularHash(request.Token), cancellationToken);

        if (convite is null || !convite.EstaPendente(_relogio.AgoraUtc))
            throw new NaoEncontradoException(MensagensConvite.Invalido);

        return new ConvitePublicoDto(convite.Email, convite.NomeRestaurante, convite.Role, convite.ExpiraEm);
    }
}

public class AceitarConviteHandler : IRequestHandler<AceitarConviteCommand, LoginResponse>
{
    private readonly IConviteRepository _conviteRepository;
    private readonly IGeradorRefreshToken _geradorToken;
    private readonly IIdentityService _identityService;
    private readonly EmissorSessao _emissorSessao;
    private readonly IApplicationDbContext _context;
    private readonly IRelogio _relogio;
    private readonly IRestauranteRepository _restauranteRepository;

    public AceitarConviteHandler(
        IRestauranteRepository restauranteRepository,
        IConviteRepository conviteRepository,
        IGeradorRefreshToken geradorToken,
        IIdentityService identityService,
        EmissorSessao emissorSessao,
        IApplicationDbContext context,
        IRelogio relogio)
    {
        _conviteRepository = conviteRepository;
        _geradorToken = geradorToken;
        _identityService = identityService;
        _emissorSessao = emissorSessao;
        _context = context;
        _relogio = relogio;
        _restauranteRepository = restauranteRepository;
    }

    public async Task<LoginResponse> Handle(AceitarConviteCommand request, CancellationToken cancellationToken)
    {
        var hash = _geradorToken.CalcularHash(request.Token);

        return await _context.ExecutarEmTransacaoAsync(async ct =>
        {
            var convite = await _conviteRepository.ObterPorHashAsync(hash, ct)
                ?? throw new NaoEncontradoException(MensagensConvite.Invalido);

            convite.Aceitar(_relogio.AgoraUtc);

            var restaurante = await _restauranteRepository.ObterPorIdSemFiltroAsync(convite.RestauranteId, ct)
                ?? throw new NaoEncontradoException(MensagensConvite.Invalido);
            if (!restaurante.EstaLiberado(_relogio.AgoraUtc))
                throw new DomainException("O restaurante está sem plano ativo. Fale com o administrador.");
            restaurante.GarantirQueCabeMaisUmUsuario(
                await _identityService.ContarUsuariosDoRestauranteAsync(convite.RestauranteId, ct));

            if (await _identityService.EmailExisteAsync(convite.Email, ct))
                throw new DomainException("Já existe uma conta com este e-mail. Faça login.");

            var usuarioId = await _identityService.CriarUsuarioAsync(
                request.Nome, convite.Email, request.Senha, convite.RestauranteId, convite.Role, emailConfirmado: true, ct);

            _conviteRepository.Atualizar(convite);

            var usuario = new UsuarioAutenticado(usuarioId, request.Nome, convite.Email, convite.RestauranteId);
            var resposta = await _emissorSessao.EmitirAsync(usuario, ct);
            await _context.SaveChangesAsync(ct);

            return resposta;
        }, cancellationToken);
    }
}

file static class MensagensConvite
{
    public const string Invalido = "Convite inválido ou expirado. Peça um novo convite ao administrador do restaurante.";
}

file static class ConviteMapeamentos
{
    public static ConviteItemListaDto ParaItemListaDto(this Convite convite, DateTime agora) =>
        new(convite.Id, convite.Email, convite.Role, convite.ObterStatus(agora), convite.CriadoEm, convite.ExpiraEm);
}
