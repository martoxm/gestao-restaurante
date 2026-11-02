namespace RestauranteEstoque.Identidade.Contracts.Usuarios;

public record UsuarioItemListaDto(Guid Id, string Nome, string Email, string Role);

public record AlterarRoleUsuarioRequest(string Role);
