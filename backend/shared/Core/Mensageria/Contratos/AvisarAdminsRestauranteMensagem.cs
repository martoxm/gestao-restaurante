namespace RestauranteEstoque.Core.Mensageria.Contratos;

public sealed record AvisarAdminsRestauranteMensagem(Guid RestauranteId, string Assunto, string CorpoHtml);
