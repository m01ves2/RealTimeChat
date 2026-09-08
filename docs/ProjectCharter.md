# Real-Time Chat — Project Charter

## Purpose

Real-Time Chat is a learning project about the evolution of communication between a browser and a server: from traditional HTTP-based PHP applications to real-time .NET applications.

The same chat problem will be implemented in several ways. Each stage should reveal the limitations of the previous approach and explain why the next architectural step is needed.

## Learning Goals

The project should provide practical understanding of:

- the HTTP request-response model;
- asynchronous browser requests, polling, and long polling;
- persistent connections and real-time communication;
- SignalR, connection state, and reconnection;
- the differences between Blazor Server and Blazor WebAssembly;
- storing chat history in PostgreSQL;
- deploying web applications behind nginx.

## Final MVP

In the final version, a user will be able to:

- enter a display name without permanent registration;
- see several predefined rooms and switch between them;
- load message history stored in the database;
- send and receive text messages immediately;
- see the users who are online in the current room;
- see a temporary `Ivan is typing...` indicator;
- see the current connection state;
- reconnect automatically after a temporary connection failure;
- use the application on desktop and mobile screens.

The final project will include both Blazor Server and Blazor WebAssembly clients and will be deployed on an Ubuntu server behind nginx.

Intermediate implementations may support only the features required to demonstrate their communication approach. Early PHP versions will intentionally use a minimal HTML, CSS, and JavaScript interface.

## Out of Scope

The project will not include:

- permanent accounts, passwords, roles, or an administration panel;
- private messages or user-created rooms;
- editing, deleting, searching, or reacting to messages;
- replies, threads, mentions, or read receipts;
- file, image, video, or voice-message uploads;
- push notifications;
- microservices or production-scale horizontal scaling.

## Constraints

- The project will use a monorepo for its PHP, .NET, documentation, and deployment parts.
- The .NET applications will use .NET 10.
- Rooms will be predefined and messages will contain plain text only.
- Intermediate versions should remain small and focused on one communication approach.
- New technology should be introduced only when it solves a visible limitation.

## Completion Criteria

The project is complete when:

- the planned communication approaches have been implemented and compared;
- the final clients support the agreed MVP;
- message history survives application restarts;
- presence, typing notifications, connection state, and reconnection can be demonstrated;
- the application works on desktop and mobile screens;
- the system is deployed behind nginx and its architecture is documented.
