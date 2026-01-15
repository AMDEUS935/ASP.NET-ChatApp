// Program.cs는 ASP.NET Core 앱의 시작점
// 여기서 어떤 기능(서비스)을 쓸지 등록하고, 어떤 URL로 어떤 기능을 제공할지(라우팅) 연결

var builder = WebApplication.CreateBuilder(args);

// SignalR 사용을 위한 서비스 등록
// - "Hub" 라는 실시간 통신 엔드포인트를 만들 수 있게 해줌
builder.Services.AddSignalR();

var app = builder.Build();

// 기본 문서(index.html 같은 파일)를 자동으로 찾아주는 미들웨어
// - 브라우저가 / 로 접속했을 때 wwwroot/index.html이 있으면 그걸 보여줘
app.UseDefaultFiles();

// wwwroot 폴더 안의 정적 파일(html/css/js)을 서빙하는 미들웨어
// - 즉, 우리가 만들 index.html을 브라우저가 받아갈 수 있게 됨
app.UseStaticFiles();

// "/hubs/chat" 이라는 경로로 SignalR Hub를 연결
// - JS 클라이언트가 이 주소로 연결해서 메시지를 주고 받게 됨
app.MapHub<ChatHub>("/hubs/chat");

// 앱 실행
app.Run();
