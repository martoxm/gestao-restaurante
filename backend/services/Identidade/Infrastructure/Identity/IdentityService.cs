using GestaoRestaurante.Core.Application.Exceptions;
using GestaoRestaurante.Core.Seguranca;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GestaoRestaurante.Identidade.Application.Abstractions.Services;

namespace GestaoRestaurante.Identidade.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> EmailExisteAsync(string email, CancellationToken cancellationToken)
    {
        var usuario = await _userManager.FindByEmailAsync(email);
        return usuario is not null;
    }

    public async Task<Guid> CriarUsuarioAsync(string nome, string email, string senha, Guid restauranteId, string role, bool emailConfirmado, CancellationToken cancellationToken)
    {
        var usuario = new ApplicationUser
        {
            EmailConfirmed = emailConfirmado,
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            Nome = nome,
            RestauranteId = restauranteId
        };

        var result = await _userManager.CreateAsync(usuario, senha);

        if (!result.Succeeded)
        {
            var failures = result.Errors
                .Select(error => new ValidationFailure(CampoDoErro(error.Code), error.Description));

            throw new ValidationException(failures);
        }

        await _userManager.AddToRoleAsync(usuario, role);

        return usuario.Id;
    }

    private static string CampoDoErro(string codigo)
    {
        if (codigo.StartsWith("Password", StringComparison.Ordinal))
            return "Senha";

        if (codigo.EndsWith("Email", StringComparison.Ordinal) || codigo.EndsWith("UserName", StringComparison.Ordinal))
            return "Email";

        return string.Empty;
    }

    public async Task<UsuarioAutenticado?> ValidarCredenciaisAsync(string email, string senha, CancellationToken cancellationToken)
    {
        var usuario = await _userManager.FindByEmailAsync(email);

        if (usuario is null || !await _userManager.CheckPasswordAsync(usuario, senha))
            return null;

        if (EstaDesativado(usuario))
            throw new SessaoInvalidaException("Seu acesso foi desativado. Fale com o administrador do restaurante.");

        return ParaUsuarioAutenticado(usuario);
    }

    public async Task<UsuarioAutenticado?> ObterUsuarioPorIdAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await _userManager.FindByIdAsync(usuarioId.ToString());

        return usuario is null || EstaDesativado(usuario)
            ? null
            : ParaUsuarioAutenticado(usuario);
    }

    private static UsuarioAutenticado ParaUsuarioAutenticado(ApplicationUser usuario) =>
        new(usuario.Id, usuario.Nome, usuario.Email!, usuario.RestauranteId, usuario.EmailConfirmed);

    private static bool EstaDesativado(ApplicationUser usuario) =>
        usuario.LockoutEnd.HasValue && usuario.LockoutEnd.Value > DateTimeOffset.UtcNow;

    public async Task<IReadOnlyList<string>> ObterRolesAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await _userManager.FindByIdAsync(usuarioId.ToString());

        if (usuario is null)
            return [];

        var roles = await _userManager.GetRolesAsync(usuario);
        return roles.ToList();
    }

    public async Task<IReadOnlyList<string>> ListarEmailsAdminsAsync(Guid restauranteId, CancellationToken cancellationToken)
    {
        var admins = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);

        return admins
            .Where(usuario => usuario.RestauranteId == restauranteId && !string.IsNullOrWhiteSpace(usuario.Email))
            .Select(usuario => usuario.Email!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<int> ContarUsuariosDoRestauranteAsync(Guid restauranteId, CancellationToken cancellationToken)
    {
        var agora = DateTimeOffset.UtcNow;
        return await _userManager.Users.CountAsync(usuario => usuario.RestauranteId == restauranteId
            && (usuario.LockoutEnd == null || usuario.LockoutEnd <= agora), cancellationToken);
    }

    public async Task<IReadOnlyList<UsuarioComRole>> ListarUsuariosDoRestauranteAsync(Guid restauranteId, CancellationToken cancellationToken)
    {
        var usuarios = await _userManager.Users
            .Where(usuario => usuario.RestauranteId == restauranteId)
            .ToListAsync(cancellationToken);

        var idsAdmins = (await _userManager.GetUsersInRoleAsync(RoleNames.Admin))
            .Select(usuario => usuario.Id)
            .ToHashSet();

        return usuarios
            .Select(usuario => new UsuarioComRole(
                usuario.Id,
                usuario.Nome,
                usuario.Email!,
                idsAdmins.Contains(usuario.Id) ? RoleNames.Admin : RoleNames.Funcionario,
                !EstaDesativado(usuario)))
            .ToList();
    }

    public async Task DefinirAtivoAsync(Guid usuarioId, bool ativo, CancellationToken cancellationToken)
    {
        var usuario = await _userManager.FindByIdAsync(usuarioId.ToString())
            ?? throw new InvalidOperationException($"Usuário '{usuarioId}' não existe.");

        await _userManager.SetLockoutEnabledAsync(usuario, true);
        await _userManager.SetLockoutEndDateAsync(usuario, ativo ? null : DateTimeOffset.MaxValue);
    }

    public async Task<(Guid UsuarioId, string Token)?> GerarTokenRedefinicaoSenhaAsync(string email, CancellationToken cancellationToken)
    {
        var usuario = await _userManager.FindByEmailAsync(email);
        if (usuario is null || EstaDesativado(usuario))
            return null;

        return (usuario.Id, await _userManager.GeneratePasswordResetTokenAsync(usuario));
    }

    public async Task<Guid> RedefinirSenhaAsync(string email, string token, string novaSenha, CancellationToken cancellationToken)
    {
        const string linkInvalido = "Este link de redefinição é inválido ou já venceu. Peça um novo.";

        var usuario = await _userManager.FindByEmailAsync(email);
        if (usuario is null || EstaDesativado(usuario))
            throw new ValidationException([new ValidationFailure(string.Empty, linkInvalido)]);

        var result = await _userManager.ResetPasswordAsync(usuario, token, novaSenha);
        if (!result.Succeeded)
        {
            var failures = result.Errors.Select(error => error.Code == "InvalidToken"
                ? new ValidationFailure(string.Empty, linkInvalido)
                : new ValidationFailure(CampoDoErro(error.Code) == "Senha" ? "NovaSenha" : string.Empty, error.Description));
            throw new ValidationException(failures);
        }

        if (!usuario.EmailConfirmed)
        {
            usuario.EmailConfirmed = true;
            await _userManager.UpdateAsync(usuario);
        }

        return usuario.Id;
    }

    public async Task<string> GerarTokenConfirmacaoEmailAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await _userManager.FindByIdAsync(usuarioId.ToString())
            ?? throw new InvalidOperationException($"Usuário '{usuarioId}' não existe.");

        return await _userManager.GenerateEmailConfirmationTokenAsync(usuario);
    }

    public async Task<bool> ConfirmarEmailAsync(Guid usuarioId, string token, CancellationToken cancellationToken)
    {
        var usuario = await _userManager.FindByIdAsync(usuarioId.ToString());
        if (usuario is null)
            return false;

        if (usuario.EmailConfirmed)
            return true;

        return (await _userManager.ConfirmEmailAsync(usuario, token)).Succeeded;
    }

    public async Task<UsuarioDaPlataforma?> ObterUsuarioParaPlataformaAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await _userManager.FindByIdAsync(usuarioId.ToString());
        if (usuario is null)
            return null;

        var roles = await _userManager.GetRolesAsync(usuario);
        return new UsuarioDaPlataforma(usuario.Id, usuario.Email!, usuario.RestauranteId, !EstaDesativado(usuario), roles.ToList());
    }

    public async Task DefinirRoleAsync(Guid usuarioId, string role, CancellationToken cancellationToken)
    {
        var usuario = await _userManager.FindByIdAsync(usuarioId.ToString())
            ?? throw new InvalidOperationException($"Usuário '{usuarioId}' não existe.");

        var rolesAtuais = await _userManager.GetRolesAsync(usuario);
        var remover = rolesAtuais.Where(atual => atual != role).ToList();
        if (remover.Count > 0)
            await _userManager.RemoveFromRolesAsync(usuario, remover);

        if (!rolesAtuais.Contains(role))
            await _userManager.AddToRoleAsync(usuario, role);
    }
}
