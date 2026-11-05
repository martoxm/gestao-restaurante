using GestaoRestaurante.Core.Application.Exceptions;
using GestaoRestaurante.Core.Domain;
using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Identidade.Application.UseCases.Auth;
using GestaoRestaurante.Identidade.Application.UseCases.Convites;
using GestaoRestaurante.Identidade.Domain.Entities;
using GestaoRestaurante.Identidade.Tests.Common;

namespace GestaoRestaurante.Identidade.Tests.Handlers;

public class ConvitesHandlersTests
{
    private readonly FakeUsuarioAtualService _usuarioAtual = new();
    private readonly FakeRelogio _relogio = new();
    private readonly FakeIdentityService _identityService = new();
    private readonly FakeOutboxMensagemRepository _outbox = new();
    private readonly FakeGeradorRefreshToken _gerador = new();
    private readonly FakeJwtTokenGenerator _jwt = new();
    private readonly TestDbContext _context;
    private readonly Restaurante _restaurante;

    public ConvitesHandlersTests()
    {
        _context = TestDbContextFactory.Create(_usuarioAtual);
        _restaurante = Restaurante.Criar("Cantina da Ana", _relogio.AgoraUtc);
        _context.Restaurantes.Add(_restaurante);
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
        _usuarioAtual.RestauranteId = _restaurante.Id;
    }

    [Fact]
    public async Task Criar_DeveGravarConviteEMandarEmailPeloOutbox()
    {
        var resposta = await CriarHandler().Handle(new CriarConviteCommand("bruno@teste.com", "Funcionario"), CancellationToken.None);

        var convite = Assert.Single(_context.Convites);
        Assert.Equal(resposta.Id, convite.Id);
        Assert.Equal(_restaurante.Id, convite.RestauranteId);
        Assert.Equal("hash-rt-1", convite.TokenHash);

        var mensagem = Assert.Single(_outbox.Mensagens);
        Assert.Equal(Filas.Email, mensagem.Fila);
        Assert.Contains("aceitar-convite?token=rt-1", mensagem.Conteudo);
    }

    [Fact]
    public async Task Criar_MesmoEmailDeNovo_DeveCancelarOConviteAnterior()
    {
        var handler = CriarHandler();
        var primeiro = await handler.Handle(new CriarConviteCommand("bruno@teste.com", "Funcionario"), CancellationToken.None);
        _context.ChangeTracker.Clear();

        await handler.Handle(new CriarConviteCommand("bruno@teste.com", "Admin"), CancellationToken.None);

        var antigo = _context.Convites.Single(convite => convite.Id == primeiro.Id);
        Assert.NotNull(antigo.CanceladoEm);
        Assert.Equal(2, _context.Convites.Count());
    }

    [Fact]
    public async Task Aceitar_DeveCriarUsuarioComRestauranteERoleDoConvite()
    {
        await CriarHandler().Handle(new CriarConviteCommand("bruno@teste.com", "Admin"), CancellationToken.None);
        _context.ChangeTracker.Clear();
        _usuarioAtual.RestauranteId = null;

        var resposta = await AceitarHandler().Handle(new AceitarConviteCommand("rt-1", "Bruno", "senha123"), CancellationToken.None);

        Assert.Equal("bruno@teste.com", resposta.Email);
        var usuario = Assert.Single(_identityService.Usuarios.Values);
        Assert.Equal(_restaurante.Id, usuario.RestauranteId);
        Assert.Equal("Admin", _identityService.Roles[usuario.Id]);
        Assert.True(_context.UltimaTransacao!.Confirmada);
    }

    [Fact]
    public async Task Aceitar_MesmoLinkDuasVezes_DeveRecusarASegunda()
    {
        await CriarHandler().Handle(new CriarConviteCommand("bruno@teste.com", "Funcionario"), CancellationToken.None);
        _context.ChangeTracker.Clear();
        await AceitarHandler().Handle(new AceitarConviteCommand("rt-1", "Bruno", "senha123"), CancellationToken.None);
        _context.ChangeTracker.Clear();

        await Assert.ThrowsAsync<DomainException>(() =>
            AceitarHandler().Handle(new AceitarConviteCommand("rt-1", "Outro", "senha123"), CancellationToken.None));
    }

    [Fact]
    public async Task Aceitar_ConviteVencido_DeveRecusar()
    {
        await CriarHandler().Handle(new CriarConviteCommand("bruno@teste.com", "Funcionario"), CancellationToken.None);
        _context.ChangeTracker.Clear();
        _relogio.AgoraUtc = _relogio.AgoraUtc.AddDays(8);

        await Assert.ThrowsAsync<DomainException>(() =>
            AceitarHandler().Handle(new AceitarConviteCommand("rt-1", "Bruno", "senha123"), CancellationToken.None));
        Assert.Empty(_identityService.Usuarios);
    }

    [Fact]
    public async Task ObterPorToken_TokenQueNaoExiste_DeveDar404()
    {
        var handler = new ObterConvitePorTokenHandler(new FakeConviteRepository(_context), _gerador, _relogio);

        await Assert.ThrowsAsync<NaoEncontradoException>(() =>
            handler.Handle(new ObterConvitePorTokenQuery("token-inventado"), CancellationToken.None));
    }

    [Fact]
    public async Task Cancelar_ConviteDeOutroRestaurante_DeveDar404()
    {
        var resposta = await CriarHandler().Handle(new CriarConviteCommand("bruno@teste.com", "Funcionario"), CancellationToken.None);
        _context.ChangeTracker.Clear();
        _usuarioAtual.RestauranteId = Guid.NewGuid();

        var handler = new CancelarConviteHandler(new FakeConviteRepository(_context), _context, _relogio);

        await Assert.ThrowsAsync<NaoEncontradoException>(() =>
            handler.Handle(new CancelarConviteCommand(resposta.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Criar_PlanoTesteComTresVagasOcupadas_DeveRecusar()
    {
        foreach (var email in new[] { "a@teste.com", "b@teste.com", "c@teste.com" })
            await CriarHandler().Handle(new CriarConviteCommand(email, "Funcionario"), CancellationToken.None);
        _context.ChangeTracker.Clear();

        await Assert.ThrowsAsync<DomainException>(() =>
            CriarHandler().Handle(new CriarConviteCommand("d@teste.com", "Funcionario"), CancellationToken.None));
    }

    [Fact]
    public async Task Criar_ReenviarMesmoEmailComPlanoCheio_DevePermitir()
    {
        foreach (var email in new[] { "a@teste.com", "b@teste.com", "c@teste.com" })
            await CriarHandler().Handle(new CriarConviteCommand(email, "Funcionario"), CancellationToken.None);
        _context.ChangeTracker.Clear();

        await CriarHandler().Handle(new CriarConviteCommand("c@teste.com", "Admin"), CancellationToken.None);
    }

    [Fact]
    public async Task Aceitar_DeveEmitirTokenComRestauranteLiberado()
    {
        await CriarHandler().Handle(new CriarConviteCommand("bruno@teste.com", "Funcionario"), CancellationToken.None);
        _context.ChangeTracker.Clear();

        await AceitarHandler().Handle(new AceitarConviteCommand("rt-1", "Bruno", "senha123"), CancellationToken.None);

        Assert.True(_jwt.UltimoRestauranteLiberado);
    }

    [Fact]
    public async Task Aceitar_ComTesteVencido_DeveRecusar()
    {
        var vencido = Restaurante.Criar("Vencido", _relogio.AgoraUtc.AddDays(-20));
        _context.Restaurantes.Add(vencido);
        _context.Convites.Add(Convite.Criar(vencido.Id, vencido.Nome, "ze@teste.com", "Funcionario", "hash-tok-vencido", _relogio.AgoraUtc));
        _context.SaveChanges();
        _context.ChangeTracker.Clear();

        await Assert.ThrowsAsync<DomainException>(() =>
            AceitarHandler().Handle(new AceitarConviteCommand("tok-vencido", "Zé", "senha123"), CancellationToken.None));
        Assert.Empty(_identityService.Usuarios);
    }

    private CriarConviteHandler CriarHandler() =>
        new(_identityService, new FakeConviteRepository(_context), new FakeRestauranteRepository(_context), _outbox,
            _gerador, new FakeLinksFront(), _usuarioAtual, _context, _relogio);

    private AceitarConviteHandler AceitarHandler() =>
        new(new FakeRestauranteRepository(_context), new FakeConviteRepository(_context), _gerador, _identityService,
            new EmissorSessao(new FakeRestauranteRepository(_context), _identityService, _jwt, _gerador, new FakeRefreshTokenRepository(_context), _relogio),
            _context, _relogio);
}
