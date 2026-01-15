using Microsoft.AspNetCore.SignalR;

public class ChatHub : Hub
{
	// =========================
	// (기존 기능) 전체에게 뿌리기
	// - 지금 index.html은 이걸 쓰고 있음
	// =========================
	public async Task SendMessage(string user, string message)
	{
		await Clients.All.SendAsync("message", user, message);
	}

	// =========================
	// (추가 기능) 1:1 채팅을 위한 "방 이름" 만들기
	// - A와 B가 들어오면 항상 같은 방 이름이 나오게 해야 함
	// - 그래서 문자열을 정렬해서 "작은 것__큰 것" 형태로 만듦
	// =========================
	private static string Room(string userA, string userB)
	{
		// 예: ("B","A")든 ("A","B")든 결과는 "A__B"
		return string.CompareOrdinal(userA, userB) < 0
			? $"{userA}__{userB}"
			: $"{userB}__{userA}";
	}

	// =========================
	// (추가 기능) 1:1 방에 들어가기
	// - 브라우저가 "나(me), 상대(otherUser)"를 보내면
	//   그 둘이 쓸 방(그룹)에 현재 연결을 넣어줌
	// =========================
	public Task JoinRoom(string me, string otherUser)
	{
		var room = Room(me, otherUser);

		// 지금 내 연결(Context.ConnectionId)을 room 그룹에 추가
		return Groups.AddToGroupAsync(Context.ConnectionId, room);
	}

	// =========================
	// (추가 기능) 1:1로만 메시지 보내기
	// - "나(me) + 상대(otherUser)" 방에만 메시지를 뿌림
	// =========================
	public Task SendToUser(string me, string otherUser, string message)
	{
		var room = Room(me, otherUser);

		// ✅ 여기서 All이 아니라 Group(room)만!
		return Clients.Group(room).SendAsync("message", me, message);
	}
}
