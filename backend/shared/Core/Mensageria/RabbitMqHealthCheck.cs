using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GestaoRestaurante.Core.Mensageria;

public sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly ConexaoRabbitMq _conexao;

    public RabbitMqHealthCheck(ConexaoRabbitMq conexao)
    {
        _conexao = conexao;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            limite.CancelAfter(TimeSpan.FromSeconds(5));

            var conexao = await _conexao.ObterAsync(limite.Token);
            return conexao.IsOpen
                ? HealthCheckResult.Healthy("Conectado ao RabbitMQ.")
                : HealthCheckResult.Degraded("A conexão com o RabbitMQ está fechada.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("Não deu pra conectar no RabbitMQ.", ex);
        }
    }
}
