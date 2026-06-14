# Superchat - Business Messaging Platform

Open-source business messaging platform (Superchat alternative) built with .NET 8 Clean Architecture. Centralizes WhatsApp, Telegram, Email, and Web Chat into a unified inbox with CRM, automation, AI, campaigns, and team collaboration.

## Architecture

```
Superchat/
├── src/
│   ├── Core/
│   │   ├── Domain/         # 30+ entities across 7 sub-domains
│   │   └── Application/    # CQRS (MediatR), DTOs, FluentValidation
│   ├── Infrastructure/     # EF Core + SQLite, 15+ repositories, channel services
│   └── API/                # REST API + MVC views (single project)
│       ├── Controllers/    # REST API + HomeController
│       ├── Views/          # Razor views (inbox, CRM, campaigns, etc.)
│       └── wwwroot/        # Static assets (CSS, JS)
├── baileys-service/        # Node.js Baileys WhatsApp gateway
└── docker-compose.yml
```

The API project serves both the REST API (`/api/*`) and the frontend dashboard (`/`). Single deployable unit.

## Features

- **WhatsApp Business** — Connect via Baileys, shared inbox, message templates, broadcast campaigns
- **Omnichannel Inbox** — WhatsApp, Telegram, Email, Instagram, Messenger with unified conversation view
- **CRM** — Contact management, tags, lifecycle stages, segmentation
- **Team Collaboration** — Agent roles, conversation assignment, presence indicators
- **Automation** — Visual workflow builder (trigger → action), delay support
- **AI Agent** — Ollama (free, local), Claude, or OpenAI — configurable persona
- **Campaigns** — Schedule WhatsApp campaigns, track delivery/opens/clicks
- **Analytics** — KPIs, agent performance, trend charts (Chart.js)
- **Web Widget** — Customizable chat widget for websites

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Backend | .NET 8, C# 12 |
| Frontend | ASP.NET Core MVC + Razor Views |
| Database | SQLite (via EF Core) |
| CQRS | MediatR |
| Validation | FluentValidation |
| Real-time | SignalR |
| WhatsApp | Baileys (Node.js) |
| AI | Ollama (free, local, default) / Claude / OpenAI |
| UI | Bootstrap 5, Chart.js |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/) (only for WhatsApp Baileys service)
- [Ollama](https://ollama.com/) (optional, for local AI)

## Quick Start

### 1. Start the API (also serves the frontend)

```bash
dotnet run --project src/API
```

Open **http://localhost:5000** in your browser — the full dashboard loads immediately.

### 2. (Optional) Install Ollama for free local AI

```bash
ollama pull llama3.2
```

The API auto-connects to `http://localhost:11434`. No API key needed.

### 3. (Optional) Start Baileys for WhatsApp

```bash
cd baileys-service
npm install
npm start
```

Then scan the QR code in Settings → Connection.

## Quick Start (no WhatsApp, no AI)

Just `dotnet run --project src/API` and open `http://localhost:5000`. All panels work — only WhatsApp messaging and AI auto-replies depend on external services.

## Docker

```bash
docker compose up --build
```

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
| GET/PUT | `/api/config/ai/{id}` | AI configuration |
| GET | `/api/config/qr` | WhatsApp QR code |
| GET | `/api/config/status` | Connection status |
| GET/PUT | `/api/widget/config` | Web widget config |

## All Open Source & Free

Zero paid dependencies:
- **Ollama** — free local AI (default)
- **SQLite** — file-based database
- **Baileys** — free WhatsApp Web API
- **Bootstrap 5 + Chart.js** — free UI
- **SignalR** — free real-time WebSockets
- **MediatR, FluentValidation** — free NuGet packages
