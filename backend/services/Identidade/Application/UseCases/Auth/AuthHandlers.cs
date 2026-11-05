using GestaoRestaurante.Core.Domain;
using GestaoRestaurante.Core.Application.Abstractions;
using GestaoRestaurante.Core.Seguranca;
using MediatR;
using GestaoRestaurante.Identidade.Application.Abstractions.Repositories;
using GestaoRestaurante.Identidade.Application.Abstractions.Services;
using GestaoRestaurante.Core.Application.Exceptions;
using GestaoRestaurante.Identidade.Contracts.Auth;
using GestaoRestaurante.Identidade.Domain.Entities;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Core.Mensageria.Contratos;
using System.Net;

namespace GestaoRestaurante.Identidade.Application.UseCases.Auth;

public class LoginHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IIdentityService _identityService;
    private readonly EmissorSessao _emissorSessao;
    private readonly IApplicationDbContext _context;

    public LoginHandler(IIdentityService identityService, EmissorSessao emissorSessao, IApplicationDbContext context)
    {
        _identityService = identityService;
        _emissorSessao = emissorSessao;
        _context = context;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var usuario = await _identityService.ValidarCredenciaisAsync(email, request.Senha, cancellationToken) ?? throw new CredenciaisInvalidasException();

        var resposta = await _emissorSessao.EmitirAsync(usuario, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return resposta;
    }
}

public class RegistrarHandler : IRequestHandler<RegistrarCommand, LoginResponse>
{
    private readonly IIdentityService _identityService;
    private readonly EmissorSessao _emissorSessao;
    private readonly IRestauranteRepository _restauranteRepository;
    private readonly IApplicationDbContext _context;
    private readonly IRelogio _relogio;
    private readonly IOutboxMensagemRepository _outboxRepository;
    private readonly ILinksFront _linksFront;

    public RegistrarHandler(
        IIdentityService identityService,
        EmissorSessao emissorSessao,
        IRestauranteRepository restauranteRepository,
        IApplicationDbContext context,
        IRelogio relogio,
        IOutboxMensagemRepository outboxRepository,
        ILinksFront linksFront)
    {
        _outboxRepository = outboxRepository;
        _linksFront = linksFront;
        _identityService = identityService;
        _emissorSessao = emissorSessao;
        _restauranteRepository = restauranteRepository;
        _context = context;
        _relogio = relogio;
    }

    public async Task<LoginResponse> Handle(RegistrarCommand request, CancellationToken cancellationToken)
    {
        return await _context.ExecutarEmTransacaoAsync(async ct =>
        {
            var restaurante = Restaurante.Criar(request.NomeRestaurante, _relogio.AgoraUtc);
            _restauranteRepository.Adicionar(restaurante);
            await _context.SaveChangesAsync(ct);

            var usuarioId = await _identityService.CriarUsuarioAsync(request.Nome, request.Email, request.Senha, restaurante.Id, RoleNames.Admin, emailConfirmado: false, ct);

            var tokenConfirmacao = await _identityService.GerarTokenConfirmacaoEmailAsync(usuarioId, ct);
            _outboxRepository.Adicionar(OutboxMensagem.Criar(
                Filas.Email,
                EmailsConta.ConfirmarEmail(request.Email, request.Nome, _linksFront.ConfirmarEmail(usuarioId, tokenConfirmacao)),
                _relogio.AgoraUtc));

            var usuario = new UsuarioAutenticado(usuarioId, request.Nome, request.Email, restaurante.Id, EmailConfirmado: false);
            var resposta = await _emissorSessao.EmitirAsync(usuario, ct);
            await _context.SaveChangesAsync(ct);

            return resposta;
        }, cancellationToken);
    }
}

public class RenovarTokenHandler : IRequestHandler<RenovarTokenCommand, LoginResponse>
{
    private const string MensagemSessaoExpirada = "Sua sessão expirou. Faça login novamente.";

    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IGeradorRefreshToken _geradorRefreshToken;
    private readonly IIdentityService _identityService;
    private readonly EmissorSessao _emissorSessao;
    private readonly IApplicationDbContext _context;
    private readonly IRelogio _relogio;

    public RenovarTokenHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IGeradorRefreshToken geradorRefreshToken,
        IIdentityService identityService,
        EmissorSessao emissorSessao,
        IApplicationDbContext context,
        IRelogio relogio)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _geradorRefreshToken = geradorRefreshToken;
        _identityService = identityService;
        _emissorSessao = emissorSessao;
        _context = context;
        _relogio = relogio;
    }

    public async Task<LoginResponse> Handle(RenovarTokenCommand request, CancellationToken cancellationToken)
    {
        var agora = _relogio.AgoraUtc;
        var hash = _geradorRefreshToken.CalcularHash(request.RefreshToken);

        var refreshToken = await _refreshTokenRepository.ObterPorHashAsync(hash, cancellationToken)
            ?? throw new SessaoInvalidaException(MensagemSessaoExpirada);

        if (refreshToken.FoiReusadoForaDaTolerancia(agora))
        {
            var ativos = await _refreshTokenRepository.ListarAtivosDoUsuarioAsync(refreshToken.UsuarioId, agora, cancellationToken);
            foreach (var ativo in ativos)
                _refreshTokenRepository.Remover(ativo);

            await _context.SaveChangesAsync(cancellationToken);
            throw new SessaoInvalidaException(MensagemSessaoExpirada);
        }

        if (!refreshToken.PodeSerUsado(agora))
            throw new SessaoInvalidaException(MensagemSessaoExpirada);

        var usuario = await _identityService.ObterUsuarioPorIdAsync(refreshToken.UsuarioId, cancellationToken)
            ?? throw new SessaoInvalidaException(MensagemSessaoExpirada);

        refreshToken.Revogar(agora);
        _refreshTokenRepository.Atualizar(refreshToken);

        var resposta = await _emissorSessao.EmitirAsync(usuario, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return resposta;
    }
}

public class EmissorSessao
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IGeradorRefreshToken _geradorRefreshToken;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRelogio _relogio;
    private readonly IRestauranteRepository _restauranteRepository;

    public EmissorSessao(
        IRestauranteRepository restauranteRepository,
        IIdentityService identityService,
        IJwtTokenGenerator jwtTokenGenerator,
        IGeradorRefreshToken geradorRefreshToken,
        IRefreshTokenRepository refreshTokenRepository,
        IRelogio relogio)
    {
        _identityService = identityService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _geradorRefreshToken = geradorRefreshToken;
        _refreshTokenRepository = refreshTokenRepository;
        _relogio = relogio;
        _restauranteRepository = restauranteRepository;
    }

    public async Task<LoginResponse> EmitirAsync(UsuarioAutenticado usuario, CancellationToken cancellationToken)
    {
        var roles = await _identityService.ObterRolesAsync(usuario.Id, cancellationToken);
        var agora = _relogio.AgoraUtc;

        var liberado = true;
        if (usuario.RestauranteId is Guid restauranteId)
        {
            var restaurante = await _restauranteRepository.ObterPorIdSemFiltroAsync(restauranteId, cancellationToken);
            liberado = restaurante?.EstaLiberado(agora) ?? false;
        }

        var token = _jwtTokenGenerator.GerarToken(usuario.Id, usuario.Nome, usuario.Email, roles, usuario.RestauranteId, liberado, usuario.EmailConfirmado);

        var refreshToken = _geradorRefreshToken.Gerar();
        _refreshTokenRepository.Adicionar(
            RefreshToken.Criar(usuario.Id, refreshToken.Hash, agora, agora.AddDays(_geradorRefreshToken.DiasValidade)));

        return new LoginResponse(token, refreshToken.Token, usuario.Nome, usuario.Email);
    }
}


public class EsqueciSenhaHandler : IRequestHandler<EsqueciSenhaCommand>
{
    private readonly IIdentityService _identityService;
    private readonly IOutboxMensagemRepository _outboxRepository;
    private readonly ILinksFront _linksFront;
    private readonly IApplicationDbContext _context;
    private readonly IRelogio _relogio;

    public EsqueciSenhaHandler(IIdentityService identityService, IOutboxMensagemRepository outboxRepository,
        ILinksFront linksFront, IApplicationDbContext context, IRelogio relogio)
    {
        _identityService = identityService;
        _outboxRepository = outboxRepository;
        _linksFront = linksFront;
        _context = context;
        _relogio = relogio;
    }

    public async Task Handle(EsqueciSenhaCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var gerado = await _identityService.GerarTokenRedefinicaoSenhaAsync(email, cancellationToken);

        if (gerado is null)
            return;

        _outboxRepository.Adicionar(OutboxMensagem.Criar(
            Filas.Email,
            EmailsConta.RedefinirSenha(email, _linksFront.RedefinirSenha(email, gerado.Value.Token)),
            _relogio.AgoraUtc));
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class RedefinirSenhaHandler : IRequestHandler<RedefinirSenhaCommand>
{
    private readonly IIdentityService _identityService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IApplicationDbContext _context;
    private readonly IRelogio _relogio;

    public RedefinirSenhaHandler(IIdentityService identityService, IRefreshTokenRepository refreshTokenRepository,
        IApplicationDbContext context, IRelogio relogio)
    {
        _identityService = identityService;
        _refreshTokenRepository = refreshTokenRepository;
        _context = context;
        _relogio = relogio;
    }

    public async Task Handle(RedefinirSenhaCommand request, CancellationToken cancellationToken)
    {
        var usuarioId = await _identityService.RedefinirSenhaAsync(
            request.Email.Trim().ToLowerInvariant(), request.Token, request.NovaSenha, cancellationToken);

        await SessoesUsuario.EncerrarTodasAsync(_refreshTokenRepository, usuarioId, _relogio.AgoraUtc, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class ConfirmarEmailHandler : IRequestHandler<ConfirmarEmailCommand>
{
    private readonly IIdentityService _identityService;

    public ConfirmarEmailHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(ConfirmarEmailCommand request, CancellationToken cancellationToken)
    {
        if (!await _identityService.ConfirmarEmailAsync(request.UsuarioId, request.Token, cancellationToken))
            throw new DomainException("Este link de confirmação é inválido ou já venceu. Peça um novo pelo aviso no sistema.");
    }
}

public class ReenviarConfirmacaoEmailHandler : IRequestHandler<ReenviarConfirmacaoEmailCommand>
{
    private readonly IIdentityService _identityService;
    private readonly IOutboxMensagemRepository _outboxRepository;
    private readonly ILinksFront _linksFront;
    private readonly IApplicationDbContext _context;
    private readonly IRelogio _relogio;

    public ReenviarConfirmacaoEmailHandler(IIdentityService identityService, IOutboxMensagemRepository outboxRepository,
        ILinksFront linksFront, IApplicationDbContext context, IRelogio relogio)
    {
        _identityService = identityService;
        _outboxRepository = outboxRepository;
        _linksFront = linksFront;
        _context = context;
        _relogio = relogio;
    }

    public async Task Handle(ReenviarConfirmacaoEmailCommand request, CancellationToken cancellationToken)
    {
        var usuario = await _identityService.ObterUsuarioPorIdAsync(request.UsuarioId, cancellationToken)
            ?? throw new NaoEncontradoException("Usuário não encontrado.");

        if (usuario.EmailConfirmado)
            throw new DomainException("Seu e-mail já está confirmado.");

        var token = await _identityService.GerarTokenConfirmacaoEmailAsync(usuario.Id, cancellationToken);
        _outboxRepository.Adicionar(OutboxMensagem.Criar(
            Filas.Email,
            EmailsConta.ConfirmarEmail(usuario.Email, usuario.Nome, _linksFront.ConfirmarEmail(usuario.Id, token)),
            _relogio.AgoraUtc));
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public static class SessoesUsuario
{
    public static async Task EncerrarTodasAsync(IRefreshTokenRepository repository, Guid usuarioId, DateTime agora, CancellationToken cancellationToken)
    {
        foreach (var ativo in await repository.ListarAtivosDoUsuarioAsync(usuarioId, agora, cancellationToken))
            repository.Remover(ativo);
    }
}

file static class EmailsConta
{
    public static EnviarEmailMensagem ConfirmarEmail(string email, string nome, string link) =>
        new([email], "Confirme seu e-mail - Gestão Restaurante", $"""
            <p>Olá, {WebUtility.HtmlEncode(nome)}!</p>
            <p>Confirme que este e-mail é seu: <a href="{link}">clique aqui para confirmar</a>.</p>
            <p>Se não foi você que criou a conta, pode ignorar esta mensagem.</p>
            """);

    public static EnviarEmailMensagem RedefinirSenha(string email, string link) =>
        new([email], "Redefinir sua senha - Gestão Restaurante", $"""
            <p>Recebemos um pedido para redefinir a sua senha.</p>
            <p><a href="{link}">Clique aqui para escolher uma senha nova</a>. O link vale por 1 dia e só funciona uma vez.</p>
            <p>Se não foi você, ignore esta mensagem: sua senha continua a mesma.</p>
            """);
}
