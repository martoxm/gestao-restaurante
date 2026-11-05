using GestaoRestaurante.Core.Application.Abstractions;
namespace GestaoRestaurante.Identidade.Application.Abstractions.Services;

public interface IJwtTokenGenerator
{
    string GerarToken(Guid usuarioId, string nome, string email, IEnumerable<string> roles, Guid? restauranteId, bool restauranteLiberado = true, bool emailConfirmado = true);
}
