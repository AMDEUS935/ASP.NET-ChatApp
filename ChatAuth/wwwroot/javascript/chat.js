const inputField = document.querySelector(".input-field");
const sendBtn = document.querySelector("button");
const chatBox = document.querySelector(".chat-box");

function addMessage(isMine, message) {
	const div = document.createElement("div");
	div.className = "chat " + (isMine ? "outgoing" : "incoming");
	div.innerHTML = `<div class="details"><p>${message}</p></div>`;
	chatBox.appendChild(div);
	chatBox.scrollTop = chatBox.scrollHeight;
}

// 1) SignalR 연결
const conn = new signalR.HubConnectionBuilder()
	.withUrl("/hubs/chat")
	.withAutomaticReconnect()
	.build();

// 2) 서버 -> 클라이언트 (Push)
conn.on("message", (user, message) => {
	const isMine = false; 
	addMessage(isMine, message);
});

// 3) 연결 시작
await conn.start();

// 4) DM Join 
await conn.invoke("JoinDm", otherEmail);

// 5) 메시지 전송
sendBtn.onclick = async () => {
	const msg = inputField.value.trim();
	if (!msg) return;

	await conn.invoke("SendDm", otherEmail, msg);
	addMessage(true, msg); 
	inputField.value = "";
};