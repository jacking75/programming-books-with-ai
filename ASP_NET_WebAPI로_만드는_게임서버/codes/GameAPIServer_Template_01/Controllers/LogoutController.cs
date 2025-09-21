using System.Threading.Tasks;
using GameAPIServer.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;


namespace GameAPIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class LogoutController : ControllerBase
{
    readonly ILogger<LogoutController> _logger;

    public LogoutController(ILogger<LogoutController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 로그아웃 API </br>
    /// 해당 유저의 토큰을 Redis에서 삭제합니다.
    /// </summary>
    [HttpPost]
    public async Task<LogoutResponse> DeleteUserToken([FromHeader] Header request)
    {
        await Task.CompletedTask;

        LogoutResponse response = new();
        return response;
    }
}
