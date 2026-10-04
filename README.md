# Real-Time Chat

Educational project focused on the evolution of real-time web communication.

The same chat application was implemented several times using increasingly modern approaches:

1. Classic PHP request/response
2. Short Polling
3. Long Polling
4. Blazor WebAssembly with HTTP API + SignalR
5. Blazor Server with server-side real-time coordination

The final version is implemented with **.NET 10, Blazor Server, ASP.NET Core Identity, EF Core, and PostgreSQL**.

## Final Features

- User authentication with ASP.NET Core Identity and cookies
- Predefined chat rooms
- Public and private messages
- Persistent message history
- Online users
- Typing indicator
- Automatic connection restoration
- Adaptive interface
- Message history scrolling that preserves the user's position

## Repository Structure

```text
apps/
├── php-chat/
└── dotnet-chat/

docs/
├── Architecture.md
├── ArchitectureEvolution.md
└── Deployment.md
```

`php-chat` contains the Classic PHP, Short Polling, and Long Polling stages.

`dotnet-chat` contains the .NET evolution and the final Blazor Server implementation.

## Documentation

- [Architecture](docs/Architecture.md) — final application architecture.
- [Architecture Evolution](docs/ArchitectureEvolution.md) — how the project evolved from classic HTTP requests to server-side real-time UI.
- [Deployment](docs/Deployment.md) — production deployment notes.

## Purpose

The project was built primarily as a learning exercise: to understand how real-time communication changes when moving from traditional HTTP requests to polling, WebSockets, SignalR, and finally Blazor Server.