using GestaoRestaurante.Identidade.Domain.Entities;

namespace GestaoRestaurante.Identidade.Application.Abstractions.Repositories;

public interface INotificacaoPlataformaRepository
{
    void Adicionar(NotificacaoPlataforma notificacao);

    Task<bool> ExisteChaveAsync(string chave, CancellationToken cancellationToken);

    Task<(IReadOnlyList<NotificacaoPlataforma> Itens, int Total)> ListarAsync(int pagina, int tamanhoPagina, bool soNaoLidas, CancellationToken cancellationToken);

    Task<int> ContarNaoLidasAsync(CancellationToken cancellationToken);

    Task<int> MarcarComoLidasAsync(Guid? id, DateTime agora, CancellationToken cancellationToken);
}
