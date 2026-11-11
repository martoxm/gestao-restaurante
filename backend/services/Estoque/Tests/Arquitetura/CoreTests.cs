using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace GestaoRestaurante.Tests.Arquitetura;

public class CoreTests
{
    private static readonly string[] NamespacesDosServicos =
    [
        "GestaoRestaurante.Domain", "GestaoRestaurante.Application",
        "GestaoRestaurante.Infrastructure", "GestaoRestaurante.Contracts", "GestaoRestaurante.Api",
        "GestaoRestaurante.Identidade", "GestaoRestaurante.Vendas",
        "GestaoRestaurante.Pagamentos", "GestaoRestaurante.Email",
    ];

    [Theory]
    [InlineData("GestaoRestaurante.Core")]
    [InlineData("GestaoRestaurante.Core.Persistencia")]
    public void Core_NaoDeveDependerDeNenhumServico(string assembly)
    {
        var resultado = Types.InAssembly(Assembly.Load(assembly))
            .ShouldNot().HaveDependencyOnAny(NamespacesDosServicos).GetResult();

        Assert.True(resultado.IsSuccessful,
            "Quebraram a regra: " + string.Join(", ", resultado.FailingTypeNames ?? []));
    }
}
