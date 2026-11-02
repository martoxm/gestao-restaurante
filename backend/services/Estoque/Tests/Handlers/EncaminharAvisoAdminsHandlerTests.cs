using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using RestauranteEstoque.Application.Abstractions.Services;
using RestauranteEstoque.Application.UseCases.Produtos;
using RestauranteEstoque.Core.Mensageria;
using RestauranteEstoque.Core.Mensageria.Contratos;
using RestauranteEstoque.Tests.Common;

namespace RestauranteEstoque.Tests.Handlers;

public class EncaminharAvisoAdminsHandlerTests
{
    private sealed class FakeIdentidadeClient : IIdentidadeClient
    {
        public List<string> Emails { get; } = [];
        public int Chamadas { get; private set; }

        public Task<IReadOnlyList<string>> ListarEmailsAdminsAsync(Guid restauranteId, CancellationToken cancellationToken)
        {
            Chamadas++;
            return Task.FromResult<IReadOnlyList<string>>(Emails);
        }
    }

    private static (EncaminharAvisoAdminsHandler Handler, FakeIdentidadeClient Identidade, FakeOutboxMensagemRepository Outbox, TestDbContext Context) Montar()
    {
        var context = TestDbContextFactory.Create();
        var identidade = new FakeIdentidadeClient();
        var outbox = new FakeOutboxMensagemRepository();
        var handler = new EncaminharAvisoAdminsHandler(
            identidade, outbox, new FakeMensagemProcessadaRepository(context), context, new FakeRelogio(),
            NullLogger<EncaminharAvisoAdminsHandler>.Instance);
        return (handler, identidade, outbox, context);
    }

    private static EncaminharAvisoAdminsCommand Pedido(Guid mensagemId) =>
        new(mensagemId, DadosTeste.RestauranteId, "Estoque baixo: Arroz", "<p>corpo</p>");

    [Fact]
    public async Task Handle_ComAdmins_GravaEmailComOsDestinatariosProntos()
    {
        var (handler, identidade, outbox, context) = Montar();
        identidade.Emails.AddRange(["ana@teste.com", "bia@teste.com"]);

        await handler.Handle(Pedido(Guid.NewGuid()), CancellationToken.None);

        var linha = Assert.Single(outbox.Mensagens);
        Assert.Equal(Filas.Email, linha.Fila);
        var email = JsonSerializer.Deserialize<EnviarEmailMensagem>(linha.Conteudo)!;
        Assert.Equal(["ana@teste.com", "bia@teste.com"], email.Destinatarios);
        Assert.Equal("Estoque baixo: Arroz", email.Assunto);
        Assert.Single(context.MensagensProcessadas);
    }

    [Fact]
    public async Task Handle_SemAdmins_NaoGravaEmailMasMarcaComoProcessada()
    {
        var (handler, _, outbox, context) = Montar();

        await handler.Handle(Pedido(Guid.NewGuid()), CancellationToken.None);

        Assert.Empty(outbox.Mensagens);
        Assert.Single(context.MensagensProcessadas);
    }

    [Fact]
    public async Task Handle_MesmaMensagemDuasVezes_PedeOEmailUmaVezSo()
    {
        var (handler, identidade, outbox, _) = Montar();
        identidade.Emails.Add("ana@teste.com");
        var mensagemId = Guid.NewGuid();

        await handler.Handle(Pedido(mensagemId), CancellationToken.None);
        await handler.Handle(Pedido(mensagemId), CancellationToken.None);

        Assert.Single(outbox.Mensagens);
        Assert.Equal(1, identidade.Chamadas);
    }
}
