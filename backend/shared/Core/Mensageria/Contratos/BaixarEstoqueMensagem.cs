namespace GestaoRestaurante.Core.Mensageria.Contratos;

public sealed record BaixarEstoqueMensagem(Guid VendaId, Guid RestauranteId, IReadOnlyList<ItemBaixaEstoque> Itens, int? NumeroVenda = null);

public sealed record ItemBaixaEstoque(Guid ProdutoId, int Quantidade);
