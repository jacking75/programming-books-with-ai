// 1주차에는 API 서버 본체가 없다. 솔루션 형태만 미리 잡아 두고 2주차에서 채운다.
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/", () => "MMORPG2D.ApiServer (week01 placeholder)");
app.Run();
