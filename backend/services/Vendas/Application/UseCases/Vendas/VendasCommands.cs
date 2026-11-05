using MediatR;
using GestaoRestaurante.Core.Mensageria.Contratos;
using GestaoRestaurante.Vendas.Contracts.Vendas;

namespace GestaoRestaurante.Vendas.Application.UseCases.Vendas;

public record CriarVendaCommand(IReadOnlyList<ItemVendaRequest> Itens, string TokenCartao) : IRequest<Guid>;

public record ProcessarResultadoBaixaCommand(Guid MensagemId, ResultadoBaixaEstoqueMensagem Resultado) : IRequest;

public record ProcessarResultadoPagamentoCommand(Guid MensagemId, ResultadoPagamentoMensagem Resultado) : IRequest;
