using System.Text;
using System.Text.Json;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Core.Mensageria.Contratos;

namespace GestaoRestaurante.Tests.Contratos;

public class ContratosMensagensTests
{
    private static readonly Guid VendaId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid RestauranteId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ProdutoId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly DateTime Data = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static (string Json, T Lida) IdaEVolta<T>(T mensagem) where T : class
    {
        var json = OutboxMensagem.Criar("fila-teste", mensagem, Data).Conteudo;
        var corpo = Encoding.UTF8.GetBytes(json);
        var lida = JsonSerializer.Deserialize<T>(corpo.AsSpan())!;
        return (json, lida);
    }

    private static void AssertVoltouIgual<T>(T original, T lida) =>
        Assert.Equal(JsonSerializer.Serialize(original), JsonSerializer.Serialize(lida));

    public static TheoryData<object, string> Contratos => new()
    {
        {
            new BaixarEstoqueMensagem(VendaId, RestauranteId, [new ItemBaixaEstoque(ProdutoId, 2)], 42),
            """{"VendaId":"11111111-1111-1111-1111-111111111111","RestauranteId":"22222222-2222-2222-2222-222222222222","Itens":[{"ProdutoId":"33333333-3333-3333-3333-333333333333","Quantidade":2}],"NumeroVenda":42}"""
        },
        {
            new DevolverEstoqueMensagem(VendaId, RestauranteId, [new ItemBaixaEstoque(ProdutoId, 2)], "Pagamento recusado", 42),
            """{"VendaId":"11111111-1111-1111-1111-111111111111","RestauranteId":"22222222-2222-2222-2222-222222222222","Itens":[{"ProdutoId":"33333333-3333-3333-3333-333333333333","Quantidade":2}],"Motivo":"Pagamento recusado","NumeroVenda":42}"""
        },
        {
            new ResultadoBaixaEstoqueMensagem(VendaId, RestauranteId, false, "Sem saldo"),
            """{"VendaId":"11111111-1111-1111-1111-111111111111","RestauranteId":"22222222-2222-2222-2222-222222222222","Baixado":false,"Motivo":"Sem saldo"}"""
        },
        {
            new ProcessarPagamentoMensagem(VendaId, RestauranteId, 15.5m, "tok_aprovado"),
            """{"VendaId":"11111111-1111-1111-1111-111111111111","RestauranteId":"22222222-2222-2222-2222-222222222222","Valor":15.5,"TokenCartao":"tok_aprovado"}"""
        },
        {
            new ResultadoPagamentoMensagem(VendaId, RestauranteId, ProdutoId, true, null),
            """{"VendaId":"11111111-1111-1111-1111-111111111111","RestauranteId":"22222222-2222-2222-2222-222222222222","PagamentoId":"33333333-3333-3333-3333-333333333333","Aprovado":true,"Motivo":null}"""
        },
        {
            new EnviarEmailMensagem(["admin@teste.com"], "Assunto", "<p>Oi</p>"),
            """{"Destinatarios":["admin@teste.com"],"Assunto":"Assunto","CorpoHtml":"<p>Oi</p>"}""".Replace("<", "\\u003C").Replace(">", "\\u003E")
        },
        {
            new AvisarAdminsRestauranteMensagem(RestauranteId, "Assunto", "Corpo"),
            """{"RestauranteId":"22222222-2222-2222-2222-222222222222","Assunto":"Assunto","CorpoHtml":"Corpo"}"""
        },
        {
            new PlanoPagoMensagem(VendaId, RestauranteId, "Pro", 30, Data),
            """{"CobrancaId":"11111111-1111-1111-1111-111111111111","RestauranteId":"22222222-2222-2222-2222-222222222222","Plano":"Pro","Dias":30,"PagoEm":"2026-01-01T12:00:00Z"}"""
        },
        {
            new ProdutoCatalogoMensagem(ProdutoId, RestauranteId, "Pão", 1.25m, false, Data),
            """{"ProdutoId":"33333333-3333-3333-3333-333333333333","RestauranteId":"22222222-2222-2222-2222-222222222222","Nome":"Pão","Preco":1.25,"Excluido":false,"OcorridoEm":"2026-01-01T12:00:00Z"}""".Replace("ã", "\\u00E3")
        },
    };

    [Theory]
    [MemberData(nameof(Contratos))]
    public void Contrato_DeveSerializarNoFormatoTravadoEVoltarIgual(object mensagem, string jsonEsperado)
    {
        var metodo = typeof(ContratosMensagensTests)
            .GetMethod(nameof(Verificar), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(mensagem.GetType());

        metodo.Invoke(null, [mensagem, jsonEsperado]);
    }

    private static void Verificar<T>(T mensagem, string jsonEsperado) where T : class
    {
        var (json, lida) = IdaEVolta(mensagem);
        Assert.Equal(jsonEsperado, json);
        AssertVoltouIgual(mensagem, lida);
    }

    [Fact]
    public void TodoContratoDoCore_DeveEstarNaListaDeTestes()
    {
        var testados = Contratos.Select(linha => linha[0].GetType()).ToHashSet();
        var contratos = typeof(BaixarEstoqueMensagem).Assembly.GetTypes()
            .Where(t => t.Namespace == typeof(BaixarEstoqueMensagem).Namespace && t.Name.EndsWith("Mensagem"))
            .ToList();

        Assert.All(contratos, t => Assert.Contains(t, testados));
    }

    [Fact]
    public void BaixarEstoque_JsonAntigoSemNumeroVenda_DeveLerComNumeroNulo()
    {
        const string antigo = """{"VendaId":"11111111-1111-1111-1111-111111111111","RestauranteId":"22222222-2222-2222-2222-222222222222","Itens":[{"ProdutoId":"33333333-3333-3333-3333-333333333333","Quantidade":2}]}""";

        var lida = JsonSerializer.Deserialize<BaixarEstoqueMensagem>(Encoding.UTF8.GetBytes(antigo).AsSpan())!;

        Assert.Equal(VendaId, lida.VendaId);
        Assert.Single(lida.Itens);
        Assert.Null(lida.NumeroVenda);
    }

    [Fact]
    public void DevolverEstoque_JsonAntigoSemNumeroVenda_DeveLerComNumeroNulo()
    {
        const string antigo = """{"VendaId":"11111111-1111-1111-1111-111111111111","RestauranteId":"22222222-2222-2222-2222-222222222222","Itens":[],"Motivo":"Recusado"}""";

        var lida = JsonSerializer.Deserialize<DevolverEstoqueMensagem>(Encoding.UTF8.GetBytes(antigo).AsSpan())!;

        Assert.Equal("Recusado", lida.Motivo);
        Assert.Null(lida.NumeroVenda);
    }
}
