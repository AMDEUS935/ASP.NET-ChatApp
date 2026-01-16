
# ASP.NET ChatApp 
(PHP/JS Polling → ASP.NET Core/WebSocket)

기존 **PHP + JavaScript(AJAX 폴링)** 기반 1:1 채팅을 **ASP.NET Core + SignalR(WebSocket)** 기반으로 이식한 프로젝트입니다.  
레거시의 “주기적 조회” 방식 대신, 서버가 클라이언트에 **즉시 Push** 하는 실시간 구조로 변경했습니다.

현재는 **웹소켓( SignalR ) 기반 실시간 통신과 기본 UI/인증 흐름**을 우선 구축한 상태이며, **세부적인 서버 기능은 미완성**인 프로토타입 버전입니다.  

---

## 1) Polling → SignalR(WebSocket)로 바꾼 이유

### Polling(주기적 조회)의 한계
- **불필요한 트래픽 증가**: 새 메시지가 없어도 일정 주기로 계속 요청 발생
- **지연**: “다음 폴링 주기”까지 메시지 표시가 늦어짐
- **서버 부하/확장성 문제**: 동시 접속자 증가 시 반복 요청이 누적되어 비용/부하 상승
- **상태 동기화 난이도**: 온라인 표시, 실시간 이벤트(입장/퇴장) 처리에 비효율

### SignalR(WebSocket) 전환 효과
- **실시간 Push**: 메시지/상태 변경을 서버가 즉시 전송
- **트래픽/부하 감소**: 이벤트가 있을 때만 통신
- **온라인 상태 연동이 자연스러움**: 연결/해제 이벤트로 온라인/오프라인 처리 용이
- **1:1 DM 구조에 적합**: SignalR Group을 이용해 DM 채널(룸) 단위로 송수신

---

## 2) 주요 기능 (핵심 변경점)

- **실시간 1:1 DM**: SignalR Hub + Group(대화방) 기반 메시지 송수신
- **로그인/회원가입**: ASP.NET Identity 기반 인증(쿠키)
- **유저 목록/검색**: 유저 페이지에서 사용자 리스트 조회 및 탐색
- **온라인/오프라인 표시**: SignalR 연결/해제 시점을 기반으로 상태 반영
- **프로필 이미지 업로드/삭제**
  - 이미지 업로드 시 서버 저장 + 사용자 Claim(profile_img)에 반영
  - 삭제 시 기본 이미지로 복귀(이전 파일 정리)

---

## 3) API / Endpoints

### Pages
- `/users.html`  
  유저 목록 + 내 프로필 이미지 변경 UI

- `/chat.html?other={email}`  
  특정 유저와의 1:1 채팅 UI

### REST API
- `GET /api/me`  
  내 정보 조회 (name / email / imageUrl)
- `GET /api/users`  
  유저 목록 조회 (email / name / isOnline)
- `POST /api/profile-image`  
  프로필 이미지 업로드 (예: 2MB 제한, jpg/png/gif/webp)
- `DELETE /api/profile-image`  
  프로필 이미지 삭제(기본 이미지로 복귀)
- `GET /api/csrf`  
  Anti-forgery 토큰 발급 (요청 헤더에 `X-CSRF-TOKEN` 사용)

### SignalR Hub
- `GET /hubs/chat`  
  인증 사용자만 연결 가능(Authorize)

**Client → Server**
- `JoinDm(otherEmail)` : 1:1 DM 그룹(대화방) 참가
- `SendDm(otherEmail, message)` : 상대에게 DM 전송

**Server → Client**
- `message(user, message)` : 메시지 수신 이벤트

---

## 4) 레거시 대비 변경사항(요약)

### 레거시(PHP/JS Polling)
- 클라이언트가 **주기적으로** 서버에 요청하여 새 메시지 조회
- 새 메시지가 없어도 요청이 반복됨(트래픽/부하 증가)
- 온라인 상태/실시간 이벤트 처리에 비효율

### 이식 후(ASP.NET Core + SignalR)
- 서버가 메시지를 **즉시 Push** (실시간)
- SignalR 연결 이벤트 기반으로 **온라인/오프라인** 반영
- 1:1 DM을 **Group(룸) 단위**로 관리하여 송수신 구조 단순화

---
