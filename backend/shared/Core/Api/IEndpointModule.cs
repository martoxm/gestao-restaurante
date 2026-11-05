using Microsoft.AspNetCore.Routing;
namespace GestaoRestaurante.Core.Api;

public interface IEndpointModule
{
    void MapEndpoints(IEndpointRouteBuilder app);
}
