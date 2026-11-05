namespace GestaoRestaurante.Core.Mensageria.Contratos;

public sealed record ProdutoCatalogoMensagem(
    Guid ProdutoId,
    Guid RestauranteId,
    string Nome,
    decimal Preco,
    bool Excluido,
    DateTime OcorridoEm);
