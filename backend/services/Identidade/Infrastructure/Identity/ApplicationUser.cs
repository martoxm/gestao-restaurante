using Microsoft.AspNetCore.Identity;

namespace GestaoRestaurante.Identidade.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string Nome { get; set; } = string.Empty;

    public Guid? RestauranteId { get; set; }
}
