(function () {
  const button = document.createElement("button");
  button.innerText = "💬";
  button.style.position = "fixed";
  button.style.bottom = "20px";
  button.style.right = "20px";
  button.style.width = "60px";
  button.style.height = "60px";
  button.style.borderRadius = "50%";
  button.style.background = "#4f46e5";
  button.style.color = "white";
  button.style.border = "none";
  button.style.fontSize = "24px";
  button.style.cursor = "pointer";

  document.body.appendChild(button);

  const chatBox = document.createElement("div");
  chatBox.style.position = "fixed";
  chatBox.style.bottom = "90px";
  chatBox.style.right = "20px";
  chatBox.style.width = "300px";
  chatBox.style.height = "400px";
  chatBox.style.background = "white";
  chatBox.style.borderRadius = "10px";
  chatBox.style.display = "none";
  chatBox.style.flexDirection = "column";
  chatBox.style.boxShadow = "0 5px 20px rgba(0,0,0,0.2)";
  chatBox.style.display = "none";

  chatBox.innerHTML = `
    <div id="messages" style="flex:1; padding:10px; overflow:auto;"></div>
    <div style="display:flex; border-top:1px solid #ddd;">
      <input id="input" style="flex:1; border:none; padding:10px;" placeholder="Escribí..." />
      <button id="send" style="background:#4f46e5; color:white; border:none; padding:10px;">➤</button>
    </div>
  `;

  document.body.appendChild(chatBox);

  button.onclick = () => {
    chatBox.style.display = chatBox.style.display === "none" ? "flex" : "none";
  };

  chatBox.querySelector("#send").onclick = async () => {
    const input = chatBox.querySelector("#input");
    const messages = chatBox.querySelector("#messages");

    const userMessage = input.value.trim();
    if (!userMessage) return;

    messages.innerHTML += `<div style="text-align:right;">${userMessage}</div>`;

    const res = await fetch("http://localhost:5193/chat", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ message: userMessage }),
    });

    const data = await res.json();

    messages.innerHTML += `<div style="text-align:left;">${data.answer}</div>`;

    input.value = "";
    messages.scrollTop = messages.scrollHeight;
  };
})();
