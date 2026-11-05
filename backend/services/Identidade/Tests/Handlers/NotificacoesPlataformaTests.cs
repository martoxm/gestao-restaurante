using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Core.Mensageria.Contratos;
using GestaoRestaurante.Identidade.Application.UseCases.Plataforma;
using GestaoRestaurante.Identidade.Application.UseCases.Restaurantes;
using GestaoRestaurante.Identidade.Contracts.Plataforma;
using GestaoRestaurante.Identidade.Domain.Entities;
using GestaoRestaurante.Identidade.Domain.Enums;
using GestaoRestaurante.Identidade.Tests.Common;

namespace GestaoRestaurante.Identidade.Tests.Handlers;

public class NotificacoesPlataformaTests
{
    private static readonly DateTime CriadoEm = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

    private sealed class FakeMensagensProcessadas : IMensagemProcessadaRepository
    {
        private readonly List<Guid> _ids = [];
        public Task<bool> ExisteAsync(Guid mensagemId, CancellationToken cancellationToken) => Task.FromResult(_ids.Contains(mensagemId));
        public void Adicionar(MensagemProcessada mensagem) => _ids.Add(mensagem.Id);
    }


    [Fact]
    public void PlanoPago_PrimeiroPagamento_EhAssinatura()
    {
        var restaurante = Restaurante.Criar("Cantina", CriadoEm);
        restaurante.AplicarPagamentoDoPlano(PlanoRestaurante.Basico, 30, CriadoEm);

        var notificacao = NotificacaoPlataforma.PlanoPago(restaurante, PlanoRestaurante.Teste, jaTinhaAssinado: false, 30, CriadoEm);

        Assert.Equal(NotificacaoPlataforma.TipoPlanoAssinado, notificacao.Tipo);
        Assert.Contains("Cantina", notificacao.Mensagem);
    }

    [Fact]
    public void PlanoPago_MesmoPlanoDeNovo_EhRenovacao()
    {
        var restaurante = Restaurante.Criar("Cantina", CriadoEm);
        restaurante.AplicarPagamentoDoPlano(PlanoRestaurante.Basico, 30, CriadoEm);

        var notificacao = NotificacaoPlataforma.PlanoPago(restaurante, PlanoRestaurante.Basico, jaTinhaAssinado: true, 30, CriadoEm);

        Assert.Equal(NotificacaoPlataforma.TipoPlanoRenovado, notificacao.Tipo);
    }

    [Fact]
    public void PlanoPago_BasicoParaPro_EhTroca()
    {
        var restaurante = Restaurante.Criar("Cantina", CriadoEm);
        restaurante.AplicarPagamentoDoPlano(PlanoRestaurante.Pro, 30, CriadoEm);

        var notificacao = NotificacaoPlataforma.PlanoPago(restaurante, PlanoRestaurante.Basico, jaTinhaAssinado: true, 30, CriadoEm);

        Assert.Equal(NotificacaoPlataforma.TipoPlanoTrocado, notificacao.Tipo);
    }

    [Fact]
    public void Vencimento_TesteAindaValendo_NaoNotifica()
    {
        var restaurante = Restaurante.Criar("Cantina", CriadoEm);

        Assert.Null(NotificacaoPlataforma.Vencimento(restaurante, CriadoEm.AddDays(5)));
    }


    [Fact]
    public async Task AplicarPagamento_GravaNotificacaoDeAssinatura()
    {
        var context = TestDbContextFactory.Create();
        var restaurante = Restaurante.Criar("Cantina", CriadoEm);
        context.Restaurantes.Add(restaurante);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var handler = new AplicarPagamentoPlanoHandler(new FakeRestauranteRepository(context), new FakeMensagensProcessadas(), context,
            new FakeRelogio { AgoraUtc = CriadoEm.AddDays(5) }, NullLogger<AplicarPagamentoPlanoHandler>.Instance,
            new FakeNotificacaoPlataformaRepository(context));

        await handler.Handle(new AplicarPagamentoPlanoCommand(new PlanoPagoMensagem(Guid.NewGuid(), restaurante.Id, "Pro", 30, CriadoEm)), CancellationToken.None);

        var notificacao = await context.NotificacoesPlataforma.SingleAsync();
        Assert.Equal(NotificacaoPlataforma.TipoPlanoAssinado, notificacao.Tipo);
        Assert.Equal(restaurante.Id, notificacao.RestauranteId);
    }

    [Fact]
    public async Task NotificarVencimentos_AvisaUmaVezSo()
    {
        var context = TestDbContextFactory.Create();
        var vencido = Restaurante.Criar("Vencido", CriadoEm);
        var emDia = Restaurante.Criar("Em dia", CriadoEm.AddDays(10));
        context.Restaurantes.AddRange(vencido, emDia);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var handler = CriarNotificarVencimentos(context, CriadoEm.AddDays(15));

        Assert.Equal(1, await handler.Handle(new NotificarVencimentosCommand(), CancellationToken.None));
        context.ChangeTracker.Clear();
        Assert.Equal(0, await handler.Handle(new NotificarVencimentosCommand(), CancellationToken.None));

        var notificacao = await context.NotificacoesPlataforma.SingleAsync();
        Assert.Equal(NotificacaoPlataforma.TipoTesteVencido, notificacao.Tipo);
        Assert.Equal(vencido.Id, notificacao.RestauranteId);
    }

    [Fact]
    public async Task NotificarVencimentos_PlanoPagoVencido_AvisaPlanoVencido()
    {
        var context = TestDbContextFactory.Create();
        var restaurante = Restaurante.Criar("Cantina", CriadoEm);
        restaurante.AplicarPagamentoDoPlano(PlanoRestaurante.Basico, 30, CriadoEm);
        context.Restaurantes.Add(restaurante);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await CriarNotificarVencimentos(context, CriadoEm.AddDays(31)).Handle(new NotificarVencimentosCommand(), CancellationToken.None);

        Assert.Equal(NotificacaoPlataforma.TipoPlanoVencido, (await context.NotificacoesPlataforma.SingleAsync()).Tipo);
    }

    [Fact]
    public async Task ListarContarEMarcar_FuncionamJuntos()
    {
        var context = TestDbContextFactory.Create();
        var repositorio = new FakeNotificacaoPlataformaRepository(context);
        repositorio.Adicionar(NotificacaoPlataforma.Criar("NovoCadastro", "Antiga", "a", CriadoEm));
        repositorio.Adicionar(NotificacaoPlataforma.Criar("NovoCadastro", "Nova", "b", CriadoEm.AddHours(1)));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var relogio = new FakeRelogio { AgoraUtc = CriadoEm.AddDays(1) };
        var lista = await new ListarNotificacoesPlataformaHandler(repositorio)
            .Handle(new ListarNotificacoesPlataformaQuery(new ListarNotificacoesRequest()), CancellationToken.None);
        Assert.Equal("Nova", lista.Itens.First().Titulo);

        var contar = new ContarNotificacoesNaoLidasHandler(repositorio);
        Assert.Equal(2, (await contar.Handle(new ContarNotificacoesNaoLidasQuery(), CancellationToken.None)).Quantidade);

        var marcar = new MarcarNotificacoesLidasHandler(repositorio, relogio);
        Assert.Equal(1, await marcar.Handle(new MarcarNotificacoesLidasCommand(lista.Itens.First().Id), CancellationToken.None));
        context.ChangeTracker.Clear();
        Assert.Equal(1, (await contar.Handle(new ContarNotificacoesNaoLidasQuery(), CancellationToken.None)).Quantidade);

        Assert.Equal(1, await marcar.Handle(new MarcarNotificacoesLidasCommand(null), CancellationToken.None));
        context.ChangeTracker.Clear();
        Assert.Equal(0, (await contar.Handle(new ContarNotificacoesNaoLidasQuery(), CancellationToken.None)).Quantidade);
    }

    private static NotificarVencimentosHandler CriarNotificarVencimentos(TestDbContext context, DateTime agora) =>
        new(new FakeRestauranteRepository(context), new FakeNotificacaoPlataformaRepository(context), context, new FakeRelogio { AgoraUtc = agora });
}
