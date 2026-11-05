namespace GestaoRestaurante.Identidade.Application.Abstractions.Services;

public interface ILinksFront
{
    string AceitarConvite(string token);

    string RedefinirSenha(string email, string token);

    string ConfirmarEmail(Guid usuarioId, string token);

    string Assinatura();
}
