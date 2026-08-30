using CloudStructures.Structures;
using MMORPG2D.ApiServer.Infrastructure;

namespace MMORPG2D.ApiServer.Middleware;

/// <summary>
/// X-Token 헤더가 있으면 Redis 에서 accountId 를 꺼내 HttpContext.Items 에 박는다.
/// 토큰이 없거나 만료되면 아무것도 하지 않는다 — 컨트롤러에서 Unauthorized 를 결정.
/// </summary>
public static class TokenAuthMiddleware
{
    public static IApplicationBuilder UseTokenAuth(this IApplicationBuilder app)
    {
        return app.Use(async (ctx, next) =>
        {
            if (ctx.Request.Headers.TryGetValue("X-Token", out var t) &&
                !string.IsNullOrWhiteSpace(t))
            {
                var redis = ctx.RequestServices.GetRequiredService<RedisProvider>();
                var sess = new RedisString<long>(redis.Conn, $"sess:{t}", Services.AuthService.SessionTtl);
                var v = await sess.GetAsync();
                if (v.HasValue) ctx.Items["AccountId"] = v.Value;
            }
            await next();
        });
    }
}
