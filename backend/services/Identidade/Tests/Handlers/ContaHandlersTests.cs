using FluentValidation;
using GestaoRestaurante.Core.Domain;
using GestaoRestaurante.Core.Mensageria.Contratos;
using GestaoRestaurante.Identidade.Application.Abstractions.Services;
using GestaoRestaurante.Identidade.Application.UseCases.Auth;
using GestaoRestaurante.Identidade.Application.UseCases.Usuarios;
using GestaoRestaurante.Identidade.Domain.Entities;
using GestaoRestaurante.Identidade.Tests.Common;

namespace GestaoRestaurante.Identidade.Tests.Handlers;

public class ContaHandlersTests
{
    private readonly FakeIdentityService _identityService = new();
    private readonly FakeOutboxMensagemRepository _outbox = new();
    private readonly FakeRelogio _relogio = new();
    private readonly FakeUsuarioAtualService _usuarioAtual = new();
    private readonly TestDbContext _context;
    private readonly Restaurante _restaurante;
    private readonly Guid _anaId;

    public ContaHandlersTests()
    {
        _context = TestDbContextFactory.Create(_usuarioAtual);
        _restaurante = Restaurante.Criar("Cantina", _relogio.AgoraUtc);
        _context.Restaurantes.Add(_restaurante);
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
        _usuarioAtual.RestauranteId = _restaurante.Id;
        _anaId = _identityService.CriarUsuarioAsync("Ana", "ana@teste.com", "senha123", _restaurante.Id, "Admin", false, default).Result;
    }

    [Fact]
    public async Task EsqueciSenha_EmailExistente_DeveMandarLinkPeloOutbox()
    {
        await EsqueciHandler().Handle(new EsqueciSenhaCommand("Ana@Teste.com "), CancellationToken.None);

        var mensagem = Assert.Single(_outbox.Mensagens);
        Assert.Contains($"tok-{_anaId}", mensagem.Conteudo);
    }

    [Fact]
    public async Task EsqueciSenha_EmailQueNaoExiste_NaoDeveMandarNadaNemFalhar()
    {
        await EsqueciHandler().Handle(new EsqueciSenhaCommand("ninguem@teste.com"), CancellationToken.None);

        Assert.Empty(_outbox.Mensagens);
    }

    [Fact]
    public async Task RedefinirSenha_DeveTrocarASenhaEDerrubarAsSessoes()
    {
        _context.RefreshTokens.Add(RefreshToken.Criar(_anaId, "hash-x", _relogio.AgoraUtc, _relogio.AgoraUtc.AddDays(14)));
        _context.SaveChanges();
        _context.ChangeTracker.Clear();

        await new RedefinirSenhaHandler(_identityService, new FakeRefreshTokenRepository(_context), _context, _relogio)
            .Handle(new RedefinirSenhaCommand("ana@teste.com", $"tok-{_anaId}", "novaSenha1"), CancellationToken.None);

        Assert.Equal("novaSenha1", _identityService.SenhasRedefinidas[_anaId]);
        Assert.Empty(_context.RefreshTokens);
    }

    [Fact]
    public async Task ConfirmarEmail_TokenErrado_DeveLancarDomainException()
    {
        await Assert.ThrowsAsync<DomainException>(() =>
            new ConfirmarEmailHandler(_identityService).Handle(new ConfirmarEmailCommand(_anaId, "errado"), CancellationToken.None));
    }

    [Fact]
    public async Task ConfirmarEmail_TokenCerto_DeveMarcarComoConfirmado()
    {
        await new ConfirmarEmailHandler(_identityService).Handle(new ConfirmarEmailCommand(_anaId, $"conf-{_anaId}"), CancellationToken.None);

        Assert.True(_identityService.Usuarios[_anaId].EmailConfirmado);
    }

    [Fact]
    public async Task Desativar_Funcionario_DeveBloquearEApagarSessoes()
    {
        var bruno = new UsuarioComRole(Guid.NewGuid(), "Bruno", "bruno@teste.com", "Funcionario");
        _identityService.UsuariosPorRestaurante[_restaurante.Id] = [bruno];
        _context.RefreshTokens.Add(RefreshToken.Criar(bruno.Id, "hash-b", _relogio.AgoraUtc, _relogio.AgoraUtc.AddDays(14)));
        _context.SaveChanges();
        _context.ChangeTracker.Clear();

        await AtivoHandler().Handle(new AlterarAtivoUsuarioCommand(bruno.Id, false), CancellationToken.None);

        Assert.Contains(bruno.Id, _identityService.Desativados);
        Assert.Empty(_context.RefreshTokens);
    }

    [Fact]
    public async Task Desativar_Admin_DeveLancarDomainException()
    {
        var admin = new UsuarioComRole(Guid.NewGuid(), "Carla", "carla@teste.com", "Admin");
        _identityService.UsuariosPorRestaurante[_restaurante.Id] = [admin];

        await Assert.ThrowsAsync<DomainException>(() =>
            AtivoHandler().Handle(new AlterarAtivoUsuarioCommand(admin.Id, false), CancellationToken.None));
    }

    [Fact]
    public async Task Reativar_ComPlanoCheio_DeveLancarDomainException()
    {
        var desativado = new UsuarioComRole(Guid.NewGuid(), "Davi", "davi@teste.com", "Funcionario", Ativo: false);
        _identityService.UsuariosPorRestaurante[_restaurante.Id] =
        [
            new(Guid.NewGuid(), "Eva", "eva@teste.com", "Funcionario"),
            new(Guid.NewGuid(), "Fabio", "fabio@teste.com", "Funcionario"),
            desativado,
        ];

        await Assert.ThrowsAsync<DomainException>(() =>
            AtivoHandler().Handle(new AlterarAtivoUsuarioCommand(desativado.Id, true), CancellationToken.None));
    }

    private EsqueciSenhaHandler EsqueciHandler() =>
        new(_identityService, _outbox, new FakeLinksFront(), _context, _relogio);

    private AlterarAtivoUsuarioHandler AtivoHandler() =>
        new(_identityService, _usuarioAtual, new FakeRestauranteRepository(_context), new FakeConviteRepository(_context),
            new FakeRefreshTokenRepository(_context), _context, _relogio);
}
