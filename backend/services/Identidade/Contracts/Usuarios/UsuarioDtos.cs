namespace GestaoRestaurante.Identidade.Contracts.Usuarios;

public record UsuarioItemListaDto(Guid Id, string Nome, string Email, string Role, bool Ativo);

public record AlterarRoleUsuarioRequest(string Role);

public record AlterarAtivoUsuarioRequest(bool Ativo);
