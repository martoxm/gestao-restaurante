using System.Text.Json.Serialization;

namespace GestaoRestaurante.Contracts.Common;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DirecaoOrdenacao
{
    Asc,
    Desc
}
