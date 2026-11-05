namespace GestaoRestaurante.Identidade.Contracts.Convites;

public record CriarConviteRequest(string Email, string Role);

public record ConviteCriadoResponse(Guid Id, DateTime ExpiraEm);

public record ConviteItemListaDto(Guid Id, string Email, string Role, string Status, DateTime CriadoEm, DateTime ExpiraEm);

public record ConvitePublicoDto(string Email, string NomeRestaurante, string Role, DateTime ExpiraEm);
