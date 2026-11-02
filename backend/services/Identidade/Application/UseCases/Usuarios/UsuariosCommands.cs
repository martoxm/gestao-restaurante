using MediatR;

namespace RestauranteEstoque.Identidade.Application.UseCases.Usuarios;

public record AlterarRoleUsuarioCommand(Guid UsuarioId, string Role) : IRequest;
