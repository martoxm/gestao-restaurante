namespace GestaoRestaurante.Core.Application.Abstractions;

public interface IUsuarioAtualService
{
    Guid? RestauranteId { get; }

    Guid ObterRestauranteIdObrigatorio();
}
