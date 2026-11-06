namespace Shared.Auth;

public class KeycloakOptions
{
    public const string Seccion = "Keycloak";

    public string Authority { get; set; } = "";
    public string Audience { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string? ClientSecret { get; set; }   // viene de user-secrets, no del appsettings
}