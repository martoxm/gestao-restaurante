using MediatR;
using GestaoRestaurante.Identidade.Contracts.Convites;

namespace GestaoRestaurante.Identidade.Application.UseCases.Convites;

public record ListarConvitesQuery : IRequest<IReadOnlyList<ConviteItemListaDto>>;

public record ObterConvitePorTokenQuery(string Token) : IRequest<ConvitePublicoDto>;
