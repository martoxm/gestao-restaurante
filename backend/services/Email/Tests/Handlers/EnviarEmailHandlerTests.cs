using GestaoRestaurante.Core.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using GestaoRestaurante.Email.Application.Abstractions.Services;
using GestaoRestaurante.Email.Application.UseCases.Emails;
using GestaoRestaurante.Email.Application.Validators;
using GestaoRestaurante.Email.Infrastructure.Persistence;
using GestaoRestaurante.Core.Persistencia.Mensageria;

namespace GestaoRestaurante.Email.Tests.Handlers;

public class EnviarEmailHandlerTests
{
    private sealed class FakeEmailService : IEmailService
    {
        public bool Responder { get; set; } = true;
        public List<(List<string> Destinatarios, string Assunto)> Enviados { get; } = [];

        public Task<bool> EnviarAsync(IEnumerable<string> destinatarios, string assunto, string corpoHtml, CancellationToken ct = default)
        {
            if (Responder)
                Enviados.Add((destinatarios.ToList(), assunto));
            return Task.FromResult(Responder);
        }
    }

    private sealed class FakeRelogio : IRelogio
    {
        public DateTime AgoraUtc { get; } = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    }

    private static (EnviarEmailHandler Handler, FakeEmailService Smtp, AppDbContext Context) Montar()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        var smtp = new FakeEmailService();
        var handler = new EnviarEmailHandler(smtp, new MensagemProcessadaRepository<AppDbContext>(context), context, new FakeRelogio());
        return (handler, smtp, context);
    }

    private static EnviarEmailCommand Pedido(Guid mensagemId) =>
        new(mensagemId, ["ana@teste.com"], "Estoque baixo: Arroz", "<p>corpo</p>");

    [Fact]
    public async Task Handle_SmtpOk_EnviaEMarcaComoProcessada()
    {
        var (handler, smtp, context) = Montar();

        var ok = await handler.Handle(Pedido(Guid.NewGuid()), CancellationToken.None);

        Assert.True(ok);
        var enviado = Assert.Single(smtp.Enviados);
        Assert.Equal(["ana@teste.com"], enviado.Destinatarios);
        Assert.Equal(1, await context.MensagensProcessadas.CountAsync());
    }

    [Fact]
    public async Task Handle_SmtpFalhou_DevolveFalseENaoMarca()
    {
        var (handler, smtp, context) = Montar();
        smtp.Responder = false;

        var ok = await handler.Handle(Pedido(Guid.NewGuid()), CancellationToken.None);

        Assert.False(ok);
        Assert.Equal(0, await context.MensagensProcessadas.CountAsync());
    }

    [Fact]
    public async Task Handle_MesmaMensagemDuasVezes_EnviaUmaVezSo()
    {
        var (handler, smtp, _) = Montar();
        var mensagemId = Guid.NewGuid();

        await handler.Handle(Pedido(mensagemId), CancellationToken.None);
        var segunda = await handler.Handle(Pedido(mensagemId), CancellationToken.None);

        Assert.True(segunda);
        Assert.Single(smtp.Enviados);
    }

    [Fact]
    public void Validator_SemDestinatarioOuComEmailInvalido_Recusa()
    {
        var validator = new EnviarEmailCommandValidator();

        Assert.False(validator.Validate(new EnviarEmailCommand(Guid.NewGuid(), [], "Assunto", "")).IsValid);
        Assert.False(validator.Validate(new EnviarEmailCommand(Guid.NewGuid(), ["nao-e-email"], "Assunto", "")).IsValid);
        Assert.True(validator.Validate(Pedido(Guid.NewGuid())).IsValid);
    }
}
