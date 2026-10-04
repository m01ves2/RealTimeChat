# Architecture

The final Real-Time Chat implementation uses **Blazor Server** with a layered .NET architecture.

## Overview

```text
Browser
   │
   │ Blazor Server circuit
   ▼
RealTimeChat.BlazorServer
   │
   ├── Razor Components
   ├── ASP.NET Core Identity
   ├── ChatNotifier
   ├── RoomPresence
   ├── TypingNotifier
   └── RoomCircuitHandler
   │
   ▼
RealTimeChat.Application
   │
   └── ChatService
   │
   ▼
RealTimeChat.Infrastructure
   │
   ├── EF Core
   ├── Identity persistence
   └── Repositories
   │
   ▼
PostgreSQL
```

## Projects

- **RealTimeChat.Domain** — chat entities and domain validation.
- **RealTimeChat.Application** — application models, repository abstractions, and `ChatService`.
- **RealTimeChat.Infrastructure** — EF Core, PostgreSQL persistence, ASP.NET Core Identity, and repository implementations.
- **RealTimeChat.BlazorServer** — final UI, authentication flow, real-time coordination, and application host.

## Real-Time Communication

The final Blazor Server UI does not use a separate application SignalR Hub.

Blazor already maintains a persistent circuit between the browser and the server. Communication between different users' components is coordinated inside the server process:

- `ChatNotifier` delivers new messages to active room components.
- `RoomPresence` tracks online room participants.
- `TypingNotifier` distributes typing state changes.
- `RoomCircuitHandler` tracks circuit connection and reconnection state.

These services allow multiple Blazor Server circuits to exchange real-time state while the UI remains server-side.

## Data and Authentication

ASP.NET Core Identity provides cookie authentication.

PostgreSQL stores:

- users;
- predefined rooms;
- public and private messages.

EF Core is used through the Infrastructure layer.

## Message Flow

```text
User submits message
        ↓
Room component
        ↓
ChatService
        ↓
Repository / EF Core
        ↓
PostgreSQL
        ↓
ChatNotifier
        ↓
Other active room components
        ↓
Blazor renders updated UI
```

Private messages are stored in the same message table and are filtered by author and recipient when history is loaded or messages are delivered.