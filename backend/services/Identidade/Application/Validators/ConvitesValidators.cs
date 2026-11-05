using FluentValidation;
using GestaoRestaurante.Core.Seguranca;
using GestaoRestaurante.Identidade.Application.Abstractions.Services;
using GestaoRestaurante.Identidade.Application.UseCases.Convites;

namespace GestaoRestaurante.Identidade.Application.Validators;

public class CriarConviteCommandValidator : AbstractValidator<CriarConviteCommand>
{
    public CriarConviteCommandValidator(IIdentityService identityService)
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .EmailAddress().WithMessage("O e-mail informado não é válido.")
            .MaximumLength(200).WithMessage("O e-mail deve ter no máximo 200 caracteres.")
            .MustAsync(async (email, ct) => !await identityService.EmailExisteAsync(email.Trim(), ct))
            .WithMessage("Este e-mail já tem uma conta no sistema.");

        RuleFor(command => command.Role)
            .Must(role => role is RoleNames.Admin or RoleNames.Funcionario)
            .WithMessage($"A role tem que ser '{RoleNames.Admin}' ou '{RoleNames.Funcionario}'.");
    }
}

public class AceitarConviteCommandValidator : AbstractValidator<AceitarConviteCommand>
{
    public AceitarConviteCommandValidator()
    {
        RuleFor(command => command.Token)
            .NotEmpty().WithMessage("O token do convite é obrigatório.");

        RuleFor(command => command.Nome)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MaximumLength(150).WithMessage("O nome deve ter no máximo 150 caracteres.");

        RuleFor(command => command.Senha)
            .NotEmpty().WithMessage("A senha é obrigatória.");
    }
}

public class ObterConvitePorTokenQueryValidator : AbstractValidator<ObterConvitePorTokenQuery>
{
    public ObterConvitePorTokenQueryValidator()
    {
        RuleFor(query => query.Token)
            .NotEmpty().WithMessage("O token do convite é obrigatório.");
    }
}
