using MediatR;
using GestaoRestaurante.Core.Application.Abstractions;
using GestaoRestaurante.Core.Application.Exceptions;
using GestaoRestaurante.Core.Domain;
using GestaoRestaurante.Core.Seguranca;
using GestaoRestaurante.Identidade.Application.Abstractions.Services;
using GestaoRestaurante.Identidade.Contracts.Usuarios;
using GestaoRestaurante.Identidade.Application.Abstractions.Repositories;
using GestaoRestaurante.Identidade.Application.UseCases.Auth;

namespace GestaoRestaurante.Identidade.Application.UseCases.Usuarios;

public class ListarUsuariosHandler : IRequestHandler<ListarUsuariosQuery, IReadOnlyList<UsuarioItemListaDto>>
{
    private readonly IIdentityService _identityService;
    private readonly IUsuarioAtualService _usuarioAtualService;

    public ListarUsuariosHandler(IIdentityService identityService, IUsuarioAtualService usuarioAtualService)
    {
        _identityService = identityService;
        _usuarioAtualService = usuarioAtualService;
    }

    public async Task<IReadOnlyList<UsuarioItemListaDto>> Handle(ListarUsuariosQuery request, CancellationToken cancellationToken)
    {
        var restauranteId = _usuarioAtualService.ObterRestauranteIdObrigatorio();
        var usuarios = await _identityService.ListarUsuariosDoRestauranteAsync(restauranteId, cancellationToken);

        return usuarios
            .OrderBy(usuario => usuario.Nome)
            .Select(usuario => new UsuarioItemListaDto(usuario.Id, usuario.Nome, usuario.Email, usuario.Role, usuario.Ativo))
            .ToList();
    }
}

public class AlterarRoleUsuarioHandler : IRequestHandler<AlterarRoleUsuarioCommand>
{
    private readonly IIdentityService _identityService;
    private readonly IUsuarioAtualService _usuarioAtualService;

    public AlterarRoleUsuarioHandler(IIdentityService identityService, IUsuarioAtualService usuarioAtualService)
    {
        _identityService = identityService;
        _usuarioAtualService = usuarioAtualService;
    }

    public async Task Handle(AlterarRoleUsuarioCommand request, CancellationToken cancellationToken)
    {
        var restauranteId = _usuarioAtualService.ObterRestauranteIdObrigatorio();
        var usuarios = await _identityService.ListarUsuariosDoRestauranteAsync(restauranteId, cancellationToken);

        var usuario = usuarios.FirstOrDefault(u => u.Id == request.UsuarioId)
            ?? throw new NaoEncontradoException($"Usuário com Id '{request.UsuarioId}' não encontrado.");

        if (usuario.Role == request.Role)
            return;

        var quantidadeAdmins = usuarios.Count(u => u.Role == RoleNames.Admin);
        if (usuario.Role == RoleNames.Admin && quantidadeAdmins == 1)
            throw new DomainException("O restaurante precisa ter pelo menos um Admin. Promova outra pessoa antes de rebaixar este usuário.");

        await _identityService.DefinirRoleAsync(usuario.Id, request.Role, cancellationToken);
    }
}

public class AlterarAtivoUsuarioHandler : IRequestHandler<AlterarAtivoUsuarioCommand>
{
    private readonly IIdentityService _identityService;
    private readonly IUsuarioAtualService _usuarioAtualService;
    private readonly IRestauranteRepository _restauranteRepository;
    private readonly IConviteRepository _conviteRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IApplicationDbContext _context;
    private readonly IRelogio _relogio;

    public AlterarAtivoUsuarioHandler(
        IIdentityService identityService,
        IUsuarioAtualService usuarioAtualService,
        IRestauranteRepository restauranteRepository,
        IConviteRepository conviteRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IApplicationDbContext context,
        IRelogio relogio)
    {
        _identityService = identityService;
        _usuarioAtualService = usuarioAtualService;
        _restauranteRepository = restauranteRepository;
        _conviteRepository = conviteRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _context = context;
        _relogio = relogio;
    }

    public async Task Handle(AlterarAtivoUsuarioCommand request, CancellationToken cancellationToken)
    {
        var restauranteId = _usuarioAtualService.ObterRestauranteIdObrigatorio();
        var agora = _relogio.AgoraUtc;
        var usuarios = await _identityService.ListarUsuariosDoRestauranteAsync(restauranteId, cancellationToken);

        var usuario = usuarios.FirstOrDefault(u => u.Id == request.UsuarioId)
            ?? throw new NaoEncontradoException($"Usuário com Id '{request.UsuarioId}' não encontrado.");

        if (usuario.Ativo == request.Ativo)
            return;

        if (!request.Ativo)
        {
            if (usuario.Role == RoleNames.Admin)
                throw new DomainException("Rebaixe esta pessoa para Funcionário antes de desativá-la.");

            await _identityService.DefinirAtivoAsync(usuario.Id, false, cancellationToken);

            await SessoesUsuario.EncerrarTodasAsync(_refreshTokenRepository, usuario.Id, agora, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        var restaurante = await _restauranteRepository.ObterPorIdAsync(restauranteId, cancellationToken)
            ?? throw new NaoEncontradoException("Restaurante não encontrado.");
        var ativos = await _identityService.ContarUsuariosDoRestauranteAsync(restauranteId, cancellationToken);
        var pendentes = await _conviteRepository.ContarPendentesAsync(agora, null, cancellationToken);
        restaurante.GarantirQueCabeMaisUmUsuario(ativos + pendentes);

        await _identityService.DefinirAtivoAsync(usuario.Id, true, cancellationToken);
    }
}
