using Microsoft.AspNetCore.Mvc;
using CallTrackingSystem.Core.Services;
using CallTrackingSystem.Core.DTOs;

namespace CallTrackingSystem.Web.Controllers;

/// <summary>
/// 詢問系統管理 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class InquirySystemsController : ControllerBase
{
    private readonly CallRecordService _callRecordService;
    private readonly ILogger<InquirySystemsController> _logger;

    public InquirySystemsController(
        CallRecordService callRecordService,
        ILogger<InquirySystemsController> logger)
    {
        _callRecordService = callRecordService;
        _logger = logger;
    }

    /// <summary>
    /// 取得所有啟用的詢問系統清單（用於下拉選單）
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>詢問系統清單</returns>
    /// <response code="200">成功取得詢問系統清單</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<InquirySystemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveInquirySystems(
        CancellationToken cancellationToken = default)
    {
        var result = await _callRecordService.GetActiveInquirySystemsAsync(cancellationToken);
        return Ok(result);
    }
}
