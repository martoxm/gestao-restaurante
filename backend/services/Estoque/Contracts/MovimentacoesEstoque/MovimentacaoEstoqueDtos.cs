namespace GestaoRestaurante.Contracts.MovimentacoesEstoque;

public record MovimentacaoEstoqueCriadaResponse(Guid Id);

public record ListarMovimentacoesEstoqueRequest(
    int Pagina = 1,
    int TamanhoPagina = 10,
    Guid? ProdutoId = null,
    string? Tipo = null,
    DateTime? DataInicio = null,
    DateTime? DataFim = null);

public record MovimentacaoEstoqueItemListaDto(
    Guid Id,
    Guid ProdutoId,
    string NomeProduto,
    string Tipo,
    int Quantidade,
    DateTime DataMovimentacao,
    string? Observacoes);
