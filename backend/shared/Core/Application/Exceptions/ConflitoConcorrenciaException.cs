namespace GestaoRestaurante.Core.Application.Exceptions;

public class ConflitoConcorrenciaException : Exception
{
    public const string MensagemPadrao =
        "Esse registro foi alterado por outra pessoa enquanto você trabalhava. Recarregue e tente de novo.";

    public ConflitoConcorrenciaException(Exception innerException)
        : base(MensagemPadrao, innerException)
    {
    }
}
