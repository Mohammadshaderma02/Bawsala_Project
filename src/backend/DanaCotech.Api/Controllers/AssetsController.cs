using DanaCotech.Application.Assets;
using DanaCotech.Application.Common;
using DanaCotech.Domain.Assets;
using Microsoft.AspNetCore.Mvc;
using DanaCotech.Api.Security;
using DanaCotech.Domain.Platform;

namespace DanaCotech.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AssetsController(IAssetService assetService, SessionTokenService tokenService) : ControllerBase
{
    [HttpGet("{assetKey}")]
    [ProducesResponseType(typeof(ServiceResponse<AssetContent>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<AssetContent>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceResponse<AssetContent>>> GetByKey(
        [FromRoute] string assetKey,
        [FromQuery] string lang = "en",
        CancellationToken cancellationToken = default)
    {
        var response = await assetService.GetByKeyAsync(assetKey, lang, cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpGet("{assetKey}/file")]
    public async Task<IActionResult> GetFile(string assetKey, CancellationToken cancellationToken)
    {
        var response = await assetService.GetByKeyAsync(assetKey, "en", cancellationToken);
        return response.Success && response.Data is not null
            ? File(Convert.FromBase64String(response.Data.Base64Content), response.Data.MimeType, enableRangeProcessing: true)
            : NotFound();
    }

    [HttpPost]
    [RequestSizeLimit(262_144_000)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string? altText, CancellationToken cancellationToken)
    {
        if (!tokenService.TryValidate(Request.Headers.Authorization, out _, out var role) || role != UserRoles.Admin)
            return StatusCode(StatusCodes.Status403Forbidden, new { Success = false, Message = "Admin access is required" });
        if (file.Length == 0 || file.Length > 250 * 1024 * 1024)
            return BadRequest(new { Success = false, Message = "File must be between 1 byte and 250 MB" });

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        var key = $"cms-{Guid.NewGuid():N}";
        var response = await assetService.SaveAsync(key, file.FileName, file.ContentType, Convert.ToBase64String(stream.ToArray()), altText, cancellationToken);
        return response.Success ? Ok(new { response.Success, response.Message, Data = new { response.Data, Url = $"/api/assets/{key}/file" } }) : BadRequest(response);
    }
}
