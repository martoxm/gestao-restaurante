using System.Text.Json;
using GestaoRestaurante.Core.Application.Events;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Core.Mensageria.Contratos;
using GestaoRestaurante.Application.UseCases.Produtos;
using GestaoRestaurante.Domain.Events;
using GestaoRestaurante.Tests.Common;

namespace GestaoRestaurante.Tests.Handlers;

public class AvisarEstoqueBaixoHandlerTests
{
    private static (AvisarEstoqueBaixoHandler Handler, FakeOutboxMensagemRepository Outbox, FakeRelogio Relogio) CriarHandler()
    {
        var outbox = new FakeOutboxMensagemRepository();
        var relogio = new FakeRelogio();
        var handler = new AvisarEstoqueBaixoHandler(outbox, relogio);
        return (handler, outbox, relogio);
    }

    private static DomainEventNotification<EstoqueFicouBaixoEvent> Notificacao(string nomeProduto, Guid restauranteId) =>
        new(new EstoqueFicouBaixoEvent(Guid.NewGuid(), nomeProduto, 7, restauranteId));

    [Fact]
    public async Task Handle_ProdutoComRestaurante_DeveGravarAvisoAosAdminsNoOutbox()
    {
        var (handler, outbox, relogio) = CriarHandler();
        var restauranteId = Guid.NewGuid();

        await handler.Handle(Notificacao("Arroz", restauranteId), CancellationToken.None);

        var linha = Assert.Single(outbox.Mensagens);
        Assert.Equal(Filas.AvisarAdmins, linha.Fila);
        Assert.Equal(nameof(AvisarAdminsRestauranteMensagem), linha.Tipo);
        Assert.Equal(relogio.AgoraUtc, linha.CriadoEm);

        var email = JsonSerializer.Deserialize<AvisarAdminsRestauranteMensagem>(linha.Conteudo)!;
        Assert.Equal(restauranteId, email.RestauranteId);
        Assert.Equal("Estoque baixo: Arroz", email.Assunto);
        Assert.Contains("<b>Arroz</b>", email.CorpoHtml);
        Assert.Contains("<b>7</b>", email.CorpoHtml);
    }

    [Fact]
    public async Task Handle_NomeComHtml_DeveEscaparParaNaoInjetarNoEmail()
    {
        var (handler, outbox, _) = CriarHandler();

        await handler.Handle(Notificacao("<a href=\"http://golpe\">clique</a>", Guid.NewGuid()), CancellationToken.None);

        var corpo = JsonSerializer.Deserialize<AvisarAdminsRestauranteMensagem>(Assert.Single(outbox.Mensagens).Conteudo)!.CorpoHtml;
        Assert.DoesNotContain("<a href", corpo);
        Assert.Contains("&lt;a href=", corpo);
    }
}
