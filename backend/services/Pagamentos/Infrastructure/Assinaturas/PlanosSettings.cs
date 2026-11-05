using GestaoRestaurante.Pagamentos.Application.Abstractions.Services;
using GestaoRestaurante.Pagamentos.Domain.Enums;

namespace GestaoRestaurante.Pagamentos.Infrastructure.Assinaturas;

public class PlanosSettings : ITabelaPlanos
{
    public const string SectionName = "Planos";

    public decimal Basico { get; set; } = 99m;
    public decimal Pro { get; set; } = 199m;

    public IReadOnlyList<PlanoAVenda> Listar() =>
    [
        new(PlanoPago.Basico, "Básico", Basico),
        new(PlanoPago.Pro, "Pro", Pro)
    ];
}

public class FrontSettings
{
    public const string SectionName = "Front";

    public string Url { get; set; } = "http://localhost:3000";

    public string UrlSucesso => $"{Url.TrimEnd('/')}/assinatura?status=sucesso";
    public string UrlCancelado => $"{Url.TrimEnd('/')}/assinatura?status=cancelado";
}
