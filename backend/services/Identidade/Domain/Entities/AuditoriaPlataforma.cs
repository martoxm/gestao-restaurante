using GestaoRestaurante.Core.Domain;

namespace GestaoRestaurante.Identidade.Domain.Entities;

public class AuditoriaPlataforma : Entity
{
    public Guid SuperAdminId { get; private set; }
    public string EmailSuperAdmin { get; private set; } = string.Empty;
    public string Acao { get; private set; } = string.Empty;
    public Guid? RestauranteId { get; private set; }
    public Guid? UsuarioAlvoId { get; private set; }
    public string? ValorAntes { get; private set; }
    public string? ValorDepois { get; private set; }
    public DateTime OcorridoEm { get; private set; }

    private AuditoriaPlataforma()
    {
    }

    public static AuditoriaPlataforma Registrar(
        Guid superAdminId,
        string emailSuperAdmin,
        string acao,
        DateTime ocorridoEm,
        Guid? restauranteId = null,
        Guid? usuarioAlvoId = null,
        string? valorAntes = null,
        string? valorDepois = null)
    {
        if (superAdminId == Guid.Empty)
            throw new DomainException("Quem fez a ação é obrigatório na auditoria.");

        if (string.IsNullOrWhiteSpace(emailSuperAdmin))
            throw new DomainException("O e-mail de quem fez a ação é obrigatório na auditoria.");

        if (string.IsNullOrWhiteSpace(acao))
            throw new DomainException("A ação é obrigatória na auditoria.");

        if (restauranteId is null && usuarioAlvoId is null)
            throw new DomainException("A auditoria precisa dizer em qual restaurante ou usuário a ação foi feita.");

        return new AuditoriaPlataforma
        {
            SuperAdminId = superAdminId,
            EmailSuperAdmin = emailSuperAdmin.Trim(),
            Acao = acao.Trim(),
            RestauranteId = restauranteId,
            UsuarioAlvoId = usuarioAlvoId,
            ValorAntes = valorAntes,
            ValorDepois = valorDepois,
            OcorridoEm = ocorridoEm
        };
    }
}
