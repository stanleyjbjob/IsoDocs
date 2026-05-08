using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Controllers;

/// <summary>
/// 健康檢查端點。提供給負載平衡器、Kubernetes liveness/readiness 探針使用。
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    /// <summary>
    /// 簡易存活檢查，固定回傳 200 與當下時間。
    /// </summary>
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "ok",
        service = "IsoDocs.Api",
        timestamp = DateTimeOffset.UtcNow,
    });
}
