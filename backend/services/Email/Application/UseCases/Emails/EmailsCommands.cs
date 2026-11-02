using MediatR;

namespace RestauranteEstoque.Email.Application.UseCases.Emails;

public record EnviarEmailCommand(
    Guid MensagemId,
    IReadOnlyList<string> Destinatarios,
    string Assunto,
    string CorpoHtml) : IRequest<bool>;
