namespace ChatAuth.Data
{
    public class UserConnection
    {
        public int Id { get; set; }

        // AspNetUsers의 Id (GUID 문자열)
        public string UserId { get; set; } = "";

        // SignalR 연결 ID
        public string ConnectionId { get; set; } = "";

        public DateTime ConnectedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
