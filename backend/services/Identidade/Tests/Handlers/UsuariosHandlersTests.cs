using FluentValidation;
using MediatR;
using GestaoRestaurante.Core.Application.Behaviors;
using GestaoRestaurante.Core.Application.Exceptions;
using GestaoRestaurante.Identidade.Application.Validators;
using GestaoRestaurante.Core.Domain;
using GestaoRestaurante.Identidade.Application.Abstractions.Services;
using GestaoRestaurante.Identidade.Application.UseCases.Usuarios;
using GestaoRestaurante.Identidade.Tests.Common;

namespace GestaoRestaurante.Identidade.Tests.Handlers;

public class UsuariosHandlersTests
{
    private readonly Guid _restauranteA = Guid.NewGuid();
    private readonly Guid _restauranteB = Guid.NewGuid();
    private readonly FakeIdentityService _identityService = new();
    private readonly FakeUsuarioAtualService _usuarioAtual = new();

    private readonly UsuarioComRole _adminA = new(Guid.NewGuid(), "Ana", "ana@a.com", "Admin");
    private readonly UsuarioComRole _funcionarioA = new(Guid.NewGuid(), "Bruno", "bruno@a.com", "Funcionario");
    private readonly UsuarioComRole _funcionarioB = new(Guid.NewGuid(), "Carla", "carla@b.com", "Funcionario");

    public UsuariosHandlersTests()
    {
        _identityService.UsuariosPorRestaurante[_restauranteA] = [_funcionarioA, _adminA];
        _identityService.UsuariosPorRestaurante[_restauranteB] = [_funcionarioB];
        _usuarioAtual.RestauranteId = _restauranteA;
    }

    private AlterarRoleUsuarioHandler CriarAlterarHandler() => new(_identityService, _usuarioAtual);

    private string RoleDe(Guid restaurante, Guid usuarioId) =>
        _identityService.UsuariosPorRestaurante[restaurante].Single(u => u.Id == usuarioId).Role;

    [Fact]
    public async Task Listar_DeveTrazerSoOsUsuariosDoRestauranteDoToken_OrdenadosPorNome()
    {
        var resultado = await new ListarUsuariosHandler(_identityService, _usuarioAtual)
            .Handle(new ListarUsuariosQuery(), CancellationToken.None);

        Assert.Equal(["Ana", "Bruno"], resultado.Select(u => u.Nome));
    }

    [Fact]
    public async Task Alterar_PromoverFuncionario_DeveVirarAdmin()
    {
        await CriarAlterarHandler().Handle(new AlterarRoleUsuarioCommand(_funcionarioA.Id, "Admin"), CancellationToken.None);

        Assert.Equal("Admin", RoleDe(_restauranteA, _funcionarioA.Id));
    }

    [Fact]
    public async Task Alterar_RebaixarOUltimoAdmin_DeveLancarDomainException()
    {
        await Assert.ThrowsAsync<DomainException>(() =>
            CriarAlterarHandler().Handle(new AlterarRoleUsuarioCommand(_adminA.Id, "Funcionario"), CancellationToken.None));

        Assert.Equal("Admin", RoleDe(_restauranteA, _adminA.Id));
    }

    [Fact]
    public async Task Alterar_RebaixarAdminComOutroAdmin_DevePermitir()
    {
        var handler = CriarAlterarHandler();
        await handler.Handle(new AlterarRoleUsuarioCommand(_funcionarioA.Id, "Admin"), CancellationToken.None);

        await handler.Handle(new AlterarRoleUsuarioCommand(_adminA.Id, "Funcionario"), CancellationToken.None);

        Assert.Equal("Funcionario", RoleDe(_restauranteA, _adminA.Id));
    }

    [Fact]
    public async Task ValidationBehavior_CommandSemRetornoComRoleInvalida_DeveLancarValidationException()
    {
        var behavior = new ValidationBehavior<AlterarRoleUsuarioCommand, Unit>([new AlterarRoleUsuarioCommandValidator()]);

        await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(new AlterarRoleUsuarioCommand(_funcionarioA.Id, "Gerente"), _ => Task.FromResult(Unit.Value), CancellationToken.None));
    }

    [Fact]
    public async Task Alterar_UsuarioDeOutroRestaurante_DeveLancarNaoEncontrado()
    {
        await Assert.ThrowsAsync<NaoEncontradoException>(() =>
            CriarAlterarHandler().Handle(new AlterarRoleUsuarioCommand(_funcionarioB.Id, "Admin"), CancellationToken.None));

        Assert.Equal("Funcionario", RoleDe(_restauranteB, _funcionarioB.Id));
    }
}
