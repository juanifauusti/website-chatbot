# Website Chatbot

Chatbot para sitios web basado en **RAG (Retrieval Augmented Generation)**.  
Permite responder preguntas utilizando contenido específico mediante búsqueda semántica e integración con modelos de IA.

```markdown
🔗 Demo: https://website-chatbot-juana.vercel.app

---

## 🚀 Features

- Generación de embeddings a partir de contenido del sitio
- Búsqueda semántica mediante **cosine similarity**
- Integración con API de IA (Cohere)
- API backend desarrollada en **ASP.NET**
- Widget embebible en cualquier sitio web
- Arquitectura desacoplada (backend + frontend)

---

## 🧠 Cómo funciona

1. Se procesa el contenido del sitio y se divide en fragmentos (chunks)
2. Cada fragmento se convierte en un embedding vectorial
3. Los embeddings se almacenan en un archivo (`embeddings.json`)
4. Cuando el usuario realiza una consulta:
   - Se genera el embedding de la pregunta
   - Se busca el fragmento más relevante mediante similitud de coseno
   - Se envía el contexto + pregunta a la IA
   - Se genera la respuesta final

---

## 🛠️ Stack Tecnológico

- **Backend:** ASP.NET (C#)
- **Frontend:** JavaScript (widget embebible)
- **IA:** Cohere API
- **Embeddings:** modelo `embed-multilingual-v3.0`
- **Algoritmo:** Cosine Similarity

---

## 📦 Uso

### Generar contenido del chatbot

1. Crear un archivo `siteContent.txt` con la información del sitio (separar los bloques con `--`)
2. Ejecutar el generador de embeddings cambiando la siteId (`Program.cs` en `embeddings-generator`)
3. Se generará el archivo `embeddings.json`
4. Copiar `embeddings.json` dentro de `chatbot-api`
5. Configurar la URL del sitio en la whitelist del backend

---

### Integrar el chatbot en un sitio web

Agregar el script del widget en el HTML:

```html
<script src="https://website-chatbot-juana.vercel.app/chatbot.js"></script>