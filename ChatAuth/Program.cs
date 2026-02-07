using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ChatAuth.Data;
using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

// 데이터베이스 설정
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// 인증 설정
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequiredLength = 4;
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddRazorPages();

var app = builder.Build();

// HTTP 요청
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapGet("/", (HttpContext ctx) =>
{
    if (ctx.User?.Identity?.IsAuthenticated == true)
        return Results.Redirect("/users.html");
    return Results.Redirect("/Identity/Account/Login");
});

app.MapRazorPages().WithStaticAssets();
app.MapHub<ChatHub>("/hubs/chat");

// GET /api/users
app.MapGet("/api/users", async (ApplicationDbContext db, HttpContext http) =>
{
    var meId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);

    var users = await (
        from u in db.Users
        join c in db.UserClaims.Where(x => x.ClaimType == "display_name") on u.Id equals c.UserId into cg
        from c in cg.DefaultIfEmpty()
        join pi in db.UserClaims.Where(x => x.ClaimType == "profile_img") on u.Id equals pi.UserId into pig
        from pi in pig.DefaultIfEmpty()
        join uc in db.UserConnections on u.Id equals uc.UserId into ucg
        where u.Id != meId
        orderby u.UserName
        select new
        {
            email = u.Email,
            name = (c != null && !string.IsNullOrWhiteSpace(c.ClaimValue)) ? c.ClaimValue : u.Email,
            isOnline = ucg.Any(),
            imageUrl = (pi != null && !string.IsNullOrWhiteSpace(pi.ClaimValue)) ? pi.ClaimValue : "/pimg.png"
        }
    ).ToListAsync();

    return Results.Ok(users);
})
    .RequireAuthorization();

// GET /logout
app.MapGet("/logout", async (SignInManager<IdentityUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/Identity/Account/Login");
})
    .RequireAuthorization();

// GET /api/me
app.MapGet("/api/me", async (HttpContext http, UserManager<IdentityUser> userManager) =>
{
    var user = await userManager.GetUserAsync(http.User);
    if (user == null) return Results.Unauthorized();

    var displayName = http.User.FindFirst("display_name")?.Value;
    if (string.IsNullOrWhiteSpace(displayName))
    {
        var claims = await userManager.GetClaimsAsync(user);
        displayName = claims.FirstOrDefault(c => c.Type == "display_name")?.Value;
    }
    displayName ??= user.UserName ?? user.Email ?? "ME";

    var imgUrl = http.User.FindFirst("profile_img")?.Value;
    if (string.IsNullOrWhiteSpace(imgUrl))
    {
        var claims = await userManager.GetClaimsAsync(user);
        imgUrl = claims.FirstOrDefault(c => c.Type == "profile_img")?.Value;
    }
    imgUrl ??= "/pimg.png";

    return Results.Ok(new { name = displayName, email = user.Email ?? user.UserName, imageUrl = imgUrl });
})
    .RequireAuthorization();

// 프로필 이미지 업로드 및 검증
app.MapPost("/api/profile-image", async (
	HttpContext context,
	[FromForm] IFormFile file,
	UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
	IWebHostEnvironment env) =>
{
	var user = await userManager.GetUserAsync(context.User);
	if (user == null) return Results.Unauthorized();

	// 1. 유효성 검사 
	var ext = Path.GetExtension(file?.FileName)?.ToLower();
	var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

	if (file == null || file.Length == 0) return Results.BadRequest("파일이 없습니다.");
	if (file.Length > 2 * 1024 * 1024) return Results.BadRequest("2MB 이하만 가능합니다.");
	if (!allowed.Contains(ext)) return Results.BadRequest("허용되지 않는 확장자입니다.");

	// 2. 경로 설정 및 파일 저장
	var profileDir = Path.Combine(env.WebRootPath, "profiles");
	Directory.CreateDirectory(profileDir); 

	var fileName = $"{Guid.NewGuid():N}{ext}";
	var savePath = Path.Combine(profileDir, fileName);

	using (var stream = File.Create(savePath))
		await file.CopyToAsync(stream);

	var imageUrl = $"/profiles/{fileName}";

	// 3. 업데이트 및 기존 파일 삭제
	var claims = await userManager.GetClaimsAsync(user);
	var oldClaim = claims.FirstOrDefault(c => c.Type == "profile_img");

	if (oldClaim != null)
	{
		await userManager.RemoveClaimAsync(user, oldClaim);
		// 기존 파일 삭제 
		var oldFilePath = Path.Combine(env.WebRootPath, oldClaim.Value.TrimStart('/'));
		if (File.Exists(oldFilePath)) File.Delete(oldFilePath);
	}

	await userManager.AddClaimAsync(user, new Claim("profile_img", imageUrl));
    await signInManager.RefreshSignInAsync(user);

	return Results.Ok(new { imageUrl });
})

.DisableAntiforgery(); 

// DELETE /api/image
app.MapDelete("/api/profile-image", async (
    HttpContext http,
    UserManager<IdentityUser> userManager,
    IWebHostEnvironment env) =>
{
    var user = await userManager.GetUserAsync(http.User);
    if (user == null) return Results.Unauthorized();

    var claims = await userManager.GetClaimsAsync(user);
    var old = claims.FirstOrDefault(c => c.Type == "profile_img");
    var oldUrl = old?.Value;

    if (old != null)
        await userManager.RemoveClaimAsync(user, old);

    try
    {
        if (!string.IsNullOrWhiteSpace(oldUrl) && oldUrl.StartsWith("/profiles/"))
        {
            var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            var dir = Path.Combine(webRoot, "profiles");
            var fileName = Path.GetFileName(oldUrl);
            var physical = Path.Combine(dir, fileName);
            if (System.IO.File.Exists(physical))
                System.IO.File.Delete(physical);
        }
    }
    catch { }

    return Results.Ok(new { imageUrl = "/pimg.png" });
});

// api/csrf
app.MapGet("/api/csrf", (IAntiforgery anti, HttpContext ctx) =>
{
    var tokens = anti.GetAndStoreTokens(ctx);
    return Results.Ok(new { token = tokens.RequestToken });
})
    .RequireAuthorization()
    .DisableAntiforgery();

app.MapGet("/api/messages/{otherEmail}", async (string otherEmail, ApplicationDbContext db, HttpContext http, UserManager<IdentityUser> userManager) =>
{
	var me = await userManager.GetUserAsync(http.User);
	var other = await userManager.FindByEmailAsync(otherEmail);

	if (me == null || other == null) return Results.Unauthorized();

	var messages = await db.ChattingMsg
		.Where(m => (m.SenderId == me.Id && m.ReceiverId == other.Id) ||
					(m.SenderId == other.Id && m.ReceiverId == me.Id))
		.OrderBy(m => m.Timestamp)
		.Select(m => new {
			m.MessageText,
			isMe = m.SenderId == me.Id,
			m.Timestamp
		})
		.ToListAsync();

	return Results.Ok(messages);
})
    .RequireAuthorization();

app.Run();
