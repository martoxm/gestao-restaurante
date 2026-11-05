using GestaoRestaurante.Core.Application.Abstractions;
using GestaoRestaurante.Application.Abstractions.Services;
using GestaoRestaurante.Core.Application.Exceptions;

namespace GestaoRestaurante.Tests.Common;

public class FakeUsuarioAtualService : IUsuarioAtualService, IDefinidorRestauranteAtual
{
    public Guid? RestauranteId { get; set; }

    public void Definir(Guid restauranteId) => RestauranteId = restauranteId;

    public Guid ObterRestauranteIdObrigatorio() =>
        RestauranteId ?? throw new SessaoInvalidaException();
}
