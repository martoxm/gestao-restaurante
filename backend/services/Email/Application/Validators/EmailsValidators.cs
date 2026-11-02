using FluentValidation;
using RestauranteEstoque.Email.Application.UseCases.Emails;

namespace RestauranteEstoque.Email.Application.Validators;

public class EnviarEmailCommandValidator : AbstractValidator<EnviarEmailCommand>
{
    public EnviarEmailCommandValidator()
    {
        RuleFor(x => x.MensagemId).NotEmpty();
        RuleFor(x => x.Destinatarios).NotEmpty().WithMessage("O e-mail precisa de pelo menos um destinatário.");
        RuleForEach(x => x.Destinatarios).NotEmpty().EmailAddress().WithMessage("Destinatário com e-mail inválido.");
        RuleFor(x => x.Assunto).NotEmpty().WithMessage("O assunto é obrigatório.");
    }
}
