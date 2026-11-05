using GestaoRestaurante.Core.Application.Abstractions;
using GestaoRestaurante.Identidade.Application.Abstractions.Services;

namespace GestaoRestaurante.Identidade.Tests.Common;

public class FakeRelogio : IRelogio
{
    public DateTime AgoraUtc { get; set; } = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
}
