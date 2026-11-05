using FluentValidation;
using GestaoRestaurante.Core.Seguranca;
using GestaoRestaurante.Identidade.Application.UseCases.Usuarios;

namespace GestaoRestaurante.Identidade.Application.Validators;

public class AlterarRoleUsuarioCommandValidator : AbstractValidator<AlterarRoleUsuarioCommand>
{
    public AlterarRoleUsuarioCommandValidator()
    {
        RuleFor(command => command.UsuarioId)
            .NotEqual(Guid.Empty).WithMessage("O Id do usuário é obrigatório.");

        RuleFor(command => command.Role)
            .Must(role => role is RoleNames.Admin or RoleNames.Funcionario)
            .WithMessage($"A role tem que ser '{RoleNames.Admin}' ou '{RoleNames.Funcionario}'.");
    }
}

public class AlterarAtivoUsuarioCommandValidator : AbstractValidator<AlterarAtivoUsuarioCommand>
{
    public AlterarAtivoUsuarioCommandValidator()
    {
        RuleFor(command => command.UsuarioId)
            .NotEqual(Guid.Empty).WithMessage("O Id do usuário é obrigatório.");
    }
}
