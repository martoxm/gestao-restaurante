namespace GestaoRestaurante.Core.Mensageria.Contratos;

public sealed record PlanoPagoMensagem(Guid CobrancaId, Guid RestauranteId, string Plano, int Dias, DateTime PagoEm);
