using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using GestaoRestaurante.Core.Domain.ValueObjects;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Core.Mensageria.Contratos;
using GestaoRestaurante.Core.Persistencia.Mensageria;
using GestaoRestaurante.Pagamentos.Application.UseCases.Assinaturas;
using GestaoRestaurante.Pagamentos.Domain.Entities;
using GestaoRestaurante.Pagamentos.Domain.Enums;
using GestaoRestaurante.Pagamentos.Infrastructure.Assinaturas;
using GestaoRestaurante.Pagamentos.Infrastructure.Gateways;
using GestaoRestaurante.Pagamentos.Infrastructure.Persistence;
using GestaoRestaurante.Pagamentos.Infrastructure.Persistence.Repositories;
using GestaoRestaurante.Pagamentos.Tests.Common;

namespace GestaoRestaurante.Pagamentos.Tests.Handlers;

public class ConfirmarPagamentoPlanoTests
{
    private const string Sessao = "cs_test_abc";

    private static async Task<(string NomeBanco, CobrancaPlano Cobranca)> CobrancaComSessaoAsync()
    {
        var nomeBanco = Guid.NewGuid().ToString();
        var cobranca = CobrancaPlano.Criar(TestePagamentos.RestauranteId, PlanoPago.Pro, Dinheiro.Criar(199m), TestePagamentos.Agora);
        cobranca.RegistrarSessao(Sessao);
        await using var context = TestePagamentos.CriarContexto(nomeBanco: nomeBanco);
        context.CobrancasPlano.Add(cobranca);
        await context.SaveChangesAsync();
        return (nomeBanco, cobranca);
    }

    private static async Task ConfirmarAsync(string nomeBanco)
    {
        await using var context = TestePagamentos.CriarContexto(new FakeUsuarioAtualService(), nomeBanco);
        var handler = new ConfirmarPagamentoPlanoHandler(new CobrancaPlanoRepository(context), new OutboxMensagemRepository<AppDbContext>(context),
            context, new FakeRelogio(), NullLogger<ConfirmarPagamentoPlanoHandler>.Instance);
        await handler.Handle(new ConfirmarPagamentoPlanoCommand(Sessao), CancellationToken.None);
    }

    private static async Task<List<OutboxMensagem>> OutboxAsync(string nomeBanco)
    {
        await using var leitura = TestePagamentos.CriarContexto(nomeBanco: nomeBanco);
        return await leitura.OutboxMensagens.ToListAsync();
    }

    [Fact]
    public async Task Handle_SessaoPaga_MarcaACobrancaEAvisaAIdentidade()
    {
        var (nomeBanco, cobranca) = await CobrancaComSessaoAsync();

        await ConfirmarAsync(nomeBanco);

        var linha = Assert.Single(await OutboxAsync(nomeBanco));
        Assert.Equal(Filas.PlanoPago, linha.Fila);
        var aviso = JsonSerializer.Deserialize<PlanoPagoMensagem>(linha.Conteudo)!;
        Assert.Equal(cobranca.Id, aviso.CobrancaId);
        Assert.Equal(TestePagamentos.RestauranteId, aviso.RestauranteId);
        Assert.Equal("Pro", aviso.Plano);
        Assert.Equal(30, aviso.Dias);

        await using var leitura = TestePagamentos.CriarContexto(nomeBanco: nomeBanco);
        Assert.Equal(StatusCobrancaPlano.Paga, (await leitura.CobrancasPlano.SingleAsync()).Status);
    }

    [Fact]
    public async Task Handle_WebhookRepetido_AvisaUmaVezSo()
    {
        var (nomeBanco, _) = await CobrancaComSessaoAsync();

        await ConfirmarAsync(nomeBanco);
        await ConfirmarAsync(nomeBanco);

        Assert.Single(await OutboxAsync(nomeBanco));
    }

    [Fact]
    public async Task Handle_SessaoQueNaoENossa_Ignora()
    {
        var nomeBanco = Guid.NewGuid().ToString();

        await ConfirmarAsync(nomeBanco);

        Assert.Empty(await OutboxAsync(nomeBanco));
    }

    [Fact]
    public async Task IniciarAssinatura_ComGatewayFalso_JaAvisaAIdentidade()
    {
        var nomeBanco = Guid.NewGuid().ToString();
        await using (var context = TestePagamentos.CriarContexto(nomeBanco: nomeBanco))
        {
            var handler = new IniciarAssinaturaHandler(new CobrancaPlanoRepository(context), new CheckoutPlanoFalso(new FrontSettings()),
                new PlanosSettings(), new OutboxMensagemRepository<AppDbContext>(context),
                new FakeUsuarioAtualService { RestauranteId = TestePagamentos.RestauranteId }, context, new FakeRelogio());
            await handler.Handle(new IniciarAssinaturaCommand("Basico"), CancellationToken.None);
        }

        var aviso = JsonSerializer.Deserialize<PlanoPagoMensagem>(Assert.Single(await OutboxAsync(nomeBanco)).Conteudo)!;
        Assert.Equal("Basico", aviso.Plano);
    }


    private const string Segredo = "whsec_teste";

    private static string EventoCheckout(string statusPagamento, bool comMetadados = true) => $$"""
        {
          "id": "evt_checkout",
          "object": "event",
          "api_version": "2025-01-27.acacia",
          "created": 1730300000,
          "type": "checkout.session.completed",
          "data": {
            "object": {
              "id": "{{Sessao}}",
              "object": "checkout.session",
              "mode": "payment",
              "payment_status": "{{statusPagamento}}",
              "metadata": {{(comMetadados ? """{ "cobrancaId": "33333333-3333-3333-3333-333333333333" }""" : "{}")}}
            }
          }
        }
        """;

    [Fact]
    public void Webhook_CheckoutPago_ViraConfirmacaoDoPlano()
    {
        var corpo = EventoCheckout("paid");

        var resultado = WebhookStripe.Interpretar(corpo, AssinaturaWebhook.Gerar(corpo, Segredo, DateTimeOffset.UtcNow), Segredo);

        Assert.True(resultado.AssinaturaValida);
        Assert.Null(resultado.Comando);
        Assert.Equal(Sessao, resultado.ConfirmacaoPlano!.IdSessao);
    }

    [Theory]
    [InlineData("unpaid", true)]
    [InlineData("paid", false)]
    public void Webhook_CheckoutNaoPagoOuSemMetadados_Ignora(string statusPagamento, bool comMetadados)
    {
        var corpo = EventoCheckout(statusPagamento, comMetadados);

        var resultado = WebhookStripe.Interpretar(corpo, AssinaturaWebhook.Gerar(corpo, Segredo, DateTimeOffset.UtcNow), Segredo);

        Assert.True(resultado.AssinaturaValida);
        Assert.Null(resultado.ConfirmacaoPlano);
    }
}
