using GestaoRestaurante.Pagamentos.Application.Abstractions.Services;

namespace GestaoRestaurante.Pagamentos.Infrastructure.Assinaturas;

public class CheckoutPlanoFalso : ICheckoutPlano
{
    private readonly FrontSettings _front;

    public CheckoutPlanoFalso(FrontSettings front)
    {
        _front = front;
    }

    public Task<SessaoCheckout> CriarSessaoAsync(PedidoCheckout pedido, CancellationToken cancellationToken) =>
        Task.FromResult(new SessaoCheckout($"cs_falso_{pedido.CobrancaId:N}", $"{_front.UrlSucesso}&simulado=1", JaPaga: true));
}
