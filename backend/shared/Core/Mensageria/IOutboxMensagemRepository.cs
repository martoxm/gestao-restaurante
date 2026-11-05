namespace GestaoRestaurante.Core.Mensageria;

public interface IOutboxMensagemRepository
{
    void Adicionar(OutboxMensagem mensagem);

    void Atualizar(OutboxMensagem mensagem);

    Task<IReadOnlyList<OutboxMensagem>> ListarPendentesAsync(int quantidade, DateTime agora, CancellationToken cancellationToken);
}
