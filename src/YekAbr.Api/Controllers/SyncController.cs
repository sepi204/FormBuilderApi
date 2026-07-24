using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YekAbr.Api.Extensions;
using YekAbr.Api.Models.Common;
using YekAbr.Domain.Enums;
using YekAbr.Services.DTOs.Sync;
using YekAbr.Services.Interfaces.Auth;
using YekAbr.Services.Interfaces.Sync;

namespace YekAbr.Api.Controllers;

[ApiController]
[Route("api/sync")]
[Authorize]
public sealed class SyncController : ControllerBase
{
    private readonly ICloudMetadataSyncService _metadataSyncService;
    private readonly ICurrentUserService _currentUserService;

    public SyncController(
        ICloudMetadataSyncService metadataSyncService,
        ICurrentUserService currentUserService)
    {
        _metadataSyncService = metadataSyncService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Sync file metadata from all connected cloud providers for the current user.
    /// </summary>
    [HttpPost("providers/files/metadata")]
    [ProducesResponseType(typeof(ApiResponse<ProviderMetadataSyncResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProviderMetadataSyncResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ProviderMetadataSyncResultDto>>> SyncAllProvidersMetadata(
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUserService.UserId))
        {
            return Unauthorized();
        }

        var result = await _metadataSyncService.SyncAllConnectedProvidersAsync(
            _currentUserService.UserId,
            cancellationToken);

        return this.ToApiResponse(result);
    }

    /// <summary>
    /// Sync file metadata from a single provider type for the current user.
    /// </summary>
    [HttpPost("providers/{providerType}/files/metadata")]
    [ProducesResponseType(typeof(ApiResponse<ProviderMetadataSyncResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProviderMetadataSyncResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ProviderMetadataSyncResultDto>>> SyncProviderMetadata(
        CloudProviderType providerType,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUserService.UserId))
        {
            return Unauthorized();
        }

        var result = await _metadataSyncService.SyncProviderAsync(
            _currentUserService.UserId,
            providerType,
            cancellationToken);

        return this.ToApiResponse(result);
    }
}
