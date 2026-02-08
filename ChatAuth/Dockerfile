FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["ChatAuth.csproj", "./"]
RUN dotnet restore "ChatAuth.csproj"

# 나머지 소스 코드를 모두 복사하고 Release 모드로 빌드
COPY . .
RUN dotnet build "ChatAuth.csproj" -c Release -o /app/build

# 실행에 필요한 파일들 따로 추출
FROM build AS publish
RUN dotnet publish "ChatAuth.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. 실행 시 가벼운 런타임 이미지기만 사용하여 용량을 최적화
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

# 만들어진 결과물만 복사해옵니다.
COPY --from=publish /app/publish .

# 서버 실행
ENTRYPOINT ["dotnet", "ChatAuth.dll"]