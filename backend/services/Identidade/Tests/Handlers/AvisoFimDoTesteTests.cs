using System.Text.Json;
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

public class AvisoFimDoTesteTests
{
    private static readonly DateTime CriadoEm = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static DateTime FaltandoDias(double dias) => CriadoEm.AddDays(Restaurante.DiasTeste).AddDays(-dias);


    [Fact]
    public void DiasParaAvisar_LongeDoFim_NaoAvisa()
    {
        var restaurante = Restaurante.Criar("Cantina", CriadoEm);

        Assert.Null(restaurante.DiasParaAvisarFimDoTeste(FaltandoDias(5)));
    }

    [Theory]
    [InlineData(3, 3)]
    [InlineData(2.5, 3)]
    [InlineData(2, 3)]
    [InlineData(1, 1)]
    [InlineData(0.2, 1)]
    public void DiasParaAvisar_PertoDoFim_DevolveOMarco(double faltam, int esperado)
    {
        var restaurante = Restaurante.Criar("Cantina", CriadoEm);

        Assert.Equal(esperado, restaurante.DiasParaAvisarFimDoTeste(FaltandoDias(faltam)));
    }

    [Fact]
    public void DiasParaAvisar_AvisoDe3JaEnviado_SoAvisaDeNovoNoDe1()
    {
        var restaurante = Restaurante.Criar("Cantina", CriadoEm);
        restaurante.RegistrarAvisoFimDoTeste(3);

        Assert.Null(restaurante.DiasParaAvisarFimDoTeste(FaltandoDias(2)));
        Assert.Equal(1, restaurante.DiasParaAvisarFimDoTeste(FaltandoDias(1)));
    }

    [Fact]
    public void DiasParaAvisar_TesteVencidoOuPlanoPago_NaoAvisa()
    {
        var vencido = Restaurante.Criar("Cantina", CriadoEm);
        var pago = Restaurante.Criar("Cantina", CriadoEm);
        pago.MudarPlano(PlanoRestaurante.Pro, CriadoEm);

        Assert.Null(vencido.DiasParaAvisarFimDoTeste(FaltandoDias(-1)));
        Assert.Null(pago.DiasParaAvisarFimDoTeste(FaltandoDias(1)));
    }

    [Fact]
    public void EstenderTeste_DepoisDoAviso_AvisosRecomecam()
    {
        var restaurante = Restaurante.Criar("Cantina", CriadoEm);
        restaurante.RegistrarAvisoFimDoTeste(1);

        restaurante.EstenderTeste(3, FaltandoDias(1));

        Assert.Null(restaurante.UltimoAvisoFimTesteDias);
    }

    [Fact]
    public void RegistrarAviso_DiaQueNaoEMarco_LancaDomainException()
    {
        var restaurante = Restaurante.Criar("Cantina", CriadoEm);

        Assert.Throws<DomainException>(() => restaurante.RegistrarAvisoFimDoTeste(2));
    }


    private sealed record Cenario(TestDbContext Context, FakeOutboxMensagemRepository Outbox, FakeIdentityService Identity, FakeRelogio Relogio, AvisarFimDoTesteHandler Handler);

    private static Cenario Montar()
    {
        var context = TestDbContextFactory.Create();
        var outbox = new FakeOutboxMensagemRepository();
        var identity = new FakeIdentityService();
        var relogio = new FakeRelogio();
        var handler = new AvisarFimDoTesteHandler(
            new FakeRestauranteRepository(context), identity, outbox, new FakeLinksFront(), context, relogio,
            NullLogger<AvisarFimDoTesteHandler>.Instance);
        return new Cenario(context, outbox, identity, relogio, handler);
    }

    private static async Task<Restaurante> AdicionarAsync(TestDbContext context, string nome)
    {
        var restaurante = Restaurante.Criar(nome, CriadoEm);
        context.Restaurantes.Add(restaurante);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return restaurante;
    }

    [Fact]
    public async Task Handle_TesteAcabandoComAdmin_GravaEmailNoOutboxEMarcaOAviso()
    {
        var c = Montar();
        var restaurante = await AdicionarAsync(c.Context, "Cantina");
        c.Identity.EmailsAdmins[restaurante.Id] = ["dono@cantina.com"];
        c.Relogio.AgoraUtc = FaltandoDias(3);

        var avisados = await c.Handler.Handle(new AvisarFimDoTesteCommand(), CancellationToken.None);

        Assert.Equal(1, avisados);
        var linha = Assert.Single(c.Outbox.Mensagens);
        Assert.Equal(Filas.Email, linha.Fila);
        var email = JsonSerializer.Deserialize<EnviarEmailMensagem>(linha.Conteudo)!;
        Assert.Equal(["dono@cantina.com"], email.Destinatarios);
        Assert.Contains("em 3 dias", email.Assunto);
        Assert.Contains("http://front/assinatura", email.CorpoHtml);

        var salvo = await c.Context.Restaurantes.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(3, salvo.UltimoAvisoFimTesteDias);
    }

    [Fact]
    public async Task Handle_RodandoDuasVezesNoMesmoDia_NaoMandaEmailRepetido()
    {
        var c = Montar();
        var restaurante = await AdicionarAsync(c.Context, "Cantina");
        c.Identity.EmailsAdmins[restaurante.Id] = ["dono@cantina.com"];
        c.Relogio.AgoraUtc = FaltandoDias(3);

        await c.Handler.Handle(new AvisarFimDoTesteCommand(), CancellationToken.None);
        c.Context.ChangeTracker.Clear();
        var segundaVez = await c.Handler.Handle(new AvisarFimDoTesteCommand(), CancellationToken.None);

        Assert.Equal(0, segundaVez);
        Assert.Single(c.Outbox.Mensagens);
    }

    [Fact]
    public async Task Handle_FaltandoUmDia_MandaOAvisoDeAmanha()
    {
        var c = Montar();
        var restaurante = await AdicionarAsync(c.Context, "Cantina");
        c.Identity.EmailsAdmins[restaurante.Id] = ["dono@cantina.com"];
        c.Relogio.AgoraUtc = FaltandoDias(0.5);

        await c.Handler.Handle(new AvisarFimDoTesteCommand(), CancellationToken.None);

        var email = JsonSerializer.Deserialize<EnviarEmailMensagem>(Assert.Single(c.Outbox.Mensagens).Conteudo)!;
        Assert.Contains("amanhã", email.Assunto);
    }

    [Fact]
    public async Task Handle_RestauranteSemAdmin_NaoMandaEmailMasMarcaOAviso()
    {
        var c = Montar();
        await AdicionarAsync(c.Context, "Cantina");
        c.Relogio.AgoraUtc = FaltandoDias(3);

        var avisados = await c.Handler.Handle(new AvisarFimDoTesteCommand(), CancellationToken.None);

        Assert.Equal(0, avisados);
        Assert.Empty(c.Outbox.Mensagens);
        Assert.Equal(3, (await c.Context.Restaurantes.IgnoreQueryFilters().SingleAsync()).UltimoAvisoFimTesteDias);
    }

    [Fact]
    public async Task Handle_TesteLongeDoFim_NaoAvisaNinguem()
    {
        var c = Montar();
        var restaurante = await AdicionarAsync(c.Context, "Cantina");
        c.Identity.EmailsAdmins[restaurante.Id] = ["dono@cantina.com"];
        c.Relogio.AgoraUtc = FaltandoDias(10);

        Assert.Equal(0, await c.Handler.Handle(new AvisarFimDoTesteCommand(), CancellationToken.None));
        Assert.Empty(c.Outbox.Mensagens);
    }
}
