using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using GestaoRestaurante.Core.Domain;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Core.Mensageria.Contratos;
using GestaoRestaurante.Identidade.Application.UseCases.Restaurantes;
using GestaoRestaurante.Identidade.Domain.Entities;
using GestaoRestaurante.Identidade.Domain.Enums;
using GestaoRestaurante.Identidade.Tests.Common;

namespace GestaoRestaurante.Identidade.Tests.Handlers;

public class AplicarPagamentoPlanoTests
{
    private static readonly DateTime CriadoEm = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private sealed class FakeMensagensProcessadas : IMensagemProcessadaRepository
    {
        public List<Guid> Ids { get; } = [];
        public Task<bool> ExisteAsync(Guid mensagemId, CancellationToken cancellationToken) => Task.FromResult(Ids.Contains(mensagemId));
        public void Adicionar(MensagemProcessada mensagem) => Ids.Add(mensagem.Id);
    }


    [Fact]
    public void Aplicar_SaindoDoTeste_ValeTrintaDiasAPartirDeAgora()
    {
        var restaurante = Restaurante.Criar("Cantina", CriadoEm);
        var agora = CriadoEm.AddDays(20);

        restaurante.AplicarPagamentoDoPlano(PlanoRestaurante.Pro, 30, agora);

        Assert.Equal(PlanoRestaurante.Pro, restaurante.Plano);
        Assert.Null(restaurante.TesteTerminaEm);
        Assert.Equal(agora.AddDays(30), restaurante.PlanoValidoAte);
        Assert.True(restaurante.EstaLiberado(agora));
    }

    [Fact]
    public void Aplicar_PlanoAindaValendo_SomaNoFimAtual()
    {
        var restaurante = Restaurante.Criar("Cantina", CriadoEm);
        restaurante.AplicarPagamentoDoPlano(PlanoRestaurante.Basico, 30, CriadoEm);

        restaurante.AplicarPagamentoDoPlano(PlanoRestaurante.Basico, 30, CriadoEm.AddDays(20));

        Assert.Equal(CriadoEm.AddDays(60), restaurante.PlanoValidoAte);
    }

    [Fact]
    public void PlanoPagoVencido_BloqueiaEscrita()
    {
        var restaurante = Restaurante.Criar("Cantina", CriadoEm);
        restaurante.AplicarPagamentoDoPlano(PlanoRestaurante.Basico, 30, CriadoEm);

        Assert.False(restaurante.EstaLiberado(CriadoEm.AddDays(31)));
        Assert.Equal("PlanoVencido", restaurante.ObterSituacao(CriadoEm.AddDays(31)));
    }

    [Fact]
    public void MudarPlanoPeloSuperAdmin_NaoVence()
    {
        var restaurante = Restaurante.Criar("Cantina", CriadoEm);
        restaurante.AplicarPagamentoDoPlano(PlanoRestaurante.Basico, 30, CriadoEm);

        restaurante.MudarPlano(PlanoRestaurante.Pro, CriadoEm);

        Assert.Null(restaurante.PlanoValidoAte);
        Assert.True(restaurante.EstaLiberado(CriadoEm.AddYears(1)));
    }

    [Fact]
    public void Aplicar_PlanoTeste_LancaDomainException()
    {
        var restaurante = Restaurante.Criar("Cantina", CriadoEm);

        Assert.Throws<DomainException>(() => restaurante.AplicarPagamentoDoPlano(PlanoRestaurante.Teste, 30, CriadoEm));
    }


    private static async Task<(TestDbContext Context, Restaurante Restaurante, FakeMensagensProcessadas Processadas, AplicarPagamentoPlanoHandler Handler)> MontarAsync()
    {
        var context = TestDbContextFactory.Create();
        var restaurante = Restaurante.Criar("Cantina", CriadoEm);
        context.Restaurantes.Add(restaurante);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var processadas = new FakeMensagensProcessadas();
        var handler = new AplicarPagamentoPlanoHandler(new FakeRestauranteRepository(context), processadas, context,
            new FakeRelogio { AgoraUtc = CriadoEm.AddDays(5) }, NullLogger<AplicarPagamentoPlanoHandler>.Instance);
        return (context, restaurante, processadas, handler);
    }

    [Fact]
    public async Task Handle_PlanoPago_AplicaNoRestaurante()
    {
        var (context, restaurante, processadas, handler) = await MontarAsync();
        var mensagem = new PlanoPagoMensagem(Guid.NewGuid(), restaurante.Id, "Pro", 30, CriadoEm.AddDays(5));

        await handler.Handle(new AplicarPagamentoPlanoCommand(mensagem), CancellationToken.None);

        var salvo = await context.Restaurantes.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(PlanoRestaurante.Pro, salvo.Plano);
        Assert.Equal(CriadoEm.AddDays(35), salvo.PlanoValidoAte);
        Assert.Contains(mensagem.CobrancaId, processadas.Ids);
    }

    [Fact]
    public async Task Handle_MesmaCobrancaDuasVezes_AplicaUmaVezSo()
    {
        var (context, restaurante, _, handler) = await MontarAsync();
        var mensagem = new PlanoPagoMensagem(Guid.NewGuid(), restaurante.Id, "Basico", 30, CriadoEm.AddDays(5));

        await handler.Handle(new AplicarPagamentoPlanoCommand(mensagem), CancellationToken.None);
        context.ChangeTracker.Clear();
        await handler.Handle(new AplicarPagamentoPlanoCommand(mensagem), CancellationToken.None);

        Assert.Equal(CriadoEm.AddDays(35), (await context.Restaurantes.IgnoreQueryFilters().SingleAsync()).PlanoValidoAte);
    }

    [Fact]
    public async Task Handle_RestauranteInexistente_SoMarcaComoProcessada()
    {
        var (_, _, processadas, handler) = await MontarAsync();
        var mensagem = new PlanoPagoMensagem(Guid.NewGuid(), Guid.NewGuid(), "Pro", 30, CriadoEm);

        await handler.Handle(new AplicarPagamentoPlanoCommand(mensagem), CancellationToken.None);

        Assert.Contains(mensagem.CobrancaId, processadas.Ids);
    }
}
