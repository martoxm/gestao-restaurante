namespace GestaoRestaurante.Email.Application.Abstractions.Services;

public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}


public interface IEmailService
{
    Task<bool> EnviarAsync(IEnumerable<string> destinatarios, string assunto, string corpoHtml, CancellationToken ct = default);
}
