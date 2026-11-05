using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using GestaoRestaurante.Core.Seguranca;
using GestaoRestaurante.Identidade.Application.Abstractions.Services;

namespace GestaoRestaurante.Identidade.Infrastructure.Security;

public class GeradorRefreshToken : IGeradorRefreshToken
{
    private readonly JwtSettings _settings;

    public GeradorRefreshToken(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public int DiasValidade => _settings.RefreshTokenDias;

    public RefreshTokenGerado Gerar()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        return new RefreshTokenGerado(token, CalcularHash(token));
    }

    public string CalcularHash(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
