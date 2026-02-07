using System.Security.Claims;
using ChatAuth.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

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


    // 1:1 입장
    public Task JoinDm(string other)
    {
        var me = Me();
        var room = Room(me, other);

        // 내 현재 연결을 그 방 그룹에 추가
        return Groups.AddToGroupAsync(Context.ConnectionId, room);
    }

	public async Task SendDm(string ReceiverEmail, string message)
	{
		var myEmail = Me(); // 현재 로그인한 내 이메일

		var sender = await _db.Users.FirstOrDefaultAsync(u => u.Email == myEmail);
		var receiver = await _db.Users.FirstOrDefaultAsync(u => u.Email == ReceiverEmail);

		if (sender == null || receiver == null) return;

		var chatMsg = new Message
		{
			SenderId = sender.Id,
			ReceiverId = receiver.Id,
			MessageText = message,
			Timestamp = DateTime.UtcNow
		};

		_db.ChattingMsg.Add(chatMsg);
		await _db.SaveChangesAsync();

		// 상대방과 나에게 메시지 전송
        var room = Room(myEmail, ReceiverEmail);
		await Clients.Group(room).SendAsync("message", myEmail, message);
	}
}