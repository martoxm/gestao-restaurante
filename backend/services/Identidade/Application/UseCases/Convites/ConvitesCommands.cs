using MediatR;
using GestaoRestaurante.Identidade.Contracts.Auth;
using GestaoRestaurante.Identidade.Contracts.Convites;

namespace GestaoRestaurante.Identidade.Application.UseCases.Convites;

public record CriarConviteCommand(string Email, string Role) : IRequest<ConviteCriadoResponse>;

public record CancelarConviteCommand(Guid Id) : IRequest;

public record AceitarConviteCommand(string Token, string Nome, string Senha) : IRequest<LoginResponse>;
