using MediatR;

namespace GestaoRestaurante.Identidade.Application.UseCases.Restaurantes;

public record AtualizarRestauranteCommand(Guid Id, string Nome) : IRequest;

public record AvisarFimDoTesteCommand : IRequest<int>;

public record AplicarPagamentoPlanoCommand(GestaoRestaurante.Core.Mensageria.Contratos.PlanoPagoMensagem Pagamento) : IRequest;
