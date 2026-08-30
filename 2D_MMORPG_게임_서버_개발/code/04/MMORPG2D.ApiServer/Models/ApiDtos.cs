namespace MMORPG2D.ApiServer.Models;

public record RegisterReq(string Id, string Pw);
public record LoginReq(string Id, string Pw);
public record CharacterCreateReq(int WorldId, string Name);

public record LoginResp(string Token, long AccountId);
public record CharacterCreateResp(bool Ok, long Id, string? Reason);
