using Microsoft.EntityFrameworkCore;
using GestaoRestaurante.Core.Application.Exceptions;
using GestaoRestaurante.Core.Domain;
using GestaoRestaurante.Core.Domain.ValueObjects;
using GestaoRestaurante.Pagamentos.Application.Abstractions.Services;
using GestaoRestaurante.Pagamentos.Application.UseCases.Assinaturas;
using GestaoRestaurante.Pagamentos.Application.Validators;
using GestaoRestaurante.Pagamentos.Domain.Entities;
using GestaoRestaurante.Pagamentos.Domain.Enums;
using GestaoRestaurante.Pagamentos.Infrastructure.Assinaturas;
using GestaoRestaurante.Core.Persistencia.Mensageria;
using GestaoRestaurante.Pagamentos.Infrastructure.Persistence;
using GestaoRestaurante.Pagamentos.Infrastructure.Persistence.Repositories;
using GestaoRestaurante.Pagamentos.Tests.Common;

namespace GestaoRestaurante.Pagamentos.Tests.Handlers;

public class IniciarAssinaturaHandlerTests
{
    private sealed class FakeCheckout : ICheckoutPlano
    {
        public PedidoCheckout? UltimoPedido { get; private set; }

        public Task<SessaoCheckout> CriarSessaoAsync(PedidoCheckout pedido, CancellationToken cancellationToken)
        {
            UltimoPedido = pedido;
            return Task.FromResult(new SessaoCheckout("cs_test_123", "https://checkout.stripe.com/c/pay/cs_test_123"));
        }
    }

    private static readonly PlanosSettings Tabela = new() { Basico = 99m, Pro = 199m };

    [Fact]
    public async Task Handle_PlanoPro_CobraOPrecoDaTabelaEGuardaASessao()
    {
        var nomeBanco = Guid.NewGuid().ToString();
        var checkout = new FakeCheckout();

        await using (var context = TestePagamentos.CriarContexto(nomeBanco: nomeBanco))
        {
            var handler = new IniciarAssinaturaHandler(new CobrancaPlanoRepository(context), checkout, Tabela, new OutboxMensagemRepository<AppDbContext>(context),
                new FakeUsuarioAtualService { RestauranteId = TestePagamentos.RestauranteId }, context, new FakeRelogio());

            var resposta = await handler.Handle(new IniciarAssinaturaCommand("pro"), CancellationToken.None);

            Assert.Equal("https://checkout.stripe.com/c/pay/cs_test_123", resposta.UrlPagamento);
        }

        Assert.Equal(199m, checkout.UltimoPedido!.Valor);
        Assert.Equal(PlanoPago.Pro, checkout.UltimoPedido.Plano);
        Assert.Equal(TestePagamentos.RestauranteId, checkout.UltimoPedido.RestauranteId);

        await using var leitura = TestePagamentos.CriarContexto(nomeBanco: nomeBanco);
        var cobranca = await leitura.CobrancasPlano.SingleAsync();
        Assert.Equal(StatusCobrancaPlano.Pendente, cobranca.Status);
        Assert.Equal("cs_test_123", cobranca.IdSessaoGateway);
        Assert.Equal(30, cobranca.Dias);
    }

    [Fact]
    public async Task Handle_TokenSemRestaurante_LancaSessaoInvalida()
    {
        await using var context = TestePagamentos.CriarContexto();
        var handler = new IniciarAssinaturaHandler(new CobrancaPlanoRepository(context), new FakeCheckout(), Tabela, new OutboxMensagemRepository<AppDbContext>(context),
            new FakeUsuarioAtualService(), context, new FakeRelogio());

        await Assert.ThrowsAsync<SessaoInvalidaException>(() => handler.Handle(new IniciarAssinaturaCommand("Basico"), CancellationToken.None));
    }

    [Fact]
    public async Task CobrancaDeOutroRestaurante_NaoApareceNaConsulta()
    {
        var nomeBanco = Guid.NewGuid().ToString();
        await using (var context = TestePagamentos.CriarContexto(nomeBanco: nomeBanco))
        {
            context.CobrancasPlano.Add(CobrancaPlano.Criar(Guid.NewGuid(), PlanoPago.Basico, Dinheiro.Criar(99m), TestePagamentos.Agora));
            await context.SaveChangesAsync();
        }

        await using var leitura = TestePagamentos.CriarContexto(nomeBanco: nomeBanco);
        Assert.Empty(await leitura.CobrancasPlano.ToListAsync());
    }

    [Theory]
    [InlineData("Basico", true)]
    [InlineData("pro", true)]
    [InlineData("Teste", false)]
    [InlineData("", false)]
    [InlineData("99", false)]
    public void Validator_SoAceitaPlanosPagos(string plano, bool valido)
    {
        var resultado = new IniciarAssinaturaCommandValidator().Validate(new IniciarAssinaturaCommand(plano));

        Assert.Equal(valido, resultado.IsValid);
    }

    [Fact]
    public void Criar_ValorZero_LancaDomainException()
    {
        Assert.Throws<DomainException>(() => CobrancaPlano.Criar(TestePagamentos.RestauranteId, PlanoPago.Pro, Dinheiro.Criar(0m), TestePagamentos.Agora));
    }

    [Fact]
    public void RegistrarSessao_DuasVezes_LancaDomainException()
    {
        var cobranca = CobrancaPlano.Criar(TestePagamentos.RestauranteId, PlanoPago.Pro, Dinheiro.Criar(199m), TestePagamentos.Agora);
        cobranca.RegistrarSessao("cs_1");

        Assert.Throws<DomainException>(() => cobranca.RegistrarSessao("cs_2"));
    }

    [Fact]
    public async Task CheckoutFalso_DevolveATelaDeSucessoSimulada()
    {
        var sessao = await new CheckoutPlanoFalso(new FrontSettings { Url = "http://localhost:3000" })
            .CriarSessaoAsync(new PedidoCheckout(Guid.NewGuid(), TestePagamentos.RestauranteId, PlanoPago.Basico, "Básico", 99m, 30), CancellationToken.None);

        Assert.Equal("http://localhost:3000/assinatura?status=sucesso&simulado=1", sessao.Url);
        Assert.StartsWith("cs_falso_", sessao.IdSessao);
    }
}
