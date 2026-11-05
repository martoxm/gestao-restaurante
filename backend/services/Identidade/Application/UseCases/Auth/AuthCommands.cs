using MediatR;
using GestaoRestaurante.Identidade.Contracts.Auth;

namespace GestaoRestaurante.Identidade.Application.UseCases.Auth;

public record LoginCommand(string Email, string Senha) : IRequest<LoginResponse>;

public record RegistrarCommand(string Nome, string Email, string Senha, string NomeRestaurante) : IRequest<LoginResponse>;

public record RenovarTokenCommand(string RefreshToken) : IRequest<LoginResponse>;


public record EsqueciSenhaCommand(string Email) : IRequest;

public record RedefinirSenhaCommand(string Email, string Token, string NovaSenha) : IRequest;

public record ConfirmarEmailCommand(Guid UsuarioId, string Token) : IRequest;

public record ReenviarConfirmacaoEmailCommand(Guid UsuarioId) : IRequest;
