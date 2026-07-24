using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YekAbr.Api.Extensions;
using YekAbr.Api.Models.Common;
using YekAbr.Services.DTOs.Transfers;
using YekAbr.Services.Interfaces.Auth;
using YekAbr.Services.Interfaces.Transfers;

namespace YekAbr.Api.Controllers;

[ApiController]
[Route("api/transfers")]
[Authorize]
public sealed class TransferController : ControllerBase
{
    private readonly IProviderSyncService _providerSyncService;
    private readonly ICurrentUserService _currentUserService;

    public TransferController(
        IProviderSyncService providerSyncService,
        ICurrentUserService currentUserService)
    {
        _providerSyncService = providerSyncService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Starts a bulk provider-to-provider file copy/sync operation.
    /// </summary>
    [HttpPost("provider-sync")]
    [ProducesResponseType(typeof(ApiResponse<ProviderSyncOperationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProviderSyncOperationDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ProviderSyncOperationDto>>> StartProviderSync(
        [FromBody] StartProviderSyncRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUserService.UserId))
        {
            return Unauthorized();
        }

        var result = await _providerSyncService.StartAsync(
            _currentUserService.UserId,
            request,
            cancellationToken);

        return this.ToApiResponse(result);
    }

    [HttpGet("{operationId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProviderSyncOperationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProviderSyncOperationDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ProviderSyncOperationDto>>> GetOperation(
        Guid operationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUserService.UserId))
        {
            return Unauthorized();
        }

        var result = await _providerSyncService.GetByIdAsync(
            _currentUserService.UserId,
            operationId,
            cancellationToken);

        return this.ToApiResponse(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedProviderSyncOperationsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedProviderSyncOperationsResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PagedProviderSyncOperationsResponse>>> ListOperations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_currentUserService.UserId))
        {
            return Unauthorized();
        }

        var result = await _providerSyncService.ListAsync(
            _currentUserService.UserId,
            page,
            pageSize,
            cancellationToken);

        return this.ToApiResponse(result);
    }
}
