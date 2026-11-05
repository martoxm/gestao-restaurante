using MediatR;
using GestaoRestaurante.Identidade.Contracts.Usuarios;

namespace GestaoRestaurante.Identidade.Application.UseCases.Usuarios;

public record ListarUsuariosQuery : IRequest<IReadOnlyList<UsuarioItemListaDto>>;
