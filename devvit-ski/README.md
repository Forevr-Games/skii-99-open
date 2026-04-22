# devvit-ski — Devvit Web Server

The TypeScript server and client for Ski99, built on Reddit's [Devvit Web](https://developers.reddit.com/docs/webview) framework.

For the full project overview, see the [root README](../README.md).

---

## Prerequisites

- **Node.js 18+** (Node 22 recommended)
- **Devvit CLI**: `npm install -g devvit`
- A Reddit account connected to [Reddit Developers](https://developers.reddit.com/)

---

## Getting Started

```bash
npm install
devvit login          # authenticate with your Reddit account
npm run dev           # start local dev server
```

**Before running:** edit `devvit.json` and change `dev.subreddit` to a subreddit you moderate or have access to for testing.

Then open a post in your dev subreddit to see the game running with your local server.

---

## Project Structure

```
devvit-ski/
├── devvit.json           ← App config: entrypoints, menu items, permissions
├── package.json
├── tsconfig.json         ← Project references to client, server, shared
│
└── src/
    ├── client/
    │   └── script.ts     ← Loads Unity WebGL into the Reddit post iframe
    │
    ├── server/
    │   ├── index.ts      ← Main Express server: all /api/* routes
    │   ├── core/
    │   │   ├── post.ts   ← reddit.submitCustomPost() helper
    │   │   └── comment.ts← reddit.submitComment() helper
    │   └── menu/         ← Supplementary Devvit menu action examples
    │
    └── shared/
        └── types/
            └── api.ts    ← TypeScript types shared between client and server
                            (must stay in sync with Assets/Scripts/Devvit/Runtime/DevvitTypes.cs)
```

---

## API Endpoints

All endpoints are served from the same origin as the Unity WebGL build. Unity calls them using relative URLs (e.g., `/api/init`).

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/init` | Returns user context (username, snoovatar, previous score, post data) |
| POST | `/api/level-completed` | Saves session score to Redis |
| POST | `/api/daily-game-completed` | Submits score to leaderboard (Redis sorted set) |
| GET | `/api/leaderboard/:postId` | Returns top 10 players with snoovatars |
| GET | `/api/leaderboard/:postId/user/:username` | Returns a specific user's rank |
| POST | `/api/create-custom-post` | Creates a new Reddit custom post |
| POST | `/api/submit-comment` | Submits a comment to a post |
| POST | `/api/open-url` | Opens a URL in the parent window (WebGL sandbox workaround) |
| POST | `/internal/on-app-install` | Lifecycle hook: creates initial post on install |

See [src/server/index.ts](src/server/index.ts) for full documentation on each endpoint.

---

## Redis Data Schema

Devvit provides a Redis instance scoped to your app. This project uses the following key patterns:

| Key | Type | Description |
|---|---|---|
| `{postId}:{username}` | String | User's session score for a post, serialized as `"score;extraData"` |
| `leaderboard:{postId}` | Sorted Set | Post leaderboard; member=username, score=numeric score |
| `score:{postId}:{username}` | String | Individual user's leaderboard score (for quick lookup) |

The `"score;extraData"` format stores multiple values in a single Redis string. For example, `"1234.56;789.0"` stores a score of 1234.56 and extra data (furthest distance) of 789.0.

---

## Devvit Permissions

This app requests the following Reddit permissions (declared in `devvit.json`):

| Permission | Used for |
|---|---|
| `SUBMIT_POST` | Creating challenge posts on behalf of users |
| `SUBMIT_COMMENT` | Submitting score comments on behalf of users |
| `SUBSCRIBE_TO_SUBREDDIT` | Included from the starter template but not actively used by this game. Safe to remove if you are forking this project. |

---

## Commands

| Command | Description |
|---|---|
| `npm run dev` | Start local dev server with live reload |
| `npm run build` | Build both client and server |
| `npm run deploy` | Build and upload a new version to Devvit |
| `npm run launch` | Publish app for review |
| `npm run login` | Authenticate CLI with Reddit |
| `npm run type-check` | Run TypeScript type checking |
| `npm run check` | Alias for `type-check` |

---

## Configuration (`devvit.json`)

Key fields every contributor should know about:

| Field | Description |
|---|---|
| `dev.subreddit` | **Change this to your own subreddit** before running `npm run dev`. This is where `devvit playtest` will deploy the app for local testing. |
| `post.entrypoints.default` | The splash screen shown when the post first opens. `inline: true` means it's rendered directly in the feed without expanding — this is what lets users see the challenge info before tapping in. |
| `post.entrypoints.game` | The full Unity WebGL game, loaded when the user taps "Start" on the splash screen. |
| `media.dir` | Folder containing static media assets uploaded with the app (e.g. images used in posts). |
| `permissions.reddit.asUser` | Reddit permissions the app requests when acting on behalf of users. `SUBMIT_POST` and `SUBMIT_COMMENT` are required for the challenge post flow. `SUBSCRIBE_TO_SUBREDDIT` is unused and safe to remove when forking. |

---

## Deployment

After building Unity (see root README for the two-step build process):

1. Copy Unity WebGL build files to `src/client/public/Build/`
2. Run `npm run build` to build the TypeScript
3. Run `npm run deploy` to upload to Devvit
4. Run `npm run launch` when ready to publish for review
