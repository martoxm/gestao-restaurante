using GestaoRestaurante.Core.Application.Abstractions;
using GestaoRestaurante.Core.Domain;

namespace GestaoRestaurante.Domain.Events;

public sealed record EstoqueFicouBaixoEvent(
    Guid ProdutoId,
    string NomeProduto,
    int QuantidadeAtual,
    Guid RestauranteId) : IDomainEvent;
