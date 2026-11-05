using MediatR;

namespace GestaoRestaurante.Email.Application.UseCases.Emails;

public record EnviarEmailCommand(
    Guid MensagemId,
    IReadOnlyList<string> Destinatarios,
    string Assunto,
    string CorpoHtml) : IRequest<bool>;
