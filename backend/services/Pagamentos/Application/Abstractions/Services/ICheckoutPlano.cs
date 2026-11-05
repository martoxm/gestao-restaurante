using GestaoRestaurante.Pagamentos.Domain.Enums;

namespace GestaoRestaurante.Pagamentos.Application.Abstractions.Services;

public interface ICheckoutPlano
{
    Task<SessaoCheckout> CriarSessaoAsync(PedidoCheckout pedido, CancellationToken cancellationToken);
}

public record PedidoCheckout(Guid CobrancaId, Guid RestauranteId, PlanoPago Plano, string NomePlano, decimal Valor, int Dias);

public record SessaoCheckout(string IdSessao, string Url, bool JaPaga = false);

public interface ITabelaPlanos
{
    IReadOnlyList<PlanoAVenda> Listar();
}

public record PlanoAVenda(PlanoPago Plano, string Nome, decimal Preco);
