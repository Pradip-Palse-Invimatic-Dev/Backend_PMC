namespace MyWebApp.Api.Common;

public class JWTSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public TimeSpan TokenLifeTime { get; set; } = TimeSpan.FromHours(1);
}