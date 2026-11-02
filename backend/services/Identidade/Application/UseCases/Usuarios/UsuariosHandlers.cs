using MediatR;
using RestauranteEstoque.Core.Application.Abstractions;
using RestauranteEstoque.Core.Application.Exceptions;
using RestauranteEstoque.Core.Domain;
using RestauranteEstoque.Core.Seguranca;
using RestauranteEstoque.Identidade.Application.Abstractions.Services;
using RestauranteEstoque.Identidade.Contracts.Usuarios;

namespace RestauranteEstoque.Identidade.Application.UseCases.Usuarios;

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
            .Select(usuario => new UsuarioItemListaDto(usuario.Id, usuario.Nome, usuario.Email, usuario.Role))
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
