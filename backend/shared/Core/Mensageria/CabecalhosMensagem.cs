using RabbitMQ.Client;

namespace GestaoRestaurante.Core.Mensageria;

public static class CabecalhosMensagem
{
    public const string TraceParent = "traceparent";

    public static string? LerTexto(IReadOnlyBasicProperties propriedades, string nome)
    {
        if (propriedades.Headers is null || !propriedades.Headers.TryGetValue(nome, out var valor))
            return null;

        var texto = valor switch
        {
            byte[] bytes => System.Text.Encoding.UTF8.GetString(bytes),
            string str => str,
            _ => null
        };
        return string.IsNullOrWhiteSpace(texto) ? null : texto;
    }
}
