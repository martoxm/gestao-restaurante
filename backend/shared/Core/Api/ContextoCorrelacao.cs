using System.Diagnostics;

namespace GestaoRestaurante.Core.Api;

public static class ContextoCorrelacao
{
    public const string NomeFonteTraces = "GestaoRestaurante.Mensageria";
    public static readonly ActivitySource FonteTraces = new(NomeFonteTraces);

    private static readonly AsyncLocal<string?> _atual = new();

    public static string? Atual
    {
        get => _atual.Value;
        set => _atual.Value = value;
    }
}
