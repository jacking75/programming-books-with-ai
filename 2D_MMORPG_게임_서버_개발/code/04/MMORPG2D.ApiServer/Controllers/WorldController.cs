using Microsoft.AspNetCore.Mvc;
using MMORPG2D.ApiServer.Services;

namespace MMORPG2D.ApiServer.Controllers;

[ApiController]
[Route("api")]
public class WorldController : ControllerBase
{
    private readonly WorldService _world;
    public WorldController(WorldService w) { _world = w; }

    [HttpGet("worlds")]
    public async Task<IActionResult> List() => Ok(await _world.ListAsync());
}
