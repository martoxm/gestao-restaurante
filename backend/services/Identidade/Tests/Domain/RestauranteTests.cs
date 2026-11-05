using GestaoRestaurante.Core.Domain;
using GestaoRestaurante.Identidade.Domain.Entities;
using GestaoRestaurante.Identidade.Domain.Enums;

namespace GestaoRestaurante.Identidade.Tests.Domain;

public class RestauranteTests
{
    private static readonly DateTime CriadoEm = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Criar_DeveNascerNoTesteAtivoComPrazoDe14Dias()
    {
        var restaurante = Restaurante.Criar("Cantina", CriadoEm);

        Assert.Equal(PlanoRestaurante.Teste, restaurante.Plano);
        Assert.True(restaurante.Ativo);
        Assert.Equal(CriadoEm.AddDays(14), restaurante.TesteTerminaEm);
    }

    [Fact]
    public void EstaLiberado_DentroDoPrazoDoTeste_DeveSerVerdadeiro()
    {
        var restaurante = Restaurante.Criar("Cantina", CriadoEm);

        Assert.True(restaurante.EstaLiberado(CriadoEm.AddDays(13)));
    }

    [Fact]
    public void EstaLiberado_DepoisDoPrazoDoTeste_DeveSerFalso()
    {
        var restaurante = Restaurante.Criar("Cantina", CriadoEm);

        Assert.False(restaurante.EstaLiberado(CriadoEm.AddDays(14)));
    }

    [Fact]
    public void Criar_SemNome_DeveLancarDomainException()
    {
        Assert.Throws<DomainException>(() => Restaurante.Criar(" ", CriadoEm));
    }

    [Fact]
    public void GarantirQueCabeMaisUmUsuario_ComVagaSobrando_NaoDeveLancar()
    {
        var restaurante = Restaurante.Criar("Cantina", CriadoEm);

        restaurante.GarantirQueCabeMaisUmUsuario(2);
    }

    [Fact]
    public void GarantirQueCabeMaisUmUsuario_PlanoTesteCheio_DeveLancarDomainException()
    {
        var restaurante = Restaurante.Criar("Cantina", CriadoEm);

        Assert.Equal(3, restaurante.LimiteUsuarios);
        Assert.Throws<DomainException>(() => restaurante.GarantirQueCabeMaisUmUsuario(3));
    }
}
