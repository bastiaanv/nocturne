using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenApi.Remote.Attributes;
using Nocturne.API.Models.Responses;
using Nocturne.API.Services.Auth;
using Nocturne.Core.Contracts.Multitenancy;
using Nocturne.Core.Models.Authorization;
using Nocturne.Core.Models.Configuration;

namespace Nocturne.API.Controllers.V4.Identity;

/// <summary>
/// Manages the tenant's single rotatable public share link ({token}.share.{baseDomain}).
/// All operations require the <c>sharing.manage</c> permission.
/// </summary>
[ApiController]
[Tags("Identity")]
[Route("api/v4/share")]
[Produces("application/json")]
[Authorize]
public class ShareLinkController : ControllerBase
{
    private readonly IShareLinkService _shareLinkService;
    private readonly ITenantAccessor _tenantAccessor;

    public ShareLinkController(IShareLinkService shareLinkService, ITenantAccessor tenantAccessor)
    {
        _shareLinkService = shareLinkService;
        _tenantAccessor = tenantAccessor;
    }

    /// <summary>Get the current public share link state.</summary>
    [HttpGet]
    [RemoteQuery]
    [ProducesResponseType(typeof(ShareLinkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ShareLinkDto>> GetShareLink(CancellationToken ct)
    {
        if (!HasPermission(TenantPermissions.SharingManage))
            return Forbid();

        return Ok(await _shareLinkService.GetAsync(_tenantAccessor.TenantId, ct));
    }

    /// <summary>
    /// Mint a new share token — enables sharing, or rotates an existing link. The previous link
    /// stops working immediately.
    /// </summary>
    [HttpPost("rotate")]
    [RemoteCommand(Invalidates = ["GetShareLink"])]
    [ProducesResponseType(typeof(ShareLinkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ShareLinkDto>> RotateShareLink(CancellationToken ct)
    {
        if (!HasPermission(TenantPermissions.SharingManage))
            return Forbid();

        return Ok(await _shareLinkService.RotateAsync(_tenantAccessor.TenantId, ct));
    }

    /// <summary>Disable public sharing and invalidate the current link.</summary>
    [HttpDelete]
    [RemoteCommand(Invalidates = ["GetShareLink"])]
    [ProducesResponseType(typeof(ShareLinkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ShareLinkDto>> DisableShareLink(CancellationToken ct)
    {
        if (!HasPermission(TenantPermissions.SharingManage))
            return Forbid();

        return Ok(await _shareLinkService.DisableAsync(_tenantAccessor.TenantId, ct));
    }

    /// <summary>Choose whether the public view shows full history or only the last 24 hours.</summary>
    [HttpPut("full-history")]
    [RemoteCommand(Invalidates = ["GetShareLink"])]
    [ProducesResponseType(typeof(ShareLinkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ShareLinkDto>> SetShareLinkFullHistory(
        [FromBody] SetShareFullHistoryRequest request, CancellationToken ct)
    {
        if (!HasPermission(TenantPermissions.SharingManage))
            return Forbid();

        return Ok(await _shareLinkService.SetFullHistoryAsync(_tenantAccessor.TenantId, request.FullHistory, ct));
    }

    /// <summary>
    /// Set which data categories anonymous viewers can see. Scopes must be read-permission atoms
    /// drawn from <c>TenantPermissions.PublicShareScopes</c>; an empty list keeps the link live but
    /// shares nothing.
    /// </summary>
    [HttpPut("scopes")]
    [RemoteCommand(Invalidates = ["GetShareLink"])]
    [ProducesResponseType(typeof(ShareLinkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ShareLinkDto>> SetShareLinkScopes(
        [FromBody] SetShareScopesRequest request, CancellationToken ct)
    {
        if (!HasPermission(TenantPermissions.SharingManage))
            return Forbid();

        try
        {
            return Ok(await _shareLinkService.SetScopesAsync(_tenantAccessor.TenantId, request.Scopes, ct));
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: 400, title: "Invalid scopes");
        }
    }

    /// <summary>
    /// Customize the appearance of the public share view (glucose units, time format, color
    /// scheme/theme). Only the provided fields change — null fields keep their current value.
    /// </summary>
    [HttpPut("appearance")]
    [RemoteCommand(Invalidates = ["GetShareLink"])]
    [ProducesResponseType(typeof(ShareLinkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ShareLinkDto>> SetShareLinkAppearance(
        [FromBody] SetShareAppearanceRequest request, CancellationToken ct)
    {
        if (!HasPermission(TenantPermissions.SharingManage))
            return Forbid();

        try
        {
            return Ok(await _shareLinkService.SetAppearanceAsync(
                _tenantAccessor.TenantId, request.Appearance, ct));
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: 400, title: "Invalid appearance");
        }
    }

    private bool HasPermission(string permission)
    {
        var grantedScopes = HttpContext.Items["GrantedScopes"] as IReadOnlySet<string>;
        return grantedScopes != null && TenantPermissions.HasPermission(grantedScopes, permission);
    }
}

/// <summary>
/// The appearance to pin for the public share view. Fields carry only the aspects being changed;
/// null leaves each aspect at its current value (or the platform default if never set).
/// </summary>
public record SetShareAppearanceRequest(ShareAppearance Appearance);

public record SetShareFullHistoryRequest(bool FullHistory);

public record SetShareScopesRequest(List<string> Scopes);
