using MediatR;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Core.Mensageria.Contratos;
using GestaoRestaurante.Vendas.Application.UseCases.ProdutosCatalogo;

namespace GestaoRestaurante.Vendas.Api.Workers;

public sealed class ProdutoCatalogoConsumidor : ConsumidorFilaBase<ProdutoCatalogoMensagem>
{
    public ProdutoCatalogoConsumidor(ConexaoRabbitMq conexao, IServiceScopeFactory scopeFactory, ILogger<ProdutoCatalogoConsumidor> logger)
        : base(conexao, scopeFactory, logger)
    {
    }

    protected override string Fila => Filas.CatalogoProdutos;

    protected override async Task<bool> ProcessarAsync(ProdutoCatalogoMensagem mensagem, Guid mensagemId, IServiceProvider servicos, CancellationToken cancellationToken)
    {
        var sender = servicos.GetRequiredService<ISender>();
        await sender.Send(new SincronizarProdutoCatalogoCommand(mensagem), cancellationToken);
        return true;
    }
}
