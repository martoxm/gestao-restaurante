namespace GestaoRestaurante.Core.Mensageria;

public interface IMensagemProcessadaRepository
{
    Task<bool> ExisteAsync(Guid mensagemId, CancellationToken cancellationToken);

    void Adicionar(MensagemProcessada mensagem);
}
