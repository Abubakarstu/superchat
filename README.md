# Superchat - WhatsApp AI Chatbot

A .NET 8 Web API using Clean Architecture for a WhatsApp AI Chatbot integration with Anthropic Claude / OpenAI.

## Architecture

```
Superchat/
├── src/
│   ├── Core/
│   │   ├── Domain/        # Entities, enums, repository interfaces
│   │   └── Application/   # CQRS (MediatR), DTOs, FluentValidation, service interfaces
│   ├── Infrastructure/    # EF Core, WhatsAppService, AiService implementations
│   └── API/              # Controllers, SignalR hub, Program.cs
├── baileys-service/       # Node.js + Baileys WhatsApp microservice
├── frontend/             # Bootstrap 5 + Tailwind HTML dashboard
├── docker-compose.yml    # Orchestrates .NET API + Baileys service
└── Superchat.sln
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- An AI API key (Anthropic Claude or OpenAI)
- Docker & Docker Compose (optional, for containerized setup)

## Quick Start (Development)

### 1. Configure Secrets

Copy and edit `src/API/appsettings.Development.json`:

```json
{
  "Ai": {
    "Provider": "claude",
    "ApiKey": "sk-ant-..."
  },
  "BaileysService": {
    "BaseUrl": "http://localhost:3001"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=superchat.db"
  }
}
```

### 2. Start Baileys Service

```bash
cd baileys-service
npm install
npm start
```

### 3. Start .NET API

```bash
dotnet run --project src/API
```

### 4. Open Frontend

Open `frontend/index.html` in your browser (or serve with any static server).

## Setting Up WhatsApp

1. Start the Baileys service and watch the terminal for a QR code.
2. Open the frontend, click **Settings** → **Connection** tab to see the QR.
3. Open WhatsApp on your phone → **Linked Devices** → **Link a Device**.
4. Scan the QR code.

## AI Provider Configuration

### Claude (Anthropic)
- Provider: `claude`
- Model: `claude-sonnet-4-20250514` (or `claude-3-haiku-20240307`)
- Get API key from: https://console.anthropic.com/

### OpenAI
- Provider: `openai`
- Model: `gpt-4o` (or `gpt-3.5-turbo`)
- Get API key from: https://platform.openai.com/api-keys

## Docker Deployment

```bash
export AI_API_KEY=sk-ant-...
export AI_PROVIDER=claude
docker compose up --build
```

The API will be available at `http://localhost:5000` and Swagger UI at `http://localhost:5000/swagger`.

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/conversations` | List all conversations |
| GET | `/api/conversations/{id}/messages` | Get messages for a conversation |
| POST | `/api/conversations/{remoteJid}/messages` | Send a message |
| POST | `/api/webhook/incoming` | Webhook for incoming WhatsApp messages |
| GET | `/api/config/ai` | Get AI configuration |
| PUT | `/api/config/ai/{id}` | Update AI configuration |
| GET | `/api/config/qr` | Get WhatsApp QR code |
| GET | `/api/config/status` | Get connection status |

## Real-time Updates

SignalR hub is available at `/hubs/messages` for real-time message and conversation updates.
