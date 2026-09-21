# .NET Real-Time Chat

The .NET application evolves through two stages:

1. a low-level Raw WebSocket broadcast prototype;
2. a complete SignalR-based chat application.

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
The complete chat application is implemented with SignalR.

## SignalR Chat

The current application uses SignalR for real-time communication.

SignalR replaces the custom connection lifecycle, receive and send loops,
outgoing channels, connection manager, and manual broadcasting.