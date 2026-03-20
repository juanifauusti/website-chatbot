(function () {
  // CONFIGURACIÓN
  const API_URL = "https://website-chatbot-nmfp.onrender.com/chat";
  const scriptTag = document.currentScript;
  const siteId = scriptTag?.getAttribute("data-site-id") || "demo-portfolio";

  const button = document.createElement("button");
  button.innerHTML = "💬";
  Object.assign(button.style, {
    position: "fixed", bottom: "20px", right: "20px", width: "60px", height: "60px",
    borderRadius: "50%", background: "#4f46e5", color: "white", border: "none",
    fontSize: "24px", cursor: "pointer", boxShadow: "0 4px 12px rgba(0,0,0,0.2)", zIndex: "10000"
  });

  const chatBox = document.createElement("div");
  Object.assign(chatBox.style, {
    position: "fixed", bottom: "90px", right: "20px", width: "350px", height: "450px",
    background: "white", borderRadius: "12px", display: "none", flexDirection: "column",
    boxShadow: "0 8px 24px rgba(0,0,0,0.2)", zIndex: "10000", overflow: "hidden",
    fontFamily: "'Segoe UI', Roboto, Helvetica, Arial, sans-serif"
  });

  chatBox.innerHTML = `
    <div style="background:#4f46e5; color:white; padding:15px; font-weight:bold; display:flex; justify-content:space-between; align-items:center;">
        <span>Asistente NovaTech</span>
        <span id="close-chat" style="cursor:pointer; font-size:20px;">×</span>
    </div>
    <div id="messages" style="flex:1; padding:15px; overflow-y:auto; background:#f9fafb; display:flex; flex-direction:column; gap:10px;">
        <div style="align-self:flex-start; background:#e5e7eb; color:#1f2937; padding:8px 12px; border-radius:12px 12px 12px 0; max-width:80%; font-size:14px;">
            ¡Hola! Soy tu asistente virtual. ¿En qué puedo ayudarte hoy?
        </div>
    </div>
    <div style="display:flex; padding:10px; border-top:1px solid #eee; background:white;">
      <input id="chat-input-field" style="flex:1; border:1px solid #ddd; padding:10px; border-radius:4px; outline:none; font-size:14px;" placeholder="Escribí tu duda..." autocomplete="off" />
      <button id="chat-send-btn" style="background:#4f46e5; color:white; border:none; padding:8px 15px; margin-left:5px; border-radius:4px; cursor:pointer; font-weight:bold;">➤</button>
    </div>
    <style>
      @keyframes blink { 0% { opacity: .2; } 20% { opacity: 1; } 100% { opacity: .2; } }
      .dot { width:6px; height:6px; background:#6b7280; border-radius:50%; animation: blink 1.4s infinite both; }
    </style>
  `;

  document.body.appendChild(button);
  document.body.appendChild(chatBox);

  const inputField = chatBox.querySelector("#chat-input-field");
  const sendBtn = chatBox.querySelector("#chat-send-btn");
  const msgContainer = chatBox.querySelector("#messages");
  const closeBtn = chatBox.querySelector("#close-chat");

  button.onclick = () => {
    chatBox.style.display = chatBox.style.display === "none" ? "flex" : "none";
    if (chatBox.style.display === "flex") inputField.focus();
  };
  
  closeBtn.onclick = () => chatBox.style.display = "none";

  const sendMessage = async () => {
    const text = inputField.value.trim();
    if (!text) return;

    msgContainer.innerHTML += `<div style="align-self:flex-end; background:#4f46e5; color:white; padding:8px 12px; border-radius:12px 12px 0 12px; max-width:80%; font-size:14px;">${text}</div>`;
    inputField.value = "";
    msgContainer.scrollTop = msgContainer.scrollHeight;

    const typingId = "typing-" + Date.now();
    const typingHtml = `
      <div id="${typingId}" style="align-self:flex-start; background:#e5e7eb; padding:12px; border-radius:12px 12px 12px 0; display:flex; gap:4px;">
        <span class="dot"></span>
        <span class="dot" style="animation-delay: 0.2s;"></span>
        <span class="dot" style="animation-delay: 0.4s;"></span>
      </div>`;
    msgContainer.insertAdjacentHTML("beforeend", typingHtml);
    msgContainer.scrollTop = msgContainer.scrollHeight;

    try {
      const res = await fetch(API_URL, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ Message: text, SiteId: siteId })
      });

      if (!res.ok) throw new Error("Error en servidor");

      const data = await res.json();
      document.getElementById(typingId)?.remove();

      msgContainer.innerHTML += `<div style="align-self:flex-start; background:#e5e7eb; color:#1f2937; padding:8px 12px; border-radius:12px 12px 12px 0; max-width:80%; font-size:14px;">${data.answer}</div>`;
    } catch (e) {
      document.getElementById(typingId)?.remove();
      msgContainer.innerHTML += `<div style="align-self:flex-start; background:#fee2e2; color:#b91c1c; padding:8px 12px; border-radius:12px; font-size:12px;">Error: No se pudo conectar con el servidor.</div>`;
    }
    msgContainer.scrollTop = msgContainer.scrollHeight;
  };

  sendBtn.addEventListener("click", sendMessage);
  inputField.addEventListener("keypress", (e) => {
    if (e.key === "Enter") sendMessage();
  });
})();