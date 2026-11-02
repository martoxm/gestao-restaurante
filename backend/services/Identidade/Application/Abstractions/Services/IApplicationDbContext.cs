namespace RestauranteEstoque.Identidade.Application.Abstractions.Services;

public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<T> ExecutarEmTransacaoAsync<T>(Func<CancellationToken, Task<T>> operacao, CancellationToken cancellationToken);
}
