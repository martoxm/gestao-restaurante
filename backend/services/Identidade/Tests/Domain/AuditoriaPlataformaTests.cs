using GestaoRestaurante.Core.Domain;
using GestaoRestaurante.Identidade.Domain.Entities;
using GestaoRestaurante.Identidade.Domain.Enums;

namespace GestaoRestaurante.Identidade.Tests.Domain;

public class AuditoriaPlataformaTests
{
    private static readonly DateTime Agora = new(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Registrar_ComDadosValidos_DeveGuardarTudo()
    {
        var superAdminId = Guid.NewGuid();
        var restauranteId = Guid.NewGuid();

        var auditoria = AuditoriaPlataforma.Registrar(
            superAdminId, " dono@plataforma.com ", "MudarPlano", Agora,
            restauranteId: restauranteId, valorAntes: "Teste", valorDepois: "Pro");

        Assert.Equal(superAdminId, auditoria.SuperAdminId);
        Assert.Equal("dono@plataforma.com", auditoria.EmailSuperAdmin);
        Assert.Equal("MudarPlano", auditoria.Acao);
        Assert.Equal(restauranteId, auditoria.RestauranteId);
        Assert.Equal("Teste", auditoria.ValorAntes);
        Assert.Equal("Pro", auditoria.ValorDepois);
        Assert.Equal(Agora, auditoria.OcorridoEm);
    }

    [Fact]
    public void Registrar_SoComUsuarioAlvo_DeveAceitar()
    {
        var auditoria = AuditoriaPlataforma.Registrar(
            Guid.NewGuid(), "dono@plataforma.com", "ForcarLogout", Agora, usuarioAlvoId: Guid.NewGuid());

        Assert.Null(auditoria.RestauranteId);
        Assert.NotNull(auditoria.UsuarioAlvoId);
    }

    [Fact]
    public void Registrar_SemRestauranteNemUsuario_DeveLancar()
    {
        Assert.Throws<DomainException>(() =>
            AuditoriaPlataforma.Registrar(Guid.NewGuid(), "dono@plataforma.com", "MudarPlano", Agora));
    }

    [Fact]
    public void Registrar_SemSuperAdmin_DeveLancar()
    {
        Assert.Throws<DomainException>(() =>
            AuditoriaPlataforma.Registrar(Guid.Empty, "dono@plataforma.com", "MudarPlano", Agora, restauranteId: Guid.NewGuid()));
    }


    [Fact]
    public void MudarPlano_ParaPago_DeveApagarOPrazoDoTeste()
    {
        var restaurante = Restaurante.Criar("Cantina", Agora);

        restaurante.MudarPlano(PlanoRestaurante.Basico, Agora);

        Assert.Equal(PlanoRestaurante.Basico, restaurante.Plano);
        Assert.Null(restaurante.TesteTerminaEm);
        Assert.Equal("Pago", restaurante.ObterSituacao(Agora));
    }

    [Fact]
    public void MudarPlano_DeVoltaProTeste_DeveDarPrazoNovoAPartirDeAgora()
    {
        var restaurante = Restaurante.Criar("Cantina", Agora);
        restaurante.MudarPlano(PlanoRestaurante.Pro, Agora);
        var depois = Agora.AddDays(100);

        restaurante.MudarPlano(PlanoRestaurante.Teste, depois);

        Assert.Equal(depois.AddDays(Restaurante.DiasTeste), restaurante.TesteTerminaEm);
    }

    [Fact]
    public void MudarPlano_ProMesmoPlano_DeveLancar()
    {
        var restaurante = Restaurante.Criar("Cantina", Agora);

        Assert.Throws<DomainException>(() => restaurante.MudarPlano(PlanoRestaurante.Teste, Agora));
    }

    [Fact]
    public void EstenderTeste_JaVencido_DeveContarAPartirDeAgora()
    {
        var restaurante = Restaurante.Criar("Cantina", Agora);
        var muitoDepois = Agora.AddDays(30);

        restaurante.EstenderTeste(5, muitoDepois);

        Assert.Equal(muitoDepois.AddDays(5), restaurante.TesteTerminaEm);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(91)]
    public void EstenderTeste_DiasForaDoLimite_DeveLancar(int dias)
    {
        var restaurante = Restaurante.Criar("Cantina", Agora);

        Assert.Throws<DomainException>(() => restaurante.EstenderTeste(dias, Agora));
    }

    [Fact]
    public void EstenderTeste_EmPlanoPago_DeveLancar()
    {
        var restaurante = Restaurante.Criar("Cantina", Agora);
        restaurante.MudarPlano(PlanoRestaurante.Pro, Agora);

        Assert.Throws<DomainException>(() => restaurante.EstenderTeste(5, Agora));
    }

    [Fact]
    public void ObterSituacao_DeveSeguirOPrazoDoTeste()
    {
        var restaurante = Restaurante.Criar("Cantina", Agora);

        Assert.Equal("EmTeste", restaurante.ObterSituacao(Agora));
        Assert.Equal("VencendoEmBreve", restaurante.ObterSituacao(Agora.AddDays(Restaurante.DiasTeste - 2)));
        Assert.Equal("TesteVencido", restaurante.ObterSituacao(Agora.AddDays(Restaurante.DiasTeste)));

        restaurante.DefinirAtivo(false);
        Assert.Equal("Inativo", restaurante.ObterSituacao(Agora));
    }

    [Fact]
    public void DefinirAtivo_NoMesmoEstado_DeveLancar()
    {
        var restaurante = Restaurante.Criar("Cantina", Agora);

        Assert.Throws<DomainException>(() => restaurante.DefinirAtivo(true));
    }

    [Fact]
    public void Registrar_SemAcao_DeveLancar()
    {
        Assert.Throws<DomainException>(() =>
            AuditoriaPlataforma.Registrar(Guid.NewGuid(), "dono@plataforma.com", " ", Agora, restauranteId: Guid.NewGuid()));
    }
}
