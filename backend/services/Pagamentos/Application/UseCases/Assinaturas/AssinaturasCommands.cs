using MediatR;
using GestaoRestaurante.Pagamentos.Contracts.Assinaturas;

namespace GestaoRestaurante.Pagamentos.Application.UseCases.Assinaturas;

public record IniciarAssinaturaCommand(string Plano) : IRequest<CheckoutAssinaturaResponse>;

public record ConfirmarPagamentoPlanoCommand(string IdSessao) : IRequest;
