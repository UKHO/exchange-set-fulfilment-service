using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace UKHO.ADDS.EFS.Orchestrator.Controllers;

/// <summary>
/// Base controller providing common functionality for all API controllers.
/// </summary>
[ApiController]
public abstract class BaseController<T> : ControllerBase where T : class
{
    protected readonly IHttpContextAccessor httpContextAccessor;

    protected BaseController(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>
    /// Gets the token audience from the current user's claims.
    /// </summary>
    public string? TokenAudience => httpContextAccessor.HttpContext?.User.FindFirstValue("aud");

    /// <summary>
    /// Gets the token issuer from the current user's claims.
    /// </summary>
    public string? TokenIssuer => httpContextAccessor.HttpContext?.User.FindFirstValue("iss");

    /// <summary>
    /// Gets the tenant ID from the current user's claims.
    /// </summary>
    public string? TokenTenantId => httpContextAccessor.HttpContext?.User.FindFirstValue("tid");

    /// <summary>
    /// Gets the user's email from the current user's claims.
    /// </summary>
    public string? UserEmail => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);
}
