using Microsoft.AspNetCore.Mvc;
using MMORPG2D.ApiServer.Models;
using MMORPG2D.ApiServer.Services;

namespace MMORPG2D.ApiServer.Controllers;

[ApiController]
[Route("api/character")]
public class CharacterController : ControllerBase
{
    private readonly CharacterService _chars;
    public CharacterController(CharacterService c) { _chars = c; }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CharacterCreateReq req)
    {
        var accountId = (long?)HttpContext.Items["AccountId"];
        if (accountId is null) return Unauthorized();

        var r = await _chars.CreateAsync(accountId.Value, req.WorldId, req.Name);
        if (r.Success) return Ok(new CharacterCreateResp(true, r.CharacterId, null));
        return BadRequest(new CharacterCreateResp(false, 0, r.Reason));
    }

    [HttpGet("list")]
    public async Task<IActionResult> List()
    {
        var accountId = (long?)HttpContext.Items["AccountId"];
        if (accountId is null) return Unauthorized();
        return Ok(await _chars.ListAsync(accountId.Value));
    }
}
