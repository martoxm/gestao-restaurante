namespace GestaoRestaurante.Core.Mensageria.Contratos;

public sealed record AvisarAdminsRestauranteMensagem(Guid RestauranteId, string Assunto, string CorpoHtml);
