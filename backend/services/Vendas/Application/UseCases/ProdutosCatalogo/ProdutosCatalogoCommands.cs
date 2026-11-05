using MediatR;
using GestaoRestaurante.Core.Mensageria.Contratos;

namespace GestaoRestaurante.Vendas.Application.UseCases.ProdutosCatalogo;

public record SincronizarProdutoCatalogoCommand(ProdutoCatalogoMensagem Produto) : IRequest;
