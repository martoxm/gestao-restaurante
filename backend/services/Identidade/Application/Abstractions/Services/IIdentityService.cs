namespace GestaoRestaurante.Identidade.Application.Abstractions.Services;

public record UsuarioAutenticado(Guid Id, string Nome, string Email, Guid? RestauranteId, bool EmailConfirmado = true);

public record UsuarioComRole(Guid Id, string Nome, string Email, string Role, bool Ativo = true);

public record UsuarioDaPlataforma(Guid Id, string Email, Guid? RestauranteId, bool Ativo, IReadOnlyList<string> Roles);

public interface IIdentityService
{
    Task<bool> EmailExisteAsync(string email, CancellationToken cancellationToken);

    Task<Guid> CriarUsuarioAsync(string nome, string email, string senha, Guid restauranteId, string role, bool emailConfirmado, CancellationToken cancellationToken);

    Task<UsuarioAutenticado?> ValidarCredenciaisAsync(string email, string senha, CancellationToken cancellationToken);

    Task<UsuarioAutenticado?> ObterUsuarioPorIdAsync(Guid usuarioId, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ObterRolesAsync(Guid usuarioId, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ListarEmailsAdminsAsync(Guid restauranteId, CancellationToken cancellationToken);

    Task<int> ContarUsuariosDoRestauranteAsync(Guid restauranteId, CancellationToken cancellationToken);

    Task<IReadOnlyList<UsuarioComRole>> ListarUsuariosDoRestauranteAsync(Guid restauranteId, CancellationToken cancellationToken);


    Task DefinirAtivoAsync(Guid usuarioId, bool ativo, CancellationToken cancellationToken);

    Task<(Guid UsuarioId, string Token)?> GerarTokenRedefinicaoSenhaAsync(string email, CancellationToken cancellationToken);

    Task<Guid> RedefinirSenhaAsync(string email, string token, string novaSenha, CancellationToken cancellationToken);

    Task<string> GerarTokenConfirmacaoEmailAsync(Guid usuarioId, CancellationToken cancellationToken);

    Task<bool> ConfirmarEmailAsync(Guid usuarioId, string token, CancellationToken cancellationToken);

    Task DefinirRoleAsync(Guid usuarioId, string role, CancellationToken cancellationToken);

    Task<UsuarioDaPlataforma?> ObterUsuarioParaPlataformaAsync(Guid usuarioId, CancellationToken cancellationToken);
}
