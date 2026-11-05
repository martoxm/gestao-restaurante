using Microsoft.Extensions.Options;
using GestaoRestaurante.Identidade.Application.Abstractions.Services;

namespace GestaoRestaurante.Identidade.Infrastructure.Links;

public class FrontSettings
{
    public const string SectionName = "Front";

    public string Url { get; set; } = "http://localhost:3000";
}

public class LinksFront : ILinksFront
{
    private readonly string _urlBase;

    public LinksFront(IOptions<FrontSettings> settings)
    {
        _urlBase = settings.Value.Url.TrimEnd('/');
    }

    public string AceitarConvite(string token) =>
        $"{_urlBase}/aceitar-convite?token={Uri.EscapeDataString(token)}";

    public string RedefinirSenha(string email, string token) =>
        $"{_urlBase}/redefinir-senha?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

    public string ConfirmarEmail(Guid usuarioId, string token) =>
        $"{_urlBase}/confirmar-email?usuarioId={usuarioId}&token={Uri.EscapeDataString(token)}";

    public string Assinatura() => $"{_urlBase}/assinatura";
}
