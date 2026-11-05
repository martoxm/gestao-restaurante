using GestaoRestaurante.Core.Domain;
using GestaoRestaurante.Identidade.Domain.Entities;

namespace GestaoRestaurante.Identidade.Tests.Domain;

public class ConviteTests
{
    private static readonly DateTime Agora = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Convite NovoConvite(string role = "Funcionario") =>
        Convite.Criar(Guid.NewGuid(), "Cantina", "  Bruno@Teste.com ", role, "hash", Agora);

    [Fact]
    public void Criar_DeveNormalizarEmailEValerSeteDias()
    {
        var convite = NovoConvite();

        Assert.Equal("bruno@teste.com", convite.Email);
        Assert.Equal(Agora.AddDays(7), convite.ExpiraEm);
        Assert.Equal("Pendente", convite.ObterStatus(Agora));
    }

    [Fact]
    public void Criar_ComRoleInvalida_DeveLancarDomainException()
    {
        Assert.Throws<DomainException>(() => NovoConvite("Gerente"));
    }

    [Fact]
    public void Aceitar_DuasVezes_DeveLancarDomainException()
    {
        var convite = NovoConvite();
        convite.Aceitar(Agora);

        Assert.Equal("Aceito", convite.ObterStatus(Agora));
        Assert.Throws<DomainException>(() => convite.Aceitar(Agora));
    }

    [Fact]
    public void Aceitar_DepoisDoPrazo_DeveLancarDomainException()
    {
        var convite = NovoConvite();

        Assert.Equal("Expirado", convite.ObterStatus(Agora.AddDays(7)));
        Assert.Throws<DomainException>(() => convite.Aceitar(Agora.AddDays(7)));
    }

    [Fact]
    public void Aceitar_DepoisDeCancelado_DeveLancarDomainException()
    {
        var convite = NovoConvite();
        convite.Cancelar(Agora);

        Assert.Equal("Cancelado", convite.ObterStatus(Agora));
        Assert.Throws<DomainException>(() => convite.Aceitar(Agora));
    }

    [Fact]
    public void Cancelar_DepoisDeAceito_DeveLancarDomainException()
    {
        var convite = NovoConvite();
        convite.Aceitar(Agora);

        Assert.Throws<DomainException>(() => convite.Cancelar(Agora));
    }
}
