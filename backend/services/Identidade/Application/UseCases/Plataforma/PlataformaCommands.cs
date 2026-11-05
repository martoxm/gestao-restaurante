using MediatR;
using GestaoRestaurante.Identidade.Contracts.Convites;

namespace GestaoRestaurante.Identidade.Application.UseCases.Plataforma;

public record SuperAdminAtual(Guid Id, string Email);

public record MudarPlanoRestauranteCommand(SuperAdminAtual QuemFaz, Guid RestauranteId, string Plano) : IRequest;

public record EstenderTesteRestauranteCommand(SuperAdminAtual QuemFaz, Guid RestauranteId, int Dias) : IRequest;

public record AlterarAtivoRestauranteCommand(SuperAdminAtual QuemFaz, Guid RestauranteId, bool Ativo) : IRequest;

public record ReenviarConviteCommand(SuperAdminAtual QuemFaz, Guid ConviteId) : IRequest<ConviteCriadoResponse>;

public record ForcarLogoutUsuarioCommand(SuperAdminAtual QuemFaz, Guid UsuarioId) : IRequest;

public record AlterarAtivoUsuarioPlataformaCommand(SuperAdminAtual QuemFaz, Guid UsuarioId, bool Ativo) : IRequest;

public record MarcarNotificacoesLidasCommand(Guid? Id) : IRequest<int>;

public record NotificarVencimentosCommand : IRequest<int>;
