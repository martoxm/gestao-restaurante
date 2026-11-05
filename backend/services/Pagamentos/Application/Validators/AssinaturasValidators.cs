using FluentValidation;
using GestaoRestaurante.Pagamentos.Application.UseCases.Assinaturas;
using GestaoRestaurante.Pagamentos.Domain.Enums;

namespace GestaoRestaurante.Pagamentos.Application.Validators;

public class IniciarAssinaturaCommandValidator : AbstractValidator<IniciarAssinaturaCommand>
{
    public IniciarAssinaturaCommandValidator()
    {
        RuleFor(command => command.Plano)
            .NotEmpty().WithMessage("Escolha um plano.")
            .Must(plano => Enum.TryParse<PlanoPago>(plano, ignoreCase: true, out var valor) && Enum.IsDefined(valor))
            .WithMessage("Plano inválido. Use Basico ou Pro.");
    }
}
