using GestaoRestaurante.Identidade.Application.Abstractions.Services;

namespace GestaoRestaurante.Identidade.Tests.Common;

public class FakeIdentityService : IIdentityService
{
    public Exception? ErroAoCriar { get; init; }

    public Dictionary<Guid, UsuarioAutenticado> Usuarios { get; } = [];

    public Dictionary<Guid, string> Roles { get; } = [];

    public Task<Guid> CriarUsuarioAsync(string nome, string email, string senha, Guid restauranteId, string role, bool emailConfirmado, CancellationToken cancellationToken)
    {
        if (ErroAoCriar is not null)
            return Task.FromException<Guid>(ErroAoCriar);

        var id = Guid.NewGuid();
        Usuarios[id] = new UsuarioAutenticado(id, nome, email, restauranteId, emailConfirmado);
        Roles[id] = role;
        return Task.FromResult(id);
    }

    public Task<UsuarioAutenticado?> ObterUsuarioPorIdAsync(Guid usuarioId, CancellationToken cancellationToken) =>
        Task.FromResult(Usuarios.GetValueOrDefault(usuarioId));

    public Task<UsuarioDaPlataforma?> ObterUsuarioParaPlataformaAsync(Guid usuarioId, CancellationToken cancellationToken) =>
        Task.FromResult(Usuarios.TryGetValue(usuarioId, out var u)
            ? new UsuarioDaPlataforma(u.Id, u.Email, u.RestauranteId, !Desativados.Contains(u.Id), [Roles.GetValueOrDefault(u.Id, "Funcionario")])
            : null);

    public Task<IReadOnlyList<string>> ObterRolesAsync(Guid usuarioId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>([Roles.GetValueOrDefault(usuarioId, "Funcionario")]);

    public Task<bool> EmailExisteAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(Usuarios.Values.Any(usuario => usuario.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

    public Task<UsuarioAutenticado?> ValidarCredenciaisAsync(string email, string senha, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Dictionary<Guid, IReadOnlyList<string>> EmailsAdmins { get; } = [];

    public Task<IReadOnlyList<string>> ListarEmailsAdminsAsync(Guid restauranteId, CancellationToken cancellationToken)
        => Task.FromResult(EmailsAdmins.TryGetValue(restauranteId, out var emails) ? emails : (IReadOnlyList<string>)[]);
    public Task<int> ContarUsuariosDoRestauranteAsync(Guid restauranteId, CancellationToken cancellationToken) =>
        Task.FromResult(
            Usuarios.Values.Count(usuario => usuario.RestauranteId == restauranteId && !Desativados.Contains(usuario.Id))
            + (UsuariosPorRestaurante.TryGetValue(restauranteId, out var lista) ? lista.Count(u => u.Ativo) : 0));

    public Dictionary<Guid, List<UsuarioComRole>> UsuariosPorRestaurante { get; } = [];

    public Task<IReadOnlyList<UsuarioComRole>> ListarUsuariosDoRestauranteAsync(Guid restauranteId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<UsuarioComRole>>(
            UsuariosPorRestaurante.TryGetValue(restauranteId, out var lista) ? lista.ToList() : []);

    public HashSet<Guid> Desativados { get; } = [];

    public Dictionary<Guid, string> SenhasRedefinidas { get; } = [];

    public Task DefinirAtivoAsync(Guid usuarioId, bool ativo, CancellationToken cancellationToken)
    {
        if (ativo) Desativados.Remove(usuarioId); else Desativados.Add(usuarioId);
        foreach (var lista in UsuariosPorRestaurante.Values)
        {
            var indice = lista.FindIndex(u => u.Id == usuarioId);
            if (indice >= 0)
                lista[indice] = lista[indice] with { Ativo = ativo };
        }
        return Task.CompletedTask;
    }

    public Task<(Guid UsuarioId, string Token)?> GerarTokenRedefinicaoSenhaAsync(string email, CancellationToken cancellationToken)
    {
        var usuario = Usuarios.Values.FirstOrDefault(u => u.Email == email && !Desativados.Contains(u.Id));
        return Task.FromResult<(Guid, string)?>(usuario is null ? null : (usuario.Id, $"tok-{usuario.Id}"));
    }

    public Task<Guid> RedefinirSenhaAsync(string email, string token, string novaSenha, CancellationToken cancellationToken)
    {
        var usuario = Usuarios.Values.First(u => u.Email == email);
        if (token != $"tok-{usuario.Id}")
            throw new FluentValidation.ValidationException("Link inválido.");
        SenhasRedefinidas[usuario.Id] = novaSenha;
        return Task.FromResult(usuario.Id);
    }

    public Task<string> GerarTokenConfirmacaoEmailAsync(Guid usuarioId, CancellationToken cancellationToken) =>
        Task.FromResult($"conf-{usuarioId}");

    public Task<bool> ConfirmarEmailAsync(Guid usuarioId, string token, CancellationToken cancellationToken)
    {
        if (token != $"conf-{usuarioId}" || !Usuarios.TryGetValue(usuarioId, out var usuario))
            return Task.FromResult(false);
        Usuarios[usuarioId] = usuario with { EmailConfirmado = true };
        return Task.FromResult(true);
    }

    public Task DefinirRoleAsync(Guid usuarioId, string role, CancellationToken cancellationToken)
    {
        foreach (var lista in UsuariosPorRestaurante.Values)
        {
            var indice = lista.FindIndex(u => u.Id == usuarioId);
            if (indice >= 0)
                lista[indice] = lista[indice] with { Role = role };
        }
        return Task.CompletedTask;
    }
}

public class FakeJwtTokenGenerator : IJwtTokenGenerator
{
    public bool? UltimoRestauranteLiberado { get; private set; }

    public string GerarToken(Guid usuarioId, string nome, string email, IEnumerable<string> roles, Guid? restauranteId, bool restauranteLiberado = true, bool emailConfirmado = true)
    {
        UltimoRestauranteLiberado = restauranteLiberado;
        return "token-falso";
    }
}

public class FakeGeradorRefreshToken : IGeradorRefreshToken
{
    private int _contador;

    public int DiasValidade => 14;

    public RefreshTokenGerado Gerar()
    {
        var token = $"rt-{++_contador}";
        return new RefreshTokenGerado(token, CalcularHash(token));
    }

    public string CalcularHash(string token) => $"hash-{token}";
}
