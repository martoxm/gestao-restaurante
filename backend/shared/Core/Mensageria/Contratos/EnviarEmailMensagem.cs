namespace GestaoRestaurante.Core.Mensageria.Contratos;

public sealed record EnviarEmailMensagem(IReadOnlyList<string> Destinatarios, string Assunto, string CorpoHtml);
