using MediatR;
using GestaoRestaurante.Pagamentos.Contracts.Assinaturas;

namespace GestaoRestaurante.Pagamentos.Application.UseCases.Assinaturas;

public record ListarPlanosQuery : IRequest<IReadOnlyList<PlanoAVendaDto>>;
