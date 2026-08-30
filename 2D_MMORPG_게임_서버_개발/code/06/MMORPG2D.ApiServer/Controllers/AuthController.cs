using Microsoft.AspNetCore.Mvc;
using MMORPG2D.ApiServer.Models;
using MMORPG2D.ApiServer.Services;

namespace MMORPG2D.ApiServer.Controllers;

[ApiController]
[Route("api")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    public AuthController(AuthService a) { _auth = a; }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterReq req)
    {
        var r = await _auth.RegisterAsync(req.Id, req.Pw);
        return r switch
        {
            RegisterResult.Ok => Ok(new { ok = true }),
            RegisterResult.Conflict => Conflict(new { ok = false, reason = "id_taken" }),
            _ => BadRequest(new { ok = false, reason = "bad_input" })
        };
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginReq req)
    {
        var r = await _auth.LoginAsync(req.Id, req.Pw);
        if (!r.Success) return Unauthorized(new { ok = false });
        return Ok(new LoginResp(r.Token!, r.AccountId));
    }
}
