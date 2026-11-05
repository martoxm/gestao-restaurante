using MediatR;
using GestaoRestaurante.Core.Mensageria.Contratos;
using GestaoRestaurante.Domain.Enums;

namespace GestaoRestaurante.Application.UseCases.MovimentacoesEstoque;

public record CriarMovimentacaoEstoqueCommand(
    Guid ProdutoId,
    TipoMovimentacaoEstoque Tipo,
    int Quantidade,
    string? Observacoes = null) : IRequest<Guid>;

public record BaixarEstoqueVendaCommand(
    Guid MensagemId,
    Guid VendaId,
    Guid RestauranteId,
    IReadOnlyList<ItemBaixaEstoque> Itens,
    int? NumeroVenda = null) : IRequest;

public record DevolverEstoqueVendaCommand(
    Guid MensagemId,
    Guid VendaId,
    Guid RestauranteId,
    IReadOnlyList<ItemBaixaEstoque> Itens,
    string Motivo,
    int? NumeroVenda = null) : IRequest;
