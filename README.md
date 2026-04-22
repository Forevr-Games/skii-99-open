# Ski99 — Reddit Devvit + Unity WebGL Game

A skiing game built with **Unity 6.2** and deployed on Reddit using the **[Devvit Web](https://developers.reddit.com/docs/webview) framework**. This repository serves as an open-source reference project demonstrating best practices for integrating Unity WebGL games with Reddit via Devvit.

## What This Demonstrates

| Feature | Where to look |
|---|---|
| Devvit web server setup (Express + Reddit APIs) | `devvit-ski/src/server/index.ts` |
| Reddit user context, snoovatar fetch | `devvit-ski/src/server/index.ts` `/api/init` |
| Redis sorted-set leaderboard | `devvit-ski/src/server/index.ts` `/api/daily-game-completed` |
| Creating custom Reddit posts from a game | `devvit-ski/src/server/core/post.ts` |
| Submitting comments from a game | `devvit-ski/src/server/core/comment.ts` |
| Opening URLs from Unity WebGL (sandboxed) | `devvit-ski/src/server/index.ts` `/api/open-url` |
| Unity ↔ Devvit HTTP bridge (C#) | `Ski_Unity_Project/Assets/Scripts/Devvit/Runtime/` |
| Mock service for editor testing | `Ski_Unity_Project/Assets/Scripts/Devvit/Runtime/DevvitServiceMock.cs` + `Ski_Unity_Project/Assets/Scripts/Devvit/Editor/DevvitMockConfigWindow.cs` |
| Challenge post / viral loop pattern | `Assets/Scripts/SaveDataManager.cs` |

---

## Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│  Reddit Post (browser)                                           │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  Devvit Client (src/client/script.ts)                   │    │
│  │  - Loads Unity WebGL build into an <iframe>             │    │
│  │  - Provides Devvit context (postId, postData)           │    │
│  └───────────────┬─────────────────────────────────────────┘    │
│                  │  HTTP (same-origin, relative URLs)            │
│  ┌───────────────▼─────────────────────────────────────────┐    │
│  │  Devvit Web Server (src/server/index.ts)                │    │
│  │  - Express routes: /api/init, /api/leaderboard, etc.    │    │
│  │  - @devvit/web/server: context, reddit, redis           │    │
│  └──────────┬─────────────────────┬───────────────────────┘    │
│             │                     │                              │
│  ┌──────────▼──────────┐  ┌──────▼──────────────────────────┐  │
│  │  Reddit API         │  │  Redis                          │  │
│  │  - getCurrentUser   │  │  - Leaderboard (sorted set)     │  │
│  │  - getSnoovatarUrl  │  │  - Per-user scores              │  │
│  │  - submitCustomPost │  │  - Level completion data        │  │
│  │  - submitComment    │  └─────────────────────────────────┘  │
│  └─────────────────────┘                                        │
└──────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│  Unity WebGL Game (Ski_Unity_Project/)                          │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  Game Logic (SkiGameManager, SkiPlayerController, etc.)   │  │
│  └────────────────────────┬──────────────────────────────────┘  │
│                           │                                      │
│  ┌────────────────────────▼──────────────────────────────────┐  │
│  │  Devvit Service Layer (Assets/Scripts/Devvit/Runtime/)    │  │
│  │  IDevvitService ──► DevvitServiceBuild  (WebGL/HTTP)      │  │
│  │               └──► DevvitServiceMock   (Editor/Testing)   │  │
│  └───────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────┘
```

**Key insight:** Unity WebGL runs inside the same origin as the Devvit server, so `UnityWebRequest.Get("/api/init")` works without any cross-origin configuration.

---

## Repository Structure

```
Ski99/
├── README.md                   ← You are here
├── CONTRIBUTING.md
│
├── devvit-ski/                 ← Devvit app (TypeScript)
│   ├── devvit.json             ← App config: entrypoints, menu items, permissions
│   ├── package.json
│   ├── src/
│   │   ├── client/
│   │   │   └── script.ts       ← Loads Unity into the Reddit post iframe
│   │   ├── server/
│   │   │   ├── index.ts        ← Main Express server: all API routes
│   │   │   ├── core/
│   │   │   │   ├── post.ts     ← reddit.submitCustomPost() helper
│   │   │   │   └── comment.ts  ← reddit.submitComment() helper
│   │   │   └── menu/           ← Devvit menu action examples (see menu/README.md)
│   │   └── shared/
│   │       └── types/api.ts    ← Shared TypeScript types (mirrors C# DevvitTypes.cs)
│
└── Ski_Unity_Project/          ← Unity 6.2 project
    └── Assets/Scripts/
        ├── Devvit/             ← All Reddit integration code lives here
        │   ├── Runtime/
        │   │   ├── IDevvitService.cs          ← Interface: all Reddit operations
        │   │   ├── DevvitService.cs           ← Factory: picks Mock vs Build
        │   │   ├── DevvitServiceBuild.cs      ← Real implementation (HTTP)
        │   │   ├── DevvitServiceMock.cs       ← Editor mock (no network needed)
        │   │   ├── DevvitTypes.cs             ← C# mirrors of shared/types/api.ts
        │   │   ├── DevvitPostData.cs          ← Custom post data structure
        │   │   ├── RedditLeaderboard.cs       ← MonoBehaviour HTTP worker
        │   │   ├── DevvitUserProfileUI.cs     ← Avatar display UI
        │   │   └── AvatarLoader.cs            ← Downloads snoovatar textures
        │   └── Editor/
        │       ├── DevvitMockConfig.cs        ← EditorPrefs-backed mock config
        │       └── DevvitMockConfigWindow.cs  ← Editor window for mock settings
        ├── SaveDataManager.cs  ← Bridges Devvit service with game logic
        └── ...                 ← Game scripts (SkiGameManager, etc.)
```

---

## Prerequisites

- **Unity 6.2** (6000.2.8f1) — [Download via Unity Hub](https://unity.com/download)
- **Node.js 18+**
- **Devvit CLI** — `npm install -g devvit`
- A Reddit account with access to a test subreddit

---

## Quick Start

### Run in Unity Editor (no Reddit account needed)

1. Open `Ski_Unity_Project/` via Unity Hub
2. Open the scene at `Assets/Scenes/Ski.unity`
3. Press **Play** — the game runs using mock data (no network required)
4. To configure mock data: **Reddit > Devvit Mock Config** in the Unity menu bar

The mock service (`DevvitServiceMock.cs`) simulates all API calls with configurable fake data and artificial network delay.

### Deploy to Reddit

See [devvit-ski/README.md](devvit-ski/README.md) for full deployment instructions.

**Short version:**
```bash
cd devvit-ski
npm install
npm run dev        # start local dev server
```

Then build Unity for WebGL and copy the output files into `devvit-ski/src/client/public/Build/`. Full steps are in the Devvit README.

---

## How the Game Uses Reddit

### Initialization
When a user opens the Reddit post, the game immediately calls `GET /api/init`. The Devvit server returns:
- **Username** — shown on the leaderboard and used for score attribution
- **Snoovatar URL** — the user's Reddit avatar, fetched server-side via `reddit.getUserById()`
- **Previous score** — retrieved from Redis using `postId:username` as the key
- **Post data** — arbitrary JSON embedded in the post when it was created (used for challenge posts)

### Leaderboard
Scores are stored in Redis sorted sets keyed by `leaderboard:postId`. Redis sorted sets natively support:
- Inserting/updating a score: `ZADD`
- Fetching top N scores in order: `ZRANGE ... REV`
- Getting a user's rank: `ZRANK`

### Challenge Post / Viral Loop
When a player gets a high score, they can "share" it. This:
1. Creates a new custom Reddit post (`reddit.submitCustomPost`) with the player's score embedded in `postData`
2. Posts a stickied comment (as the app account) with the formatted score for challengers to reply to

When another user opens that challenge post, the game reads the `postData` to display the original player's score as a target. If the challenger beats it, they comment on the stickied comment with their result.

---

## Key Devvit Concepts Demonstrated

**`context` object** — Devvit automatically injects per-request context into every server handler. Contains `postId`, `userId`, `subredditName`, and `postData`. No auth tokens or session management needed.

**`runAs: "USER"` vs `"APP"`** — Posts and comments can be submitted either as the logged-in user (requires Reddit's `UserGeneratedContent` policy) or as the app's service account.

**`postData`** — Custom posts can embed arbitrary JSON when created. This data is available in `context.postData` on every subsequent request from that post. Used here to pass challenge data between players.

**`navigateTo`** — Returning `{ navigateTo: url }` from any API handler triggers the Devvit client to open that URL. Used as a workaround since `Application.OpenURL` is blocked in the WebGL sandbox.

**`/internal/on-app-install`** — A Devvit lifecycle hook that fires when the app is installed on a subreddit. Used to automatically create the first game post.

---

## Exporting Unity for Devvit

The project must be built **twice** due to a Unity WebGL compression quirk:

1. In Unity, go to **File > Build Profiles**, switch platform to **Web**
2. Open **Player Settings > Publishing Settings**, enable **Decompression Fallback**
3. First build — set **Compression Format** to **GZip**, then Build:
   - Copy `Ski-99.data.unityweb` and `Ski-99.wasm.unityweb` → `devvit-ski/src/client/public/Build/`
4. Second build — set **Compression Format** to **Disabled**, then Build:
   - Copy `Ski-99.framework.js` → `devvit-ski/src/client/public/Build/`
5. Run `npm run dev` in `devvit-ski/` to test on Reddit

The two-build process is needed because `.unityweb` files require GZip compression to reduce download size, but the framework JS file must be uncompressed to load synchronously.

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

---

## License

**Code** (all `.cs`, `.ts`, `.js`, `.json`, `.html`, `.css`, and related source files) is licensed under the [MIT License](LICENSE). Copyright (c) 2026 ForeVR Games.

**Game assets** (art, audio, models, textures, animations, and other non-code content) are licensed under the [Asset License – Game Use Only](LICENSE-ASSETS). You may use the assets in your own games but may not redistribute them as standalone assets.
