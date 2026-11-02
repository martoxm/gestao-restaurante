namespace RestauranteEstoque.Identidade.Application.Abstractions.Services;

public record UsuarioAutenticado(Guid Id, string Nome, string Email, Guid? RestauranteId);

public record UsuarioComRole(Guid Id, string Nome, string Email, string Role);

public interface IIdentityService
{
    Task<bool> EmailExisteAsync(string email, CancellationToken cancellationToken);

    Task<Guid> CriarUsuarioAsync(string nome, string email, string senha, Guid restauranteId, CancellationToken cancellationToken);

    Task<UsuarioAutenticado?> ValidarCredenciaisAsync(string email, string senha, CancellationToken cancellationToken);

    Task<UsuarioAutenticado?> ObterUsuarioPorIdAsync(Guid usuarioId, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ObterRolesAsync(Guid usuarioId, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ListarEmailsAdminsAsync(Guid restauranteId, CancellationToken cancellationToken);

    Task<IReadOnlyList<UsuarioComRole>> ListarUsuariosDoRestauranteAsync(Guid restauranteId, CancellationToken cancellationToken);

    Task DefinirRoleAsync(Guid usuarioId, string role, CancellationToken cancellationToken);
}
