using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChatAuth.Data;

public class Message
{
	public int Id { get; set; }
	public string SenderId { get; set; } = "";
	public string ReceiverId { get; set; } = "";
    public string MessageText { get; set; } = "";
	public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

	public DbSet<Message> ChattingMsg { get; set; }
	public DbSet<UserConnection> UserConnections => Set<UserConnection>();
}