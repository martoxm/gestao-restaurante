using MediatR;
using GestaoRestaurante.Pagamentos.Contracts.Pagamentos;

namespace GestaoRestaurante.Pagamentos.Application.UseCases.Pagamentos;

public record ObterPagamentoPorIdQuery(Guid Id) : IRequest<PagamentoDetalhesDto?>;

public record ObterPagamentoPorVendaQuery(Guid VendaId) : IRequest<PagamentoDetalhesDto?>;
