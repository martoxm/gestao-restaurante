using MediatR;
using RestauranteEstoque.Identidade.Contracts.Usuarios;

namespace RestauranteEstoque.Identidade.Application.UseCases.Usuarios;

public record ListarUsuariosQuery : IRequest<IReadOnlyList<UsuarioItemListaDto>>;
