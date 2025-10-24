using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyWebApp.Api.Common;


namespace MyWebApp.Api.Services;

public class JWTService(IOptionsSnapshot<JWTSettings> jwtSettings)
{
    private readonly JWTSettings _jwtSettings = jwtSettings.Value;

    public string GenerateApplicationToken(string userId, string role)
    {
        var tokenExpiry = DateTime.UtcNow.AddHours(Convert.ToDouble(_jwtSettings.TokenLifeTime.TotalHours)); // Token valid for configured hours
        var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            };
        return GenerateToken(claims, tokenExpiry);
    }

    public string GenerateToken(Claim[] claims, DateTime tokenExpiry)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);
        var tokenDescriptor = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: tokenExpiry,
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }

    public JwtSecurityToken ValidateToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = new
                SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key))
            };
            SecurityToken validatedToken;
            handler.ValidateToken(token, validationParameters, out validatedToken);
            return jwt;
        }
        catch (Exception ex)
        {
            // throw new UnauthorizedException(ex.Message);
            throw new SecurityTokenException("Invalid token", ex);
        }
    }

}
