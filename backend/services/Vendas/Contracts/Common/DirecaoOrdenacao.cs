using System.Text.Json.Serialization;

namespace GestaoRestaurante.Vendas.Contracts.Common;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DirecaoOrdenacao
{
    Asc,
    Desc
}
