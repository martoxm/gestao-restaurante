using FluentValidation;
using RestauranteEstoque.Application.UseCases.MovimentacoesEstoque;
using RestauranteEstoque.Domain.Enums;

namespace RestauranteEstoque.Application.Validators;

public class CriarMovimentacaoEstoqueCommandValidator : AbstractValidator<CriarMovimentacaoEstoqueCommand>
{
    public CriarMovimentacaoEstoqueCommandValidator()
    {
        RuleFor(command => command.ProdutoId)
            .NotEqual(Guid.Empty).WithMessage("A movimentação precisa estar vinculada a um produto.");

        RuleFor(command => command.Tipo)
            .IsInEnum().WithMessage("O tipo de movimentação deve ser Entrada ou Saida.");

        RuleFor(command => command.Quantidade)
            .GreaterThan(0).WithMessage("A quantidade movimentada deve ser maior que zero.");

        RuleFor(command => command.Observacoes)
            .MaximumLength(500).WithMessage("As observações devem ter no máximo 500 caracteres.");
    }
}

public class ListarMovimentacoesEstoqueQueryValidator : AbstractValidator<ListarMovimentacoesEstoqueQuery>
{
    public ListarMovimentacoesEstoqueQueryValidator()
    {
        RuleFor(query => query.Pagina)
            .GreaterThanOrEqualTo(1).WithMessage("A página deve ser maior ou igual a 1.");

        RuleFor(query => query.TamanhoPagina)
            .InclusiveBetween(1, 100).WithMessage("O tamanho da página deve estar entre 1 e 100.");

        RuleFor(query => query.Tipo)
            .Must(tipo => string.IsNullOrWhiteSpace(tipo) || Enum.TryParse<TipoMovimentacaoEstoque>(tipo, ignoreCase: true, out _))
            .WithMessage("O tipo deve ser Entrada ou Saida.");

        RuleFor(query => query.DataFim)
            .GreaterThanOrEqualTo(query => query.DataInicio)
            .When(query => query.DataInicio.HasValue && query.DataFim.HasValue)
            .WithMessage("A data final não pode ser antes da data inicial.");
    }
}
