using System.Diagnostics;
using RestauranteEstoque.Core.Api;
using RestauranteEstoque.Core.Mensageria;

namespace RestauranteEstoque.Tests.Mensageria;

public class OutboxMensagemRastreioTests
{
    private record MensagemTeste(string Texto);

    [Fact]
    public void Criar_GuardaCorrelationIdETraceParentAtuais()
    {
        ContextoCorrelacao.Atual = "protocolo-123";
        using var atividade = new Activity("teste").SetIdFormat(ActivityIdFormat.W3C).Start();

        var linha = OutboxMensagem.Criar("fila-teste", new MensagemTeste("oi"), DateTime.UtcNow);

        Assert.Equal("protocolo-123", linha.CorrelationId);
        Assert.Equal(atividade.Id, linha.TraceParent);
    }

    [Fact]
    public async Task Criar_SemContexto_DeixaNulo()
    {
        var linha = await Task.Run(() =>
        {
            ContextoCorrelacao.Atual = null;
            Activity.Current = null;
            return OutboxMensagem.Criar("fila-teste", new MensagemTeste("oi"), DateTime.UtcNow);
        });

        Assert.Null(linha.CorrelationId);
        Assert.Null(linha.TraceParent);
    }
}
