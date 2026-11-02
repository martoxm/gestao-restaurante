namespace RestauranteEstoque.Identidade.Tests.Common;

public sealed class FakeTransacao
{
    public bool Confirmada { get; private set; }
    public bool Descartada { get; private set; }

    public void Confirmar() => Confirmada = true;

    public void Descartar() => Descartada = true;
}
