# PHP Chat — Short Polling

A small PHP and JavaScript chat that demonstrates short polling over HTTP. It evolved from a classic server-rendered chat that used an automatically refreshed `iframe`.

## Features

* predefined chat rooms;
* nickname-based room entry;
* PHP sessions;
* PostgreSQL message history;
* the latest 100 room messages;
* optional message recipient;
* online visitor list;
* presence heartbeat every 5 seconds;
* automatic offline detection after 30 seconds;
* HTML short polling through JavaScript `fetch()`;
* background message submission without page reload;
* recipient selection without page reload;
* immediate message refresh after sending;
* preserved message scroll position during polling;
* prepared SQL statements and escaped HTML output.

## Request Flow

```text
index.php
    → join.php
    → chat.php
        → short-polling.js
            → GET messages.php every 5 seconds
            → POST send.php
        → POST exit.php
```

* `index.php` displays the lobby.
* `join.php` validates the nickname and room, creates a visitor and starts a session.
* `chat.php` displays the selected room, message container and message form.
* `short-polling.js` periodically loads messages and visitors through `fetch()`.
* `messages.php` updates the visitor heartbeat and returns an HTML fragment containing the latest messages and online visitors.
* `send.php` validates and stores a message. Background requests receive `204 No Content`; regular HTML form submissions retain the classic redirect behavior.
* `exit.php` removes the visitor, destroys the session and returns the user to the lobby.

The browser stores only the `PHPSESSID` cookie. The nickname, room ID and visitor ID are stored in the server-side PHP session.

## Short Polling

The browser sends a request to `messages.php` every 5 seconds:

```text
request
    → immediate server response
    → wait 5 seconds
    → next request
```

The endpoint returns server-rendered HTML rather than JSON. JavaScript replaces the message and visitor markup without reloading `chat.php`.

After a message is sent, JavaScript immediately refreshes the chat instead of waiting for the next scheduled polling request.

The chat displays a rolling window containing the latest 100 messages. Older messages are not shown.

## Presence

Every request to `messages.php` updates `room_visitors.last_seen`.

A visitor is considered online when the last heartbeat was received during the previous 30 seconds. Closing a browser tab cannot be detected immediately, so the visitor remains visible until the timeout expires.

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
