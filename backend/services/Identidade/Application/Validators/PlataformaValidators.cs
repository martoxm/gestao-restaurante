using FluentValidation;
using GestaoRestaurante.Identidade.Application.UseCases.Plataforma;
using GestaoRestaurante.Identidade.Domain.Entities;
using GestaoRestaurante.Identidade.Domain.Enums;

namespace GestaoRestaurante.Identidade.Application.Validators;


public class MudarPlanoRestauranteCommandValidator : AbstractValidator<MudarPlanoRestauranteCommand>
{
    public MudarPlanoRestauranteCommandValidator()
    {
        RuleFor(command => command.Plano)
            .Must(plano => Enum.TryParse<PlanoRestaurante>(plano, ignoreCase: true, out var valor) && Enum.IsDefined(valor))
            .WithMessage("O plano tem que ser 'Teste', 'Basico' ou 'Pro'.");
    }
}

public class EstenderTesteRestauranteCommandValidator : AbstractValidator<EstenderTesteRestauranteCommand>
{
    public EstenderTesteRestauranteCommandValidator()
    {
        RuleFor(command => command.Dias)
            .InclusiveBetween(1, Restaurante.MaximoDiasExtensao)
            .WithMessage($"A extensão tem que ser de 1 a {Restaurante.MaximoDiasExtensao} dias.");
    }
}

public class ListarRestaurantesPlataformaQueryValidator : AbstractValidator<ListarRestaurantesPlataformaQuery>
{
    private static readonly string[] Status = ["ativo", "inativo", "pago", "emteste", "vencendoembreve", "testevencido"];

    public ListarRestaurantesPlataformaQueryValidator()
    {
        RuleFor(query => query.Filtro.Pagina).GreaterThanOrEqualTo(1).WithMessage("A página tem que ser 1 ou mais.");

        RuleFor(query => query.Filtro.TamanhoPagina)
            .InclusiveBetween(1, 100).WithMessage("O tamanho da página tem que ser de 1 a 100.");

        RuleFor(query => query.Filtro.Plano)
            .Must(plano => plano is null || Enum.TryParse<PlanoRestaurante>(plano, ignoreCase: true, out var valor) && Enum.IsDefined(valor))
            .WithMessage("O plano tem que ser 'Teste', 'Basico' ou 'Pro'.");

        RuleFor(query => query.Filtro.Status)
            .Must(status => status is null || Status.Contains(status.Trim().ToLowerInvariant()))
            .WithMessage("Status inválido. Use Ativo, Inativo, Pago, EmTeste, VencendoEmBreve ou TesteVencido.");
    }
}

public class ListarAuditoriaQueryValidator : AbstractValidator<ListarAuditoriaQuery>
{
    public ListarAuditoriaQueryValidator()
    {
        RuleFor(query => query.Filtro.Pagina).GreaterThanOrEqualTo(1).WithMessage("A página tem que ser 1 ou mais.");

        RuleFor(query => query.Filtro.TamanhoPagina)
            .InclusiveBetween(1, 100).WithMessage("O tamanho da página tem que ser de 1 a 100.");
    }
}

public class ListarNotificacoesPlataformaQueryValidator : AbstractValidator<ListarNotificacoesPlataformaQuery>
{
    public ListarNotificacoesPlataformaQueryValidator()
    {
        RuleFor(query => query.Filtro.Pagina).GreaterThanOrEqualTo(1).WithMessage("A página tem que ser 1 ou mais.");

        RuleFor(query => query.Filtro.TamanhoPagina)
            .InclusiveBetween(1, 100).WithMessage("O tamanho da página tem que ser de 1 a 100.");
    }
}
