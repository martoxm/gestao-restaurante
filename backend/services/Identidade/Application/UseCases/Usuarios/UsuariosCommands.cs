using MediatR;

namespace GestaoRestaurante.Identidade.Application.UseCases.Usuarios;

public record AlterarRoleUsuarioCommand(Guid UsuarioId, string Role) : IRequest;

public record AlterarAtivoUsuarioCommand(Guid UsuarioId, bool Ativo) : IRequest;
