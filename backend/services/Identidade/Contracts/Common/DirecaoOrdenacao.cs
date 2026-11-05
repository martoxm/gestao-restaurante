using System.Text.Json.Serialization;

namespace GestaoRestaurante.Identidade.Contracts.Common;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DirecaoOrdenacao
{
    Asc,
    Desc
}
