# Architecture Evolution

This document describes how Real-Time Chat evolved from a traditional PHP website into a real-time .NET application.

The purpose of the project was not only to build a chat application, but to implement the same problem using several communication models and understand what each new approach solves.

## Table of Contents

- [Evolution Overview](#evolution-overview)
- [Stage 0 - Classic PHP Chat](#stage-0---classic-php-chat)
- [Stage 1 - PHP Short Polling](#stage-1---php-short-polling)
- [Stage 2 - PHP Long Polling](#stage-2---php-long-polling)
- [Stage 3 - Raw WebSocket Prototype](#stage-3---raw-websocket-prototype)
- [Stage 4 - SignalR Prototype](#stage-4---signalr-prototype)
- [Stage 5 - Blazor WebAssembly Chat](#stage-5---blazor-webassembly-chat)
- [Stage 6 - Blazor Server Chat](#stage-6---blazor-server-chat)
- [Final Comparison](#final-comparison)

---

## Evolution Overview

```text
Classic PHP
    ↓
Short Polling
    ↓
Long Polling
    ↓
Raw WebSocket
    ↓
SignalR
    ↓
Blazor WebAssembly
HTTP API + SignalR
    ↓
Blazor Server
server-side UI + in-process coordination
```

The first three stages use PHP and progressively improve how the browser receives new data.

The .NET stages then move from low-level persistent connections to SignalR and finally to two complete Blazor clients with very different execution models.

---

# Stage 0 - Classic PHP Chat

## Model

The first implementation is a traditional server-rendered PHP application.

Every meaningful action causes a normal HTTP request:

```text
Browser
   ↓ HTTP request
PHP
   ↓
PostgreSQL
   ↓
HTML response
Browser
```

The server generates HTML and the browser displays it.

There is no persistent application connection between browser and server.

The conversation was displayed inside an iframe. Its document periodically refreshed itself using an HTML meta refresh, allowing the message list to update without reloading the entire chat page.

## Message flow

Sending a message follows the normal request/response model:

```text
HTML form
    ↓ POST
PHP endpoint
    ↓
PostgreSQL
    ↓
303 redirect
    ↓
chat page
```

Messages are stored in PostgreSQL.

The conversation view is rendered by PHP from database data.

## Features

The Classic PHP version already contains the basic chat domain:

- lobby;
- predefined rooms;
- nickname entry;
- public messages;
- private messages;
- message history;
- online visitors;
- PostgreSQL persistence.

The application uses sessions to identify the current chat participant.

## Limitation

The main problem is receiving new messages.
The server can only send a response after the browser first sends a request.
If another user sends a message, the browser does not know about it automatically.
The page must therefore be refreshed or another request must be made.
This is the limitation that motivates polling.

---

# Stage 1 - PHP Short Polling

## Model

Short Polling keeps the same PHP and HTTP architecture but adds JavaScript in the browser.

The browser periodically asks the server for new data:

```text
Browser
   │
   ├── request
   │      ↓
   │    PHP
   │      ↓
   │   response
   │
   ├── wait 5 seconds
   │
   └── repeat
```

The server still behaves like a traditional HTTP server.

The important change is that the browser now initiates requests automatically.

## Message flow

Sending messages is performed in the background with `fetch()` rather than by navigating the page.
Receiving messages uses repeated requests scheduled with `setTimeout()`.
This makes the application feel much closer to a real-time chat.

## Advantages

Compared with the Classic PHP version:

- the whole page no longer needs to reload;
- new messages appear automatically;
- message sending can happen in the background;
- scroll position can be preserved;
- the UI becomes more responsive.

## Limitation

Most polling requests may return no useful data.

For example:

```text
12:00:00 request → nothing new
12:00:05 request → nothing new
12:00:10 request → nothing new
12:00:15 request → message
```

There is also an unavoidable delay between message creation and the next polling request.
Reducing the interval improves latency but increases the number of requests.

Short Polling therefore introduces a tradeoff:

```text
lower latency
    ↕
more HTTP requests
```

Long Polling attempts to remove this problem.

---

# Stage 2 - PHP Long Polling

## Model

Long Polling still uses ordinary HTTP.
The difference is that the server does not immediately return an empty response when there is no new data.

Instead, the request waits:

```text
Browser
   ↓ request
PHP
   │
   │ wait for new data
   │ ...
   │ ...
   ↓
response
```

After the response arrives, the browser immediately starts another long-poll request.

## Message flow

The browser keeps approximately one receive request active.

The server checks whether new messages are available and returns when:

- new data appears; or
- the long-poll timeout is reached.

The next request then begins.

## Advantages

Compared with Short Polling:

- fewer useless requests;
- lower delivery latency;
- messages can appear almost immediately;
- the architecture still uses ordinary HTTP.

From the user's point of view, the chat now behaves much more like a real-time application.

## Limitation

Long Polling is still built around HTTP request/response semantics.
A connection is held open only temporarily.

After every response:

```text
request
    ↓
wait
    ↓
response
    ↓
new request
```

This is an effective workaround, but it is not a true persistent bidirectional communication channel.

Typing indicators were not implemented in the PHP stages. They were technically possible, but would have required additional polling/heartbeat state and were outside the purpose of those stages.

The next stage therefore explores WebSocket.

---

# Stage 3 - Raw WebSocket Prototype

## Purpose

The first .NET stage was intentionally not a complete chat.
It was a small prototype created to understand what a persistent WebSocket connection actually requires.

## Model

After the initial HTTP handshake, the connection is upgraded:

```text
Browser
   │
   │ HTTP Upgrade
   ▼
ASP.NET Core
   │
   │ WebSocket
   │◄────────────►
   │
Browser
```

The connection remains open and either side can send data at any time.

This is fundamentally different from polling.

## Implementation

The prototype implemented the important low-level mechanics manually:

- WebSocket upgrade;
- receive loop;
- send loop;
- fragmented message assembly;
- connection tracking;
- per-connection outgoing channels;
- broadcasting;
- cancellation;
- maximum message size;
- close handshake.

A connection manager tracked active WebSocket connections.
Each connection had independent receive and send processing.

## What WebSocket solves

There is no longer a repeated cycle of:

```text
request → response → request → response
```

Instead:

```text
persistent connection
     ↕
messages in both directions
```

The server can push a message immediately.

## What the prototype revealed

WebSocket provides the transport, but many application-level concerns remain manual.

The application must decide how to implement:

- connection management;
- user identification;
- groups or rooms;
- reconnection;
- message routing;
- serialization;
- errors;
- connection cleanup.

The prototype was useful precisely because this infrastructure was visible.

The next stage uses SignalR to see which parts can be delegated to a higher-level framework.

---

# Stage 4 - SignalR Prototype

## Purpose

Like the Raw WebSocket stage, the first SignalR implementation was intentionally small.

Its goal was to compare SignalR's programming model with the infrastructure implemented manually in the previous stage.

## Model

Instead of working directly with WebSocket frames and connection loops, the application exposes Hub methods.

```text
Client
   │
   │ Invoke("SendMessage")
   ▼
SignalR Hub
   │
   │ SendAsync("ReceiveMessage")
   ▼
Clients
```

The client subscribes to named events and invokes server methods.

## What SignalR hides

Compared with the Raw WebSocket implementation, SignalR takes responsibility for much of the connection infrastructure.

The application no longer needs its own:

- receive loop;
- send loop;
- outgoing channel per connection;
- WebSocket message assembly;
- connection manager;
- manual broadcast loop.

The programming model becomes event-oriented rather than transport-oriented.

## Important lesson

Raw WebSocket and SignalR solve problems at different abstraction levels.

WebSocket answers:

> How can two endpoints maintain a persistent bidirectional connection?

SignalR answers something closer to:

> How can application clients invoke methods and receive real-time events?

This made SignalR the natural technology for the first complete .NET chat implementation.

---

# Stage 5 - Blazor WebAssembly Chat

## Overview

The next stage turns the prototypes into a complete .NET chat application.

The browser runs a Blazor WebAssembly client.

The server provides:

- HTTP API;
- SignalR Hub;
- authentication;
- application services;
- persistence.

```text
Blazor WebAssembly
      │
      ├── HTTP API
      │
      └── SignalR
              │
              ▼
       ASP.NET Core Server
              │
              ▼
          Application
              │
              ▼
        Infrastructure
              │
              ▼
          PostgreSQL
```

## HTTP and SignalR responsibilities

HTTP and SignalR are used for different purposes.

The HTTP API handles operations such as:

- authentication;
- loading rooms;
- loading message history.

SignalR handles live events:

- new messages;
- online users;
- typing notifications.

This separation became one of the most important architectural differences compared with the PHP versions.

## Authentication

ASP.NET Core Identity provides user management.

Authentication uses cookies.
The same authenticated browser session is used by HTTP requests and the SignalR connection.
Unlike the PHP versions, users now have persistent application accounts rather than room-local nicknames.

## Rooms

A SignalR connection joins a server group representing the current room.

Conceptually:

```text
connection
    ↓
room:1
```

When the client changes rooms, it leaves the previous group and joins another one.
This allows public messages to be broadcast only to users in the same room.

## Private messages

Private messages are persisted in the same database table as public messages.

A nullable recipient ID distinguishes them:

```text
RecipientId = null
    → public

RecipientId = user ID
    → private
```

SignalR sends a private message only to the author and recipient.

Message-history queries apply the same visibility rules.

## Presence

The server tracks active SignalR connections.

Each connection records:

- room;
- user ID;
- username.

A user can have several connections, for example multiple browser tabs.
The UI shows unique users rather than individual connections.

## Typing indicator

Typing notifications are transient and are not stored in PostgreSQL.
The client periodically notifies the Hub while the user types.
Other clients display the typing state for a short period.
Repeated notifications extend that period.

## Reconnection

SignalR automatic reconnect handles temporary connection loss.

After reconnecting, the client:

1. rejoins the room;
2. reloads recent history;
3. merges missing messages;
4. restores the online state.

This is necessary because messages may have been created while the client was disconnected.

## Client execution model

An important property of this architecture is that Blazor WebAssembly executes application UI code in the browser.

The browser therefore communicates with the server explicitly:

```text
UI
 ↓
HttpClient / HubConnection
 ↓
Server
```

This becomes the main point of comparison with Blazor Server.

---

# Stage 6 - Blazor Server Chat

## Overview

The final implementation keeps the same Domain, Application, Infrastructure, Identity, EF Core, and PostgreSQL layers, but replaces the WebAssembly client architecture.

The UI now executes on the server.

```text
Browser
   │
   │ Blazor circuit
   ▼
Blazor Server
   │
   ├── Razor components
   ├── real-time coordination services
   │
   ▼
Application
   │
   ▼
Infrastructure
   │
   ▼
PostgreSQL
```

The browser receives UI updates produced by server-side components.

## The important architectural change

The Blazor Server UI does not need an application SignalR Hub to communicate with its own backend.
The component already executes inside the ASP.NET Core server process.

Therefore this WebAssembly path:

```text
Component
    ↓
HTTP API
    ↓
ChatService
```

becomes:

```text
Component
    ↓
ChatService
```

The HTTP API is unnecessary for the Blazor Server UI.

From a learning perspective, the WebAssembly version exposes the real-time boundary more explicitly: the client creates a HubConnection, invokes Hub methods, and subscribes to server events. In Blazor Server, the framework owns the browser/server circuit, so most transport details disappear from application code.

## Database lifetime

Blazor Server introduces another important difference.

A normal scoped dependency in an HTTP application often lives for one HTTP request.

In interactive Blazor Server, a scope can live with the circuit for much longer.
Keeping an EF Core `DbContext` for the complete circuit lifetime would therefore be undesirable.

The room component creates short DI scopes for database operations.

Conceptually:

```text
UI operation
    ↓
new DI scope
    ↓
ChatService
    ↓
repositories
    ↓
DbContext
    ↓
database operation finishes
    ↓
scope disposed
```

This keeps database units of work short-lived.

## Real-time communication between users

A new question appears:

> If there is no application ChatHub, how does one user's component notify another user's component?

The answer is in-process coordination.
The final application uses singleton services shared by all active circuits.

### ChatNotifier

`ChatNotifier` distributes newly stored messages to active room components.

Each component registers its own subscription.

Private messages are delivered only to subscriptions belonging to the author or recipient.

```text
Room component A
      ↓
ChatService
      ↓
PostgreSQL
      ↓
ChatNotifier
      ├── Room component A
      └── Room component B
```

The message is persisted before notification.
The database remains the source of durable history.

### RoomPresence

`RoomPresence` tracks active participation in chat rooms.

Participation is identified independently from user identity because one user can open multiple tabs.

The UI then collapses several participations belonging to the same account into one visible online user.

### TypingNotifier

Typing information is transient.

`TypingNotifier` tracks typing participation and notifies other room components.
A short timeout removes stale typing indicators when no further activity is received.

Explicit cleanup also occurs when the user:

- clears the message;
- sends the message;
- leaves the room;
- disconnects.

### RoomCircuitHandler

Blazor itself manages the browser/server circuit.

`RoomCircuitHandler` observes connection up/down events.

When a circuit disconnects, its room participation and typing state are removed.

When the circuit reconnects, the component can restore its presence.

## Component lifecycle

The final room component must handle asynchronous work carefully.

A room can change while a previous database operation is still running.

The component therefore uses a version value to prevent results from an old room load from overwriting newer state.

This illustrates an important difference between simple HTTP handlers and long-lived interactive server components:

state can continue to exist while asynchronous operations overlap.

## Message history and scrolling

The server loads the most recent visible messages.

Messages arriving during history loading are merged by message ID to avoid duplicates.

JavaScript interop is used only where browser information is required, such as scroll position.

New messages scroll the conversation to the bottom only when the user was already near the bottom.

## Authentication

Authentication still uses ASP.NET Core Identity and cookies.

Login and logout are handled as normal HTTP form operations because issuing or deleting an authentication cookie requires an HTTP response.

The interactive chat itself then runs through the Blazor Server circuit.

---

# Final Comparison

## Communication model

| Stage | Receiving new messages |
|---|---|
| Classic PHP | Page/request initiated by user |
| Short Polling | Repeated HTTP requests |
| Long Polling | HTTP request waits for data |
| Raw WebSocket | Persistent bidirectional connection |
| SignalR | Hub methods and server events |
| Blazor WASM | SignalR from browser application |
| Blazor Server | Blazor circuit + in-process coordination |

## Where UI code runs

```text
Classic PHP
    server

PHP + polling
    server rendering + browser JavaScript

Blazor WebAssembly
    browser

Blazor Server
    server
```

## Increasing abstraction

The project can also be viewed as a progression of abstraction:

```text
HTTP request / response
        ↓
polling strategy
        ↓
persistent WebSocket transport
        ↓
SignalR real-time abstraction
        ↓
Blazor application models
```

Each step hides some infrastructure but introduces a different set of architectural concerns.


## Main Lessons

### HTTP is client-driven

Traditional HTTP works naturally when the browser asks for something.

Real-time applications become more difficult when the server must notify the browser without a new user request.

### Polling can simulate real-time behavior

Short Polling and Long Polling show that useful real-time behavior is possible without WebSocket.

The difference is mostly efficiency and latency.

### WebSocket is a transport, not an application architecture

Raw WebSocket provides persistent bidirectional communication, but connection management and application protocols still need to be designed.

### SignalR raises the abstraction level

SignalR allows the application to think in terms of:

- methods;
- users;
- groups;
- events;

instead of frames and connection loops.

### Blazor WASM and Blazor Server solve the same UI problem differently

Blazor WebAssembly runs UI logic in the browser and must communicate explicitly with the server.

Blazor Server runs UI logic on the server and already owns a persistent browser connection.

This changes the architecture significantly even when the visible application behaves almost identically.

### Real-time state and persistent state are different

Messages belong in PostgreSQL.

Online presence and typing indicators do not.

Keeping this distinction clear simplified the final architecture.

---

## Result

The final project is much more than a chat UI.

The same application demonstrates how real-time communication changes as the architecture moves through:

```text
traditional HTTP
→ polling
→ persistent connections
→ SignalR
→ client-side .NET
→ server-side interactive .NET
```

The final Blazor Server version is not necessarily the universal replacement for the previous approaches.

Instead, each stage demonstrates a different set of tradeoffs and explains why real-time web applications can be implemented in several fundamentally different ways.