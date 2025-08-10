### Typing Race — Real-time Typing Competition

**Live demo:** local development only (see Installation)

A small real-time typing competition platform (Typeracer-like) built with:

- **Frontend:** Next.js (React, client components), Tailwind CSS, Heroui components
- **Realtime:** SignalR (ASP.NET Core Hub)
- **Backend:** ASP.NET Core (C#) — in-memory rooms & players, paragraph generator

This repository contains a working minimal implementation and a clear roadmap for features you can add. The goal is to be fun to play and easy to iterate on.

---

## Quick features (implemented)

- Real-time lobby with players joining a room (by invite code)
- Live leaderboard with per-player `score` (correct words) and `precision` (accuracy %)
- Live per-player typing progress updates (via SignalR events)
- Host mechanics (the first player in a room becomes host and can start the game)
- Simple paragraph generation with fallback to local Lorem Ipsum
- Clean, client-first React UI with `useGameHub` hook and LeaderBoardCard
- Basic input handling and server-side scoring logic

---

## Suggested improvements

- Persistence for rooms & results (Redis / Postgres / JSON files)
- Room cleanup for long inactivity
- Authentication (JWT / OAuth) and persistent player profiles
- Paragraph generation via LLM for more interesting text
- Multiple rounds, configurable round duration
- Better typing UI: highlight errors, caret animation, per-character scoring
- Client-side WPM estimation & richer stats (streaks, best WPM)
- Docker & production-ready deployments
- Left the game button

---

## Prerequisites

- Node.js 18+ and npm (or pnpm/yarn)
- .NET SDK 7+ (for backend)
- Optional: `concurrently` is used in monorepo dev script (it's included as a devDependency)

---

## Repository layout (expected)

```
/ (repo root)
  package.json           # dev script orchestrator (concurrently)
  apps/
    frontend/            # Next.js frontend app
    backend/             # ASP.NET Core Web API + SignalR hub
```

---

## Environment variables

Frontend expects:

```
NEXT_PUBLIC_WEBSOCKET_URL=http://localhost:5000/
```

Important: DO NOT include `gamehub` in the value — the client app will append `gamehub` when building the SignalR URL. If you use HTTPS, point to `https://...` accordingly.

Backend uses normal ASP.NET Core configuration. By default, `dotnet watch run` will host on `http://localhost:5000` and `https://localhost:5001` (Kestrel). If you change ports, update `NEXT_PUBLIC_WEBSOCKET_URL`.

---

## Development (local)

1. Install repository dependencies

```bash
npm run install
```

This runs `cd apps/frontend && npm install` for frontend and you also need .NET to be installed for the backend.

2. Start both apps (dev)

```bash
npm run dev
```

This uses the `concurrently` script configured in `package.json`:

- `dev-frontend` runs the Next.js dev server (e.g. [http://localhost:3000](http://localhost:3000))
- `dev-backend` runs the ASP.NET backend with `dotnet watch run` (e.g. [http://localhost:5000](http://localhost:5000))

3. Open the front-end in your browser (usually `http://localhost:3000`). Create a room code and pick a nickname. Open multiple browser windows/incognito to simulate players.

---

## Backend SignalR Hub — contract

### Hub methods (called by client)

- `JoinGame(roomId: string, name: string)` — add a player to the specified room. Returns events to client(s).
- `StartGame()` — host-only. Starts a round (generates paragraph, sets timer, broadcasts `game-started`).
- `PlayerTyped(typedText: string)` — client sends current typed content to server; server computes score & precision and broadcasts `player-score` updates.

### Server events (emitted)

- `player-joined` — payload `{ id, name, score, precision }`
- `players` — current players list
- `player-left` — player id
- `player-score` — `{ id, score, precision }` per-player live update
- `game-started` — paragraph string
- `game-finished` — round finished
- `new-host` — connection id of new host
- `game-created`, `game-ended`, and `error`

These are already implemented in the backend hub; the frontend `useGameHub` hook consumes them.

---

## Data model (current)

- `Game` (room): `RoomId`, `HostConnectionId`, `GameStatus`, `Paragraph`, `ParagraphWords[]`, `Players` (ConcurrentDictionary), `GameTimer`
- `Player`: `ConnectionId`, `Name`, `Score`, `Precision`

---

## How scoring and metrics work

- **Score (words):** Server splits typed text and counts how many words from the start match the target paragraph words (case-insensitive). Words after the first mismatch are not counted.
- **Precision (accuracy %):** Number of matching characters (case-insensitive) between the typed text and the same-length prefix of the paragraph, divided by typed characters \* 100.
- **Completion detection:** If the player typed all words correctly and typed length >= paragraph length, server marks them finished and ends the round early.

---
