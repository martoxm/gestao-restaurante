using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace GestaoRestaurante.Core.Seguranca;

public static class BloqueioRestauranteExtensions
{
    public const string ClaimRestauranteLiberado = "restauranteLiberado";

    private static readonly HashSet<string> MetodosDeLeitura =
        new(StringComparer.OrdinalIgnoreCase) { "GET", "HEAD", "OPTIONS" };

    public static IApplicationBuilder UseBloqueioRestauranteNaoLiberado(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            if (DeveBloquear(context))
            {
                context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
                await context.Response.WriteAsJsonAsync(new
                {
                    title = "Restaurante sem plano ativo",
                    status = StatusCodes.Status402PaymentRequired,
                    detail = "O período de teste do restaurante terminou ou o acesso foi suspenso. " +
                             "Você ainda pode consultar os dados, mas não pode criar, alterar ou excluir nada."
                }, options: null, contentType: "application/problem+json");
                return;
            }

            await next();
        });
    }

    private static bool DeveBloquear(HttpContext context)
    {
        if (MetodosDeLeitura.Contains(context.Request.Method))
            return false;

        if (context.Request.Path.Value?.Contains("/auth/", StringComparison.OrdinalIgnoreCase) == true)
            return false;

        if (context.Request.Path.Value?.Contains("/assinaturas/", StringComparison.OrdinalIgnoreCase) == true)
            return false;

        var claim = context.User.FindFirst(ClaimRestauranteLiberado)?.Value;
        return context.User.Identity?.IsAuthenticated == true
            && string.Equals(claim, "false", StringComparison.OrdinalIgnoreCase);
    }
}
