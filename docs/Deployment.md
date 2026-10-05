# Deployment

This document describes how to deploy the final .NET version of Real-Time Chat to Ubuntu 24.04.

The deployed application contains:

- Blazor Server UI
- ASP.NET Core Identity
- PostgreSQL
- SignalR / Blazor Server real-time connections
- nginx reverse proxy
- systemd service

The application is deployed as a single .NET process.

## 1. Server layout

Current deployment:

```text
Browser
   |
   | http://<SERVER_IP>:8081
   v
nginx :8081
   |
   | reverse proxy
   | HTTP + WebSocket
   v
Kestrel
127.0.0.1:5010
   |
   v
RealTimeChat.BlazorServer
   |
   v
PostgreSQL
127.0.0.1:5432
```

Ports used by other projects are left unchanged.

Current ports:

```text
80    -> BikeShop through nginx
8080  -> PHP Real-Time Chat through nginx
8081  -> .NET Real-Time Chat through nginx

5000  -> BikeShop Blazor Server
5001  -> BikeShop API
5010  -> Real-Time Chat Kestrel

5432  -> PostgreSQL
```

## 2. Requirements

Server:

- Ubuntu 24.04
- .NET 10 ASP.NET Core Runtime
- PostgreSQL 16
- nginx
- systemd

Useful checks:

```bash
lsb_release -a

dotnet --list-sdks
dotnet --list-runtimes

psql --version

nginx -v
```

Check listening ports:

```bash
sudo ss -lntp
```

## 3. PostgreSQL

### Create application role

Open PostgreSQL as administrator:

```bash
sudo -u postgres psql
```

Create a separate PostgreSQL user:

```sql
CREATE ROLE realtime_chat_dotnet_app
WITH LOGIN
PASSWORD '<PASSWORD>';
```

### Create database

```sql
CREATE DATABASE realtime_chat_dotnet
OWNER realtime_chat_dotnet_app;
```

Check roles and databases:

```sql
\du
\l
```

Exit:

```sql
\q
```

### Test connection

```bash
psql \
  -h 127.0.0.1 \
  -U realtime_chat_dotnet_app \
  -d realtime_chat_dotnet
```

Inside PostgreSQL:

```sql
\conninfo
```

Before migrations are applied, this should show no application tables:

```sql
\dt
```

## 4. Generate database deployment script

EF Core migrations are stored in:

```text
apps/dotnet-chat/src/RealTimeChat.Infrastructure
```

The startup project is:

```text
apps/dotnet-chat/src/RealTimeChat.BlazorServer
```

`RealTimeChat.BlazorServer` references:

```text
Microsoft.EntityFrameworkCore.Design
```

This package is required by EF Core command-line tools.

Check migrations:

```bash
dotnet ef migrations list \
  --project apps/dotnet-chat/src/RealTimeChat.Infrastructure \
  --startup-project apps/dotnet-chat/src/RealTimeChat.BlazorServer
```

Generate an idempotent SQL script:

```bash
dotnet ef migrations script \
  --project apps/dotnet-chat/src/RealTimeChat.Infrastructure \
  --startup-project apps/dotnet-chat/src/RealTimeChat.BlazorServer \
  --idempotent \
  --output deploy/deploy.sql
```

`--idempotent` makes the script check `__EFMigrationsHistory` before applying migrations.

## 5. Apply database migrations

Copy the script to the server:

```bash
scp deploy/deploy.sql \
  <SERVER_USER>@<SERVER_IP>:~/
```

Apply it using the application PostgreSQL user:

```bash
psql \
  -h 127.0.0.1 \
  -U realtime_chat_dotnet_app \
  -d realtime_chat_dotnet \
  -f ~/deploy.sql
```

Check tables:

```bash
psql \
  -h 127.0.0.1 \
  -U realtime_chat_dotnet_app \
  -d realtime_chat_dotnet
```

Then:

```sql
\dt
```

Expected tables include:

```text
AspNetUserClaims
AspNetUserLogins
AspNetUserTokens
AspNetUsers
ChatMessages
Rooms
__EFMigrationsHistory
```

Check applied migrations:

```sql
SELECT * FROM "__EFMigrationsHistory";
```

PostgreSQL identifiers created by EF Core use PascalCase and therefore must be quoted:

```sql
SELECT * FROM "Rooms";
SELECT * FROM "ChatMessages";
SELECT * FROM "AspNetUsers";
```

Without quotes PostgreSQL converts identifiers to lowercase.

## 6. Production configuration

During local development the connection string is stored in User Secrets:

```text
ConnectionStrings:DefaultConnection
```

User Secrets are not included in `dotnet publish`.

On the server, production configuration is supplied through systemd environment variables.

ASP.NET Core converts:

```text
ConnectionStrings__DefaultConnection
```

to:

```text
ConnectionStrings:DefaultConnection
```

The application reads it using:

```csharp
builder.Configuration.GetConnectionString("DefaultConnection")
```

Production connection string:

```text
Host=127.0.0.1;
Port=5432;
Database=realtime_chat_dotnet;
Username=realtime_chat_dotnet_app;
Password=<PASSWORD>
```

Do not store the production password in Git.

## 7. Publish application

From the repository root:

```bash
dotnet publish \
  apps/dotnet-chat/src/RealTimeChat.BlazorServer \
  -c Release \
  -o publish/realtime-chat
```

`dotnet publish` builds the application automatically.

A separate Visual Studio build is not required.

The publish directory contains the complete deployable application, including:

```text
RealTimeChat.BlazorServer.dll
appsettings.json
wwwroot/
*.dll
```

## 8. Create Linux service user

Create a dedicated system user:

```bash
sudo useradd \
  --system \
  --no-create-home \
  --shell /usr/sbin/nologin \
  realtimechat
```

Check:

```bash
id realtimechat
```

Create application directory:

```bash
sudo mkdir -p /opt/realtime-chat
sudo chown -R realtimechat:realtimechat /opt/realtime-chat
```

The application should not run as `root`.

## 9. Data Protection keys

ASP.NET Core Identity cookies depend on Data Protection keys.

The keys should survive application redeployment.

Create a persistent directory:

```bash
sudo mkdir -p /var/lib/realtime-chat/dataprotection
sudo chown -R realtimechat:realtimechat /var/lib/realtime-chat
```

The application reads the key directory from:

```text
DataProtection:KeysPath
```

On Linux it is configured as:

```text
/var/lib/realtime-chat/dataprotection
```

The application configures Data Protection only when this setting is present.

This allows Windows development to continue using the default local key storage.

## 10. Copy publish files to server

Do not copy directly to `/opt/realtime-chat`.

That directory belongs to the `realtimechat` system user.

Copy the files first to the normal user's home directory:

```bash
scp -r publish/realtime-chat \
  <SERVER_USER>@<SERVER_IP>:~/
```

Then on Ubuntu:

```bash
sudo systemctl stop realtime-chat
```

Replace the deployed application:

```bash
sudo rm -rf /opt/realtime-chat/*
sudo cp -a ~/realtime-chat/. /opt/realtime-chat/
```

Restore ownership:

```bash
sudo chown -R realtimechat:realtimechat /opt/realtime-chat
```

## 11. systemd service

Create:

```text
/etc/systemd/system/realtime-chat.service
```

Example configuration:

```ini
[Unit]
Description=RealTimeChat Blazor Server
After=network.target postgresql.service

[Service]
User=realtimechat
Group=realtimechat

WorkingDirectory=/opt/realtime-chat

ExecStart=/usr/bin/dotnet /opt/realtime-chat/RealTimeChat.BlazorServer.dll

Restart=always
RestartSec=5

SyslogIdentifier=realtime-chat

Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5010

Environment=ConnectionStrings__DefaultConnection=Host=127.0.0.1;Port=5432;Database=realtime_chat_dotnet;Username=realtime_chat_dotnet_app;Password=<PASSWORD>

Environment=DataProtection__KeysPath=/var/lib/realtime-chat/dataprotection

[Install]
WantedBy=multi-user.target
```

After creating or modifying the unit file:

```bash
sudo systemctl daemon-reload
```

Start:

```bash
sudo systemctl start realtime-chat
```

Enable automatic startup:

```bash
sudo systemctl enable realtime-chat
```

Check status:

```bash
systemctl status realtime-chat --no-pager
```

Check process owner:

```bash
ps -o user:20,pid,cmd -C dotnet
```

Real-Time Chat should run as:

```text
realtimechat
```

Check Kestrel:

```bash
sudo ss -lntp | grep 5010
```

Expected:

```text
127.0.0.1:5010
```

The Kestrel port is accessible only locally on the server.

## 12. systemd logs

View recent logs:

```bash
journalctl -u realtime-chat -n 50 --no-pager
```

Follow logs:

```bash
journalctl -u realtime-chat -f
```

## 13. Test Kestrel directly

From the Ubuntu server:

```bash
curl -i http://127.0.0.1:5010/
```

An unauthenticated request should return:

```text
HTTP/1.1 302 Found
Location: http://127.0.0.1:5010/login?ReturnUrl=%2F
```

This is expected.

The root page requires authentication, so cookie authentication redirects the client to `/login`.

Follow redirects:

```bash
curl -L -i http://127.0.0.1:5010/
```

## 14. nginx reverse proxy

Create:

```text
/etc/nginx/sites-available/realtime-chat-dotnet
```

Configuration:

```nginx
server {
    listen 8081;
    listen [::]:8081;

    server_name _;

    location / {
        proxy_pass http://127.0.0.1:5010;
        proxy_http_version 1.1;

        proxy_set_header Host $http_host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "Upgrade";

        proxy_buffering off;
    }
}
```

Enable the site:

```bash
sudo ln -s \
  /etc/nginx/sites-available/realtime-chat-dotnet \
  /etc/nginx/sites-enabled/realtime-chat-dotnet
```

Validate nginx configuration:

```bash
sudo nginx -t
```

Reload nginx:

```bash
sudo systemctl reload nginx
```

Check external port:

```bash
sudo ss -lntp | grep 8081
```

## 15. Important nginx details

### Preserve external port

Use:

```nginx
proxy_set_header Host $http_host;
```

not:

```nginx
proxy_set_header Host $host;
```

The application is exposed on non-standard HTTP port `8081`.

`$http_host` preserves:

```text
<SERVER_IP>:8081
```

If `$host` is used, the port may be lost.

ASP.NET Core could then generate a redirect like:

```text
http://<SERVER_IP>/login
```

The browser would use port `80`, which currently belongs to BikeShop.

Correct redirect:

```text
http://<SERVER_IP>:8081/login
```

### WebSocket support

These headers are required for Blazor Server and SignalR/WebSocket connections:

```nginx
proxy_set_header Upgrade $http_upgrade;
proxy_set_header Connection "Upgrade";
```

## 16. Browser test

Open:

```text
http://<SERVER_IP>:8081/
```

For the current server:

```text
http://192.168.1.102:8081/
```

Test:

1. Open `/register`.
2. Create a new account.
3. Sign in.
4. Open a room.
5. Open the application in another browser/session.
6. Register or sign in as another user.
7. Join the same room.
8. Send public messages.
9. Send private messages.
10. Check online users.
11. Check typing indicator.
12. Check reconnect behavior.

## 17. Updating the application

After changing C# code:

```text
change source
    ↓
dotnet publish
    ↓
copy publish to server
    ↓
replace /opt/realtime-chat files
    ↓
restore ownership
    ↓
restart service
```

Publish:

```bash
dotnet publish \
  apps/dotnet-chat/src/RealTimeChat.BlazorServer \
  -c Release \
  -o publish/realtime-chat
```

Copy:

```bash
scp -r publish/realtime-chat \
  <SERVER_USER>@<SERVER_IP>:~/
```

Deploy:

```bash
sudo systemctl stop realtime-chat

sudo rm -rf /opt/realtime-chat/*
sudo cp -a ~/realtime-chat/. /opt/realtime-chat/

sudo chown -R realtimechat:realtimechat /opt/realtime-chat

sudo systemctl start realtime-chat
```

Check:

```bash
systemctl status realtime-chat --no-pager
```

Changing application files does not require:

```bash
systemctl daemon-reload
```

`daemon-reload` is required only when the systemd unit file itself changes.

## 18. Updating the database

When new EF Core migrations are added:

1. Generate a new idempotent SQL script.
2. Copy it to the server.
3. Apply it to `realtime_chat_dotnet`.
4. Verify `__EFMigrationsHistory`.
5. Deploy the new application version.

Generate:

```bash
dotnet ef migrations script \
  --project apps/dotnet-chat/src/RealTimeChat.Infrastructure \
  --startup-project apps/dotnet-chat/src/RealTimeChat.BlazorServer \
  --idempotent \
  --output deploy/deploy.sql
```

Apply:

```bash
psql \
  -h 127.0.0.1 \
  -U realtime_chat_dotnet_app \
  -d realtime_chat_dotnet \
  -f ~/deploy.sql
```

## 19. Useful diagnostic commands

Application:

```bash
systemctl status realtime-chat --no-pager
journalctl -u realtime-chat -n 50 --no-pager
```

Ports:

```bash
sudo ss -lntp
```

nginx:

```bash
sudo nginx -t
sudo nginx -T
```

PostgreSQL:

```bash
systemctl status postgresql --no-pager
```

nginx status:

```bash
systemctl status nginx --no-pager
```

Database:

```bash
psql \
  -h 127.0.0.1 \
  -U realtime_chat_dotnet_app \
  -d realtime_chat_dotnet
```

Inside PostgreSQL:

```sql
\dt

SELECT * FROM "__EFMigrationsHistory";
SELECT * FROM "Rooms";
SELECT * FROM "ChatMessages";
SELECT * FROM "AspNetUsers";
```

## 20. Final deployed architecture

```text
Client browser
      |
      | HTTP :8081
      | WebSocket
      v
    nginx
      |
      | reverse proxy
      v
Kestrel :5010
      |
      v
RealTimeChat.BlazorServer
      |
      +-- Blazor Server UI
      +-- ASP.NET Core Identity
      +-- application services
      +-- real-time notifications
      |
      v
PostgreSQL :5432
```

The application is managed by systemd and runs under the dedicated `realtimechat` Linux user.
