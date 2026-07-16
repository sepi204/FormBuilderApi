using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YekAbr.Api.Extensions;
using YekAbr.Api.Models.Common;
using YekAbr.Domain.Enums;
using YekAbr.Services.DTOs.Cloud;
using YekAbr.Services.Interfaces.Auth;
using YekAbr.Services.Interfaces.Cloud;

namespace YekAbr.Api.Controllers;

[ApiController]
[Route("api/cloud/transfers")]
[Authorize]
public sealed class CloudTransfersController : ControllerBase
{
    private readonly ICloudTransferService _transferService;
    private readonly ICurrentUserService _currentUserService;

    public CloudTransfersController(
        ICloudTransferService transferService,
        ICurrentUserService currentUserService)
    {
        _transferService = transferService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CloudTransferJobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CloudTransferJobDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<CloudTransferJobDto>>> Create(
        [FromBody] CreateCloudTransferJobRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUserService.UserId))
        {
            return Unauthorized();
        }

        var result = await _transferService.CreateAsync(_currentUserService.UserId, request, cancellationToken);
        return this.ToApiResponse(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CloudTransferJobDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CloudTransferJobDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CloudTransferJobDto>>>> List(
        [FromQuery] CloudTransferStatus? status,
        [FromQuery] Guid? sourceConnectedAccountId,
        [FromQuery] Guid? destinationConnectedAccountId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_currentUserService.UserId))
        {
            return Unauthorized();
        }

        var result = await _transferService.ListAsync(
            _currentUserService.UserId,
            status,
            sourceConnectedAccountId,
            destinationConnectedAccountId,
            page,
            pageSize,
            cancellationToken);

        return this.ToApiResponse(result);
    }

    [HttpGet("{jobId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CloudTransferJobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CloudTransferJobDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<CloudTransferJobDto>>> GetById(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUserService.UserId))
        {
            return Unauthorized();
        }

        var result = await _transferService.GetByIdAsync(_currentUserService.UserId, jobId, cancellationToken);
        return this.ToApiResponse(result);
    }

    [HttpPost("{jobId:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> Cancel(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUserService.UserId))
        {
            return Unauthorized();
        }

        var result = await _transferService.CancelAsync(_currentUserService.UserId, jobId, cancellationToken);
        return this.ToApiResponse(result);
    }
}
