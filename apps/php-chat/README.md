# PHP Chat — Long Polling

A small PHP and JavaScript chat that demonstrates long polling over HTTP. It evolved from the short polling version of the chat.

## Features

* predefined chat rooms;
* nickname-based room entry;
* PHP sessions;
* PostgreSQL message history;
* the latest 100 room messages;
* optional message recipient;
* online visitor list;
* presence heartbeat through long polling requests;
* HTML long polling through JavaScript `fetch()`;
* background message submission without page reload;
* recipient selection without page reload;
* automatic refresh when a long polling request detects a new message;
* preserved scroll position while reading message history;
* automatic scrolling to the newest messages when the user is already near the bottom;
* automatic offline detection after 30 seconds;
* background message submission without page reload;
* recipient selection without page reload;


* prepared SQL statements and escaped HTML output.

## Request Flow

```text
index.php
    → join.php
    → chat.php
        → long-polling.js
            → GET messages.php?after=<lastMessageId>
            → POST send.php
        → POST exit.php
```

* `index.php` displays the lobby.
* `join.php` validates the nickname and room, creates a visitor and starts a session.
* `chat.php` displays the selected room, message container and message form.
* `long-polling.js` keeps one message request active at a time and immediately starts the next request after the previous one finishes.
* `messages.php` updates the visitor heartbeat, waits for a new message or timeout, and returns an HTML fragment containing the latest messages and online visitors.
* `send.php` validates and stores a message. Background requests receive `204 No Content`; regular HTML form submissions retain the classic redirect behavior.
* `exit.php` removes the visitor, destroys the session and returns the user to the lobby.

The browser stores only the `PHPSESSID` cookie. The nickname, room ID and visitor ID are stored in the server-side PHP session.

## Long Polling

The first request to `messages.php` returns immediately and loads the current chat state.

JavaScript remembers the ID of the newest message and sends it with the next request:

```text
GET /messages.php?after=123
The server keeps this request open until either:
    → a newer message appears; or
    → the long polling timeout expires.

request
    → server waits
    → new message or timeout
    → response
    → next request starts immediately
```

The current implementation checks PostgreSQL every 500 ms while waiting and uses a 20-second timeout.

The endpoint still returns a full server-rendered HTML snapshot containing the latest 100 messages and the online visitor list. JavaScript replaces the previous snapshot rather than appending only the new messages.

This approach intentionally keeps the implementation close to the previous short polling version.

## Presence

Each request to `messages.php` updates `room_visitors.last_seen` before entering the long polling wait.

The long polling timeout is shorter than the 30-second presence timeout, so an active client refreshes its heartbeat before being considered offline.

## Trade-offs

Long polling provides near-immediate message delivery without sending a new HTTP request every few seconds.

However, each connected client keeps a long-running HTTP request open. In this implementation, the PHP script periodically queries PostgreSQL while waiting for new messages. This is intentionally simple and educational rather than optimized for large-scale production use.

## Requirements

* nginx;
* PHP-FPM 8.3 or later;
* PHP extensions: `pdo_pgsql` and `mbstring`;
* PostgreSQL 16 or later.

The nginx document root must point to:

```text
apps/php-chat/public
```

## Database Setup

Create a PostgreSQL database and application user, then apply the migrations:

```bash
psql \
    --set=ON_ERROR_STOP=1 \
    --single-transaction \
    -h localhost \
    -U realtime_chat_app \
    -d realtime_chat \
    -f apps/php-chat/database/001_initial.sql

psql \
    --set=ON_ERROR_STOP=1 \
    --single-transaction \
    -h localhost \
    -U realtime_chat_app \
    -d realtime_chat \
    -f apps/php-chat/database/002_add_visitors_and_recipients.sql
```

Create the local configuration:

```bash
cp apps/php-chat/config/database.example.php \
   apps/php-chat/config/database.local.php
```

Set the PostgreSQL connection values in `database.local.php`. This file contains local credentials and is ignored by Git.

Open the lobby at:

```text
http://localhost/
```

## Debian Package

Build the package:

```bash
apps/php-chat/packaging/build-deb.sh
```

Install it on Ubuntu or Debian:

```bash
sudo apt install ./apps/php-chat/dist/realtime-chat-php_1.0.0_all.deb
```

Open:

```text
http://SERVER_IP:8080/
```

Remove the application while preserving its configuration and database:

```bash
sudo apt remove realtime-chat-php
```

Remove the application and generated configuration:

```bash
sudo apt purge realtime-chat-php
```

The PostgreSQL database and role are preserved to prevent accidental data loss.
