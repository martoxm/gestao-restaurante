using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace GestaoRestaurante.Vendas.Tests.Arquitetura;

public class CamadasTests
{
    private const string Prefixo = "GestaoRestaurante.Vendas";

    private static Assembly Carregar(string camada) => Assembly.Load($"{Prefixo}.{camada}");

    private static void Conferir(TestResult resultado) =>
        Assert.True(resultado.IsSuccessful,
            "Quebraram a regra: " + string.Join(", ", resultado.FailingTypeNames ?? []));

    [Theory]
    [InlineData("Microsoft.EntityFrameworkCore")]
    [InlineData("MediatR")]
    [InlineData("Microsoft.AspNetCore")]
    public void Domain_NaoDeveDependerDe(string pacote) =>
        Conferir(Types.InAssembly(Carregar("Domain")).ShouldNot().HaveDependencyOn(pacote).GetResult());

    [Fact]
    public void Contracts_NaoDeveDependerDoDomain() =>
        Conferir(Types.InAssembly(Carregar("Contracts")).ShouldNot().HaveDependencyOn($"{Prefixo}.Domain").GetResult());

    [Fact]
    public void Application_NaoDeveDependerDaInfrastructure() =>
        Conferir(Types.InAssembly(Carregar("Application")).ShouldNot().HaveDependencyOn($"{Prefixo}.Infrastructure").GetResult());
}
