using GestaoRestaurante.Core.Mensageria;
using GestaoRestaurante.Identidade.Application.Abstractions.Services;

namespace GestaoRestaurante.Identidade.Tests.Common;

public class FakeOutboxMensagemRepository : IOutboxMensagemRepository
{
    public List<OutboxMensagem> Mensagens { get; } = [];

    public void Adicionar(OutboxMensagem mensagem) => Mensagens.Add(mensagem);

    public void Atualizar(OutboxMensagem mensagem)
    {
    }

    public Task<IReadOnlyList<OutboxMensagem>> ListarPendentesAsync(int quantidade, DateTime agora, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OutboxMensagem>>(Mensagens);
}

public class FakeLinksFront : ILinksFront
{
    public string AceitarConvite(string token) => $"http://front/aceitar-convite?token={token}";

    public string RedefinirSenha(string email, string token) => $"http://front/redefinir-senha?email={email}&token={token}";

    public string ConfirmarEmail(Guid usuarioId, string token) => $"http://front/confirmar-email?usuarioId={usuarioId}&token={token}";

    public string Assinatura() => "http://front/assinatura";
}
