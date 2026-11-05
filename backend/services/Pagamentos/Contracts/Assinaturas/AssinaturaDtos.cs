namespace GestaoRestaurante.Pagamentos.Contracts.Assinaturas;

public record PlanoAVendaDto(string Plano, string Nome, decimal Preco, int Dias);

public record IniciarAssinaturaRequest(string Plano);

public record CheckoutAssinaturaResponse(Guid CobrancaId, string UrlPagamento);
