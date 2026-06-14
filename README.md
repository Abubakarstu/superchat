# Superchat - Business Messaging Platform

Open-source business messaging platform (Superchat alternative) built with .NET 8 Clean Architecture. Centralizes WhatsApp, Telegram, Email, and Web Chat into a unified inbox with CRM, automation, AI, campaigns, and team collaboration.

## Architecture

```
Superchat/
├── src/
│   ├── Core/
│   │   ├── Domain/       # 30+ entities across 7 sub-domains
│   │   └── Application/  # CQRS (MediatR), DTOs, FluentValidation
│   ├── Infrastructure/   # EF Core + SQLite, 15+ repositories, channel services
│   └── API/              # REST controllers, SignalR hub, Swagger
├── baileys-service/      # Node.js Baileys WhatsApp gateway
├── frontend/             # Bootstrap 5 SPA dashboard
└── docker-compose.yml
```

## Features

### WhatsApp Business
- Connect multiple WhatsApp numbers via Baileys
- Shared team inbox with assignment
- Message templates (Utility/Marketing/Authentication)
- Broadcast campaigns with segmentation

### Omnichannel Inbox
- WhatsApp, Telegram, Email, Instagram, Messenger support
- Unified conversation view with channel badges
- Cross-channel message history

### CRM
- Contact management with tags and lifecycle stages
- Custom fields, notes, activity timeline
- Segmentation for campaigns

### Team Collaboration
- Agent management with roles (admin/manager/agent)
- Conversation assignment and ownership
- Internal notes and mentions
- Department routing via agent groups

### Automation
- Visual workflow builder (trigger → action)
- Triggers: new message, new lead, campaign click
- Actions: assign agent, add tag, send reply
- Delay and conditional branching support

### AI Agent
- Claude or OpenAI integration
- Configurable system prompt and persona
- Temperature and max tokens control
- Auto-reply to incoming messages

### Campaigns & Broadcasts
- Create and schedule WhatsApp campaigns
- Audience segmentation by tags
- Delivery, open, click, reply tracking
- Opt-in/opt-out management

### Analytics
- Dashboard with KPIs (conversations, messages, response times)
- Agent performance metrics
- Conversation trends chart
- Channel distribution chart

### Web Widget
- Customizable chat widget for websites
- WhatsApp click-to-chat
- Bot support with agent handoff

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Backend | .NET 8, C# 12 |
| Database | SQLite (via EF Core) |
| CQRS | MediatR |
| Validation | FluentValidation |
| Real-time | SignalR |
| WhatsApp | Baileys (Node.js) |
| AI | Claude / OpenAI |
| Frontend | Bootstrap 5, Chart.js |
| Container | Docker Compose |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- AI API key (Anthropic or OpenAI)

## Quick Start

### 1. Configure

```json
// src/API/appsettings.Development.json
{
  "Ai": { "Provider": "claude", "ApiKey": "sk-ant-..." },
  "BaileysService": { "BaseUrl": "http://localhost:3001" },
  "ConnectionStrings": { "DefaultConnection": "Data Source=superchat.db" }
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

### 4. Open Dashboard

Open `frontend/index.html` in your browser.

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET/POST | `/api/conversations` | List/Send conversations |
| GET | `/api/conversations/{id}/messages` | Get messages |
| POST | `/api/webhook/incoming` | Incoming message webhook |
| GET/POST | `/api/contacts` | CRUD contacts |
| GET/POST | `/api/campaigns` | Manage campaigns |
| POST | `/api/campaigns/{id}/send` | Send campaign |
| GET/POST | `/api/templates` | WhatsApp templates |
| GET/POST | `/api/workflows` | Automation workflows |
| GET/POST | `/api/team/agents` | Manage agents |
| POST | `/api/team/assign` | Assign conversation |
| GET/POST | `/api/channels` | Connect channels |
| GET | `/api/analytics/dashboard` | Analytics dashboard |
| GET | `/api/config/ai` | AI configuration |
| PUT | `/api/config/ai/{id}` | Update AI config |
| GET | `/api/config/qr` | WhatsApp QR code |
| GET | `/api/config/status` | Connection status |
| GET | `/api/widget/config` | Web widget config |

## Docker

```bash
export AI_API_KEY=sk-ant-...
docker compose up --build
```

## All Open Source & Free

This project uses zero paid dependencies:
- **SQLite** — file-based database, no server needed
- **Baileys** — free WhatsApp Web API (no Business API fees)
- **Bootstrap 5** — free UI framework
- **Chart.js** — free analytics charts
- **SignalR** — free real-time WebSockets
- **MediatR, FluentValidation, AutoMapper** — free NuGet packages
