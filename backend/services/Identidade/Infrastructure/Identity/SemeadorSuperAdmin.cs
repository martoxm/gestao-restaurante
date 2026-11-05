using GestaoRestaurante.Core.Seguranca;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GestaoRestaurante.Identidade.Infrastructure.Identity;

public static class SemeadorSuperAdmin
{
    public const string SecaoConfiguracao = "SuperAdmin";

    public static async Task SemearAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        var email = configuration[$"{SecaoConfiguracao}:Email"]?.Trim();
        var senha = configuration[$"{SecaoConfiguracao}:Senha"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
        {
            logger.LogWarning("SuperAdmin não configurado (SuperAdmin:Email/SuperAdmin:Senha no user-secrets). A API sobe sem ele.");
            return;
        }

        var usuario = await userManager.FindByEmailAsync(email);

        if (usuario is null)
        {
            usuario = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                Nome = "Administrador da plataforma",
                EmailConfirmed = true,
                RestauranteId = null
            };

            var resultado = await userManager.CreateAsync(usuario, senha);
            if (!resultado.Succeeded)
            {
                logger.LogError("Não deu pra criar o SuperAdmin: {Erros}",
                    string.Join(" ", resultado.Errors.Select(erro => erro.Description)));
                return;
            }

            logger.LogInformation("SuperAdmin {Email} criado.", email);
        }
        else if (usuario.RestauranteId is not null)
        {
            logger.LogError("O e-mail {Email} já pertence a um restaurante; não vira SuperAdmin. Use outro e-mail.", email);
            return;
        }

        if (!await userManager.IsInRoleAsync(usuario, RoleNames.SuperAdmin))
            await userManager.AddToRoleAsync(usuario, RoleNames.SuperAdmin);
    }
}
