namespace NetIntegrationTests.Setup;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

public class BasicAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    AppSettings appSettings)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));
        }

        string username, password;
        try
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            var authHeaderVal = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Substring("Basic ".Length).Trim()));
            var authHeaderParts = authHeaderVal.Split(':');
            username = authHeaderParts[0];
            password = authHeaderParts[1];
        }
        catch
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
        }

        if (username != appSettings.BasicApiUser || password != appSettings.BasicApiPassword)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Username or Password"));
        }

        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, username),
            new Claim(ClaimTypes.Name, username)
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
