using GestaoRestaurante.Pagamentos.Application.Abstractions.Services;
using Stripe;
using Stripe.Checkout;

namespace GestaoRestaurante.Pagamentos.Infrastructure.Assinaturas;

public class CheckoutPlanoStripe : ICheckoutPlano
{
    private readonly SessionService _sessoes;
    private readonly FrontSettings _front;

    public CheckoutPlanoStripe(StripeClient client, FrontSettings front)
    {
        _sessoes = new SessionService(client);
        _front = front;
    }

    public async Task<SessaoCheckout> CriarSessaoAsync(PedidoCheckout pedido, CancellationToken cancellationToken)
    {
        var metadados = new Dictionary<string, string>
        {
            ["cobrancaId"] = pedido.CobrancaId.ToString(),
            ["restauranteId"] = pedido.RestauranteId.ToString(),
            ["plano"] = pedido.Plano.ToString()
        };

        var opcoes = new SessionCreateOptions
        {
            Mode = "payment",
            ClientReferenceId = pedido.CobrancaId.ToString(),
            SuccessUrl = _front.UrlSucesso,
            CancelUrl = _front.UrlCancelado,
            Metadata = metadados,
            PaymentIntentData = new SessionPaymentIntentDataOptions { Metadata = metadados },
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "brl",
                        UnitAmount = (long)Math.Round(pedido.Valor * 100m, MidpointRounding.AwayFromZero),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Plano {pedido.NomePlano} - {pedido.Dias} dias"
                        }
                    }
                }
            ]
        };

        var sessao = await _sessoes.CreateAsync(
            opcoes,
            new RequestOptions { IdempotencyKey = $"cobranca-plano-{pedido.CobrancaId}" },
            cancellationToken);

        return new SessaoCheckout(sessao.Id, sessao.Url);
    }
}
