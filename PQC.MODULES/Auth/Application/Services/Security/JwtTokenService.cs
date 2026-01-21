using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PQC.MODULES.Auth.Domain.Settings;
using PQC.MODULES.Users.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class JwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Nome),
            new Claim("cpf", user.Cpf),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_settings.SecretKey!)
        );

        var credentials = new SigningCredentials(
            key, SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_settings.ExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
