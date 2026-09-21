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