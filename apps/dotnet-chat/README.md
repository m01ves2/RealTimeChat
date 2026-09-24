# .NET Real-Time Chat

The .NET application evolves through three stages:

1. a low-level Raw WebSocket broadcast prototype;
2. a minimal SignalR broadcast prototype;
3. a complete SignalR-based chat application.

## Raw WebSocket Prototype

The first stage was created to explore the low-level mechanics behind
persistent bidirectional connections.

It implemented:

- HTTP Upgrade to WebSocket;
- independent receive and send loops;
- fragmented text message assembly;
- per-connection outgoing channels;
- coordinated cancellation;
- connection tracking;
- broadcast messaging;
- WebSocket close handshake.

The prototype was intentionally limited to message broadcasting.

## SignalR Broadcast Prototype

The second stage demonstrates the basic SignalR communication model.

It implements:

- a minimal SignalR Hub;
- client-to-server Hub method invocation;
- server-to-client message handlers;
- broadcast messaging;
- manual connection and disconnection;
- connection state logging;
- a JavaScript test client.

SignalR replaces the custom connection lifecycle, receive and send loops,
outgoing channels, connection manager, and manual broadcasting used by
the Raw WebSocket prototype.

## SignalR Chat Application

The complete chat application will be built on SignalR.

It will include chat rooms, nickname-based entry, PostgreSQL message
history, online users, typing indicators, connection state handling,
automatic reconnection, and Blazor clients.

## Manual authentication check

We can use `curl` or DevTools to check manually server authentification. 
Server is meant to be run on 5065 port, curl is used on the same machine.

### authentification testing by `curl`
- register:
curl -i -sS \
  -H 'Content-Type: application/json' \
  -d '{"userName":"ChatTester","password":"TestPass123!"}' \
  http://localhost:5065/api/auth/register

- login
curl -i -sS -c /tmp/chat-cookies.txt \
  -H 'Content-Type: application/json' \
  -d '{ "userName":"ChatTester","password":"TestPass123!"}' \
  http://localhost:5065/api/auth/login

- wrong password
curl -i -sS \
  -H 'Content-Type: application/json' \
  -d '{"userName":"ChatTester","password":"TestPass123!}' \
  http://localhost:5065/api/auth/login

- logout
curl -i -sS -b / tmp / chat-cookies.txt -c /tmp/chat-cookies.txt \
  -X POST http://localhost:5065/api/auth/logout


### authentification testing by DevTools
- Start `RealTimeChat.Server` and open its `/` page in the browser.
- Run this in DevTools Console on that page:

  await fetch("/api/auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
        userName: "ChatTester",
        password: "TestPass123!"
    })
}).then(async r => ({ status: r.status, body: await r.text() }));

- Then check `/api/rooms`, `/api/rooms/2`, and `/api/rooms/999`
in the same browser.