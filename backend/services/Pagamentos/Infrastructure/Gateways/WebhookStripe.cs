using GestaoRestaurante.Pagamentos.Application.UseCases.Assinaturas;
using GestaoRestaurante.Pagamentos.Application.UseCases.Pagamentos;
using Stripe.Checkout;
using Stripe;

namespace GestaoRestaurante.Pagamentos.Infrastructure.Gateways;

public static class WebhookStripe
{
    public const string NomeCabecalho = "Stripe-Signature";

    public static ResultadoWebhookStripe Interpretar(string corpo, string? assinatura, string segredo)
    {
        if (string.IsNullOrWhiteSpace(segredo) || string.IsNullOrWhiteSpace(assinatura))
            return ResultadoWebhookStripe.AssinaturaInvalida();

        Event evento;
        try
        {
            evento = EventUtility.ConstructEvent(corpo, assinatura, segredo, throwOnApiVersionMismatch: false);
        }
        catch (StripeException)
        {
            return ResultadoWebhookStripe.AssinaturaInvalida();
        }

        if (evento.Type == EventTypes.CheckoutSessionCompleted)
        {
            if (evento.Data.Object is Session sessao
                && sessao.PaymentStatus == "paid"
                && sessao.Metadata.ContainsKey("cobrancaId"))
                return ResultadoWebhookStripe.ConfirmarPlano(new ConfirmarPagamentoPlanoCommand(sessao.Id));

            return ResultadoWebhookStripe.Ignorado();
        }

        var aprovado = evento.Type switch
        {
            EventTypes.PaymentIntentSucceeded => true,
            EventTypes.PaymentIntentPaymentFailed => false,
            _ => (bool?)null
        };

        if (aprovado is null || evento.Data.Object is not PaymentIntent intent)
            return ResultadoWebhookStripe.Ignorado();

        if (!intent.Metadata.TryGetValue(MetadadosStripe.PagamentoId, out var pagamentoId) ||
            !intent.Metadata.TryGetValue(MetadadosStripe.RestauranteId, out var restauranteId) ||
            !Guid.TryParse(pagamentoId, out var idPagamento) ||
            !Guid.TryParse(restauranteId, out var idRestaurante))
            return ResultadoWebhookStripe.Ignorado();

        return ResultadoWebhookStripe.Processar(new ProcessarWebhookGatewayCommand(
            idPagamento,
            idRestaurante,
            intent.Id,
            aprovado.Value,
            aprovado.Value ? null : intent.LastPaymentError?.Message ?? "Pagamento recusado pelo Stripe."));
    }
}

public record ResultadoWebhookStripe(bool AssinaturaValida, ProcessarWebhookGatewayCommand? Comando, ConfirmarPagamentoPlanoCommand? ConfirmacaoPlano = null)
{
    public static ResultadoWebhookStripe AssinaturaInvalida() => new(false, null);
    public static ResultadoWebhookStripe Ignorado() => new(true, null);
    public static ResultadoWebhookStripe Processar(ProcessarWebhookGatewayCommand comando) => new(true, comando);
    public static ResultadoWebhookStripe ConfirmarPlano(ConfirmarPagamentoPlanoCommand comando) => new(true, null, comando);
}
