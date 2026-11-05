using Microsoft.EntityFrameworkCore;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Core.Persistencia.Mensageria;
using GestaoRestaurante.Tests.Integration.Fixtures;

namespace GestaoRestaurante.Tests.Integration.Repositories;

[Collection(nameof(SqlServerCollection))]
public sealed class LimpezaMensageriaTests(SqlServerFixture fixture)
{
    private static readonly DateTime Agora = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    [DockerFact]
    public async Task LimparAsync_DeveApagarSoOQueJaFoiProcessadoHaMaisDe7Dias()
    {
        if (!fixture.EstaDisponivel)
            return;

        var velha = OutboxMensagem.Criar(Filas.Email, new { Teste = 1 }, Agora.AddDays(-20));
        velha.MarcarComoProcessado(Agora.AddDays(-10));
        var recente = OutboxMensagem.Criar(Filas.Email, new { Teste = 2 }, Agora.AddDays(-3));
        recente.MarcarComoProcessado(Agora.AddDays(-2));
        var pendenteVelha = OutboxMensagem.Criar(Filas.Email, new { Teste = 3 }, Agora.AddDays(-30));

        var processadaVelha = MensagemProcessada.Criar(Guid.NewGuid(), Filas.Email, Agora.AddDays(-8));
        var processadaRecente = MensagemProcessada.Criar(Guid.NewGuid(), Filas.Email, Agora.AddDays(-1));

        await using (var contexto = fixture.CriarContexto())
        {
            contexto.Set<OutboxMensagem>().AddRange(velha, recente, pendenteVelha);
            contexto.Set<MensagemProcessada>().AddRange(processadaVelha, processadaRecente);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = fixture.CriarContexto())
        {
            await LimpezaMensageria.LimparAsync(contexto, Agora, CancellationToken.None);
        }

        await using var leitura = fixture.CriarContexto();
        var outbox = await leitura.Set<OutboxMensagem>().Select(m => m.Id).ToListAsync();
        var processadas = await leitura.Set<MensagemProcessada>().Select(m => m.Id).ToListAsync();

        Assert.DoesNotContain(velha.Id, outbox);
        Assert.Contains(recente.Id, outbox);
        Assert.Contains(pendenteVelha.Id, outbox);
        Assert.DoesNotContain(processadaVelha.Id, processadas);
        Assert.Contains(processadaRecente.Id, processadas);
    }
}
