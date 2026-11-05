namespace GestaoRestaurante.Application.Abstractions.Services;

public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

}
