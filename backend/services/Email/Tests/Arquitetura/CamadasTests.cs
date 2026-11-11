using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace GestaoRestaurante.Email.Tests.Arquitetura;

public class CamadasTests
{
    private const string Prefixo = "GestaoRestaurante.Email";

    private static Assembly Carregar(string camada) => Assembly.Load($"{Prefixo}.{camada}");

    private static void Conferir(TestResult resultado) =>
        Assert.True(resultado.IsSuccessful,
            "Quebraram a regra: " + string.Join(", ", resultado.FailingTypeNames ?? []));

    [Fact]
    public void Application_NaoDeveDependerDaInfrastructure() =>
        Conferir(Types.InAssembly(Carregar("Application")).ShouldNot().HaveDependencyOn($"{Prefixo}.Infrastructure").GetResult());
}
