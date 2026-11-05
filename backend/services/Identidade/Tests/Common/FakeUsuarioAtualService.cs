using GestaoRestaurante.Core.Application.Abstractions;
using GestaoRestaurante.Identidade.Application.Abstractions.Services;
using GestaoRestaurante.Core.Application.Exceptions;

namespace GestaoRestaurante.Identidade.Tests.Common;

public class FakeUsuarioAtualService : IUsuarioAtualService
{
    public Guid? RestauranteId { get; set; }

    public Guid ObterRestauranteIdObrigatorio() =>
        RestauranteId ?? throw new SessaoInvalidaException();
}
