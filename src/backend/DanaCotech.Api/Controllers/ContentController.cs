using DanaCotech.Application.Common;
using DanaCotech.Application.Content;
using DanaCotech.Domain.Content;
using Microsoft.AspNetCore.Mvc;
using DanaCotech.Api.Security;
using DanaCotech.Domain.Platform;

namespace DanaCotech.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ContentController(ILandingContentService landingContentService, SessionTokenService tokenService) : ControllerBase
{
    [HttpGet("landing")]
    [ProducesResponseType(typeof(ServiceResponse<LandingContent>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<LandingContent>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceResponse<LandingContent>>> GetLandingContent(
        [FromQuery] string lang = "en",
        CancellationToken cancellationToken = default)
    {
        var response = await landingContentService.GetLandingContentAsync(lang, cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPut("landing/{lang}")]
    [ProducesResponseType(typeof(ServiceResponse<LandingContent>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<LandingContent>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ServiceResponse<LandingContent>>> SaveLandingContent(
        string lang,
        [FromBody] LandingContent content,
        CancellationToken cancellationToken = default)
    {
        if (!tokenService.TryValidate(Request.Headers.Authorization, out _, out var role))
            return Unauthorized(ServiceResponse<LandingContent>.Fail("A valid session is required"));
        if (!string.Equals(role, UserRoles.Admin, StringComparison.OrdinalIgnoreCase))
            return StatusCode(StatusCodes.Status403Forbidden, ServiceResponse<LandingContent>.Fail("Admin access is required"));

        var response = await landingContentService.SaveLandingContentAsync(lang, content, cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }
}
