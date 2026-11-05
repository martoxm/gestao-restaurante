using MediatR;
using GestaoRestaurante.Core.Application.Models;
using GestaoRestaurante.Vendas.Contracts.Common;
using GestaoRestaurante.Vendas.Contracts.Vendas;

namespace GestaoRestaurante.Vendas.Application.UseCases.Vendas;

public record ObterVendaPorIdQuery(Guid Id) : IRequest<VendaDetalhesDto?>;

public record ListarVendasQuery(
    int Pagina = 1,
    int TamanhoPagina = 10,
    DirecaoOrdenacao DirecaoOrdenacao = DirecaoOrdenacao.Desc) : IRequest<ResultadoPaginado<VendaItemListaDto>>;
