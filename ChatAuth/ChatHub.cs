using System.Security.Claims;
using ChatAuth.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize] // 로그인한 사람만
public class ChatHub : Hub
{
    private readonly ApplicationDbContext _db;

    public ChatHub(ApplicationDbContext db)
    {
        _db = db;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            _db.UserConnections.Add(new UserConnection
            {
                UserId = userId,
                ConnectionId = Context.ConnectionId
            });
            await _db.SaveChangesAsync();
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connId = Context.ConnectionId;

        var rows = _db.UserConnections.Where(x => x.ConnectionId == connId);
        _db.UserConnections.RemoveRange(rows);
        await _db.SaveChangesAsync();

        await base.OnDisconnectedAsync(exception);
    }

    // 서버가 내 이름을 로그인 정보에서 가져옴 
    private string Me() => Context.User?.Identity?.Name ?? "UNKNOWN";

    private static string Room(string a, string b)
    {
        return string.CompareOrdinal(a, b) < 0 ? $"{a}__{b}" : $"{b}__{a}";
    }


    // 2) 1:1 입장
    // 클라이언트는 "상대(other)"만 보냄
    // 나는 서버가 로그인에서 자동으로 가져옴
    public Task JoinDm(string other)
    {
        var me = Me();
        var room = Room(me, other);

        // 내 현재 연결을 그 방 그룹에 추가
        return Groups.AddToGroupAsync(Context.ConnectionId, room);
    }


    // 3) 1:1로 보내기
    public Task SendDm(string other, string message)
    {
        var me = Me();
        var room = Room(me, other);

        return Clients.Group(room).SendAsync("message", me, message);
    }
}
