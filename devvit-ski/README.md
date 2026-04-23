# devvit-ski

TypeScript server and client for **Ski99**, built on Reddit's [Devvit Web](https://developers.reddit.com/docs/webview) framework. This is the half of the repo that runs on Reddit's infrastructure: it hosts the HTTP API, writes to Redis, creates challenge posts, and loads the Unity WebGL build into the post iframe.

For the game itself, the architecture diagram, and the Unity side of the code, see the **[root README](../README.md)**.

> **Who this README is for:** contributors forking the repo, running it locally, and opening PRs. If you just want to play, head to the Reddit post.

---

## About the game

### Premise

You're skiing down an endless mountain. The slope never ends, it just gets faster and more crowded with trees, rocks, ramps, and snowbanks. Dodge what you can, jump what you can't, and try to post a score worth beating — because once you do, the game turns it into a Reddit post other players see, play, and try to top. Ski99 is an **arcade skier built around a peer-to-peer leaderboard**: every high score becomes a new challenge post in the subreddit, and the loop closes when someone beats you in the comments.

### Features

- **Trick-based scoring.** Points come from style, not distance. The combo engine rewards:
  - **Hangtime** — points per second of airtime
  - **Spins** — Y-axis rotations, scaled from 180° up past 1080°
  - **Backflips** — X-axis rotations, scored per full 360°
  - **Near misses** — passing close to obstacles without hitting them
  - **Combo multiplier** — every trick extends a 3-second window and bumps the multiplier; bank it by landing clean, lose it by crashing
- **Procedural infinite terrain.** Slope is built from recycled 200-unit blocks spawned in a grid as you move. No pre-authored level, no streaming from disk — it just keeps going.
- **Progressive difficulty with a reprise system.** Speed climbs (up to 40 u/s), obstacle density climbs with it, and every 50 rows of hard slope the game drops in a 10-row easier "reprise" so the game stays tense without becoming unfair.
- **Zen mode.** Same slope, difficulty scaling off. For when you just want to vibe down the mountain.
- **Reddit challenge-post loop.** Post your score and the game creates a custom Reddit post with your score embedded as `postData`. When another player opens it, they see your score as the target and can reply to the challenge with a comment when they beat it.
- **Leaderboards.** Per-post top-10 with Reddit avatars (snoovatars), plus your personal rank against the field, backed by Redis sorted sets.
- **Procedural audio.** Every sound effect is synthesized at runtime (BFXR-style waveforms + ADSR + effects). Zero audio files ship in the build.

### Content

- **Obstacles:** pine trees, rocks (three variants), snowbanks, narrow steep ramps, plus a "yard sale" physics reaction when you wipe out.
- **Difficulty tiers:** obstacle types and spacing unlock as you progress; higher tiers pack more of the hazardous prefabs per row.
- **Persistence:** per-post session scores and per-post leaderboards stored in the app's Redis instance.
- **Reddit surfaces:** a splash-screen entrypoint that renders inline in the feed (shows the challenge score without expanding the post) and a full-screen game entrypoint that loads Unity WebGL.
- **Visual style:** low-poly, arcade-colorful, with a custom dither-fade shader for WebGL-friendly transparency.
- **Tech footprint:** Unity 6.2 (URP) on the client, Express 5 + Devvit Web 0.12 on the server, Redis for persistence, procedural audio meaning the WebGL build ships without any bundled sound files.

---

## What lives here

| | |
|---|---|
| **`src/server/`** | Express server (runs on Devvit). All `/api/*` routes, Reddit API calls, Redis reads/writes. |
| **`src/client/`** | Tiny bootstrap that loads the Unity WebGL build into the Reddit post iframe. |
| **`src/shared/`** | TypeScript types shared between server and client. **Mirrored on the Unity side** — see [Keeping types in sync](#keeping-types-in-sync). |
| **`devvit.json`** | Devvit app manifest: entrypoints, menu items, Reddit permissions, dev subreddit. |

Everything Unity-facing (the game, scoring, terrain, audio) lives in `../Ski_Unity_Project/`.

---

## Prerequisites

- **Node.js 18+** (Node 22 recommended)
- **Devvit CLI**: `npm install -g devvit`
- A **Reddit account** with access to a test subreddit you moderate. [Create one here](https://www.reddit.com/subreddits/create) if you don't have one.
- For end-to-end testing with the game: a Unity WebGL build dropped into `src/client/public/Build/` (see [root README](../README.md#exporting-unity-for-devvit)). You do **not** need Unity to work on the server alone.

---

## Quick start

```bash
# from repo root
cd devvit-ski
npm install
npm run login                           # authenticate Devvit CLI with Reddit
# edit devvit.json → set "dev.subreddit" to a sub you moderate
npm run dev                             # client + server + devvit playtest, all watching
```

Then open a post in your dev subreddit. The server hot-reloads on TypeScript changes.

---

## How it fits together

```
Reddit post
   │
   ├── Devvit client  (src/client/script.ts)
   │     └── loads Unity WebGL into the iframe
   │
   └── Devvit server  (src/server/index.ts)
         ├── /api/*        ← Unity calls these over HTTP (same origin)
         ├── Reddit API    ← posts, comments, user context, snoovatars
         └── Redis         ← leaderboards, per-user session scores
```

Unity never talks to Reddit directly. It hits this server's `/api/*` endpoints, which run on Devvit's infrastructure with a pre-authenticated Reddit context. This is what makes the server the single source of truth for scoring and leaderboards — the client can't fake a score without the server accepting it.

For the full architecture diagram including the challenge-post viral loop, see the [root README](../README.md#architecture).

---

## Project structure

```
devvit-ski/
├── devvit.json                   App manifest (subreddit, permissions, entrypoints)
├── package.json
├── tsconfig.json                 Project references → client, server, shared
│
└── src/
    ├── client/
    │   ├── script.ts             Loads Unity WebGL into the iframe
    │   └── public/Build/         (gitignored) Unity WebGL output lives here
    │
    ├── server/
    │   ├── index.ts              Express app — all /api/* routes
    │   ├── core/
    │   │   ├── post.ts           reddit.submitCustomPost() helper
    │   │   └── comment.ts        reddit.submitComment() helper
    │   └── menu/                 Supplementary menu action examples (see menu/README.md)
    │
    └── shared/
        └── types/
            └── api.ts            Shared request/response types
                                  ⚠️  Mirrored in Assets/Scripts/Devvit/Runtime/DevvitTypes.cs
```

---

## Local development workflow

### `npm run dev`

Runs three processes concurrently (see `package.json` for the exact `concurrently` invocation):

- **`dev:client`** — Vite watch build of `src/client/` → `dist/client/`
- **`dev:server`** — Vite watch build of `src/server/` → `dist/server/`
- **`dev:devvit`** — `devvit playtest` uploads the built bundle to your `dev.subreddit` on every change

The first run takes ~30s to upload. After that, edits to `src/server/index.ts` take ~3–5s to hit the live post.

### Testing without Reddit (fast iteration)

You can exercise server logic without hitting Reddit at all — Unity has a mock `IDevvitService` implementation (`DevvitServiceMock.cs`) selectable from the Unity Editor. Use `DevvitMockConfigWindow` (Unity menu) to set fake usernames, scores, and challenge data. This is the fastest feedback loop for anything that isn't the Reddit API itself.

Use this when: you're iterating on game logic, UI, or anything that calls `IDevvitService`.

### Testing on Reddit (full loop)

Required when you're changing:

- An `/api/*` route signature
- `devvit.json` (permissions, entrypoints, menu items)
- Anything that calls `context.reddit.*` or `context.redis.*`

Run `npm run dev`, then open a post in `dev.subreddit`. The Unity build served is whatever is in `src/client/public/Build/` — rebuild Unity if you've changed the game side.

---

## API reference

All endpoints share the Unity WebGL origin; Unity calls them with relative URLs (`/api/init`, etc.). Auth context (`context.userId`, `context.postId`) is injected by Devvit — you do **not** handle auth in handler code.

| Method | Endpoint | Purpose | Acts as |
|---|---|---|---|
| GET  | `/api/init` | User context: username, snoovatar, previous score, challenge post data | `APP` |
| POST | `/api/level-completed` | Persists session score to Redis | `APP` |
| POST | `/api/daily-game-completed` | Writes to leaderboard sorted set | `APP` |
| GET  | `/api/leaderboard/:postId` | Top 10 players + snoovatars | `APP` |
| GET  | `/api/leaderboard/:postId/user/:username` | Specific user's rank | `APP` |
| POST | `/api/create-custom-post` | Creates a challenge post carrying the player's score as `postData` | `USER` |
| POST | `/api/submit-comment` | Posts a comment on behalf of the player | `USER` |
| POST | `/api/open-url` | Navigates the parent Reddit window (WebGL sandbox workaround) | — |
| POST | `/internal/on-app-install` | Lifecycle hook: creates the seed post on install | `APP` |

**`APP` vs `USER`:** actions marked `USER` use `runAs: "USER"` so the post/comment appears under the player's name, not the bot. See the root README's [Devvit concepts section](../README.md#key-devvit-concepts-demonstrated).

Source of truth: [`src/server/index.ts`](src/server/index.ts).

---

## Redis data schema

Devvit exposes a Redis instance scoped to this app. Keys:

| Key | Type | What it holds |
|---|---|---|
| `{postId}:{username}` | String | Per-post session score, serialized as `"score;extraData"` (e.g. `"1234.56;789.0"` — score and furthest distance). Packed into one string to avoid two round-trips. |
| `leaderboard:{postId}` | Sorted Set | Per-post leaderboard. `member = username`, `score = numeric score`. Sorted set lets us pull top 10 and a user's rank in O(log N). |
| `score:{postId}:{username}` | String | User's leaderboard score, duplicated here for fast single-key lookup (avoids `ZSCORE` round-trips on hot paths). |

When adding new persistent state, follow the `{scope}:{id}` prefix convention so it's easy to audit keys per post/user.

---

## Devvit configuration (`devvit.json`)

| Field | What it does |
|---|---|
| `dev.subreddit` | **Change before running `npm run dev`.** `devvit playtest` deploys the app here. Must be a sub you moderate. |
| `post.entrypoints.default` | Splash screen shown inline in the feed (`inline: true` → no expand required). Players see the challenge score before tapping in. |
| `post.entrypoints.game` | Full Unity WebGL game, loaded when the player taps Start on the splash screen. |
| `media.dir` | Static media shipped with the app (used for post thumbnails, etc.). |
| `permissions.reddit.asUser` | Reddit permissions requested when acting as the player. See below. |

### Permissions

| Permission | Why it's requested |
|---|---|
| `SUBMIT_POST` | Creating challenge posts from the player's account. |
| `SUBMIT_COMMENT` | Posting score comments on the player's behalf. |
| `SUBSCRIBE_TO_SUBREDDIT` | Legacy from the starter template, not used. **Safe to remove if you're forking.** |

---

## Keeping types in sync

The server and Unity speak the same JSON. The types are defined twice:

- **TypeScript:** [`src/shared/types/api.ts`](src/shared/types/api.ts)
- **C#:** [`Ski_Unity_Project/Assets/Scripts/Devvit/Runtime/DevvitTypes.cs`](../Ski_Unity_Project/Assets/Scripts/Devvit/Runtime/DevvitTypes.cs)

There is no codegen. **If you change one, change the other in the same PR**, including field names (JSON serialization is case-sensitive on both sides) and optional-ness.

A good PR description for any type change lists both files and both diffs.

---

## Adding a new API endpoint

The 90% path for a new endpoint:

1. **Add the shared type** in both places:
   - `src/shared/types/api.ts` — request + response interfaces.
   - `Assets/Scripts/Devvit/Runtime/DevvitTypes.cs` — matching C# classes/structs. Field names must match exactly.
2. **Add the route** in `src/server/index.ts`. Use `context.reddit`, `context.redis`, `context.userId`, `context.postId` — don't reach for auth manually. If the action should appear under the player's name, set `runAs: "USER"`; otherwise leave as `APP`.
3. **Add the Unity caller** in `Assets/Scripts/Devvit/Runtime/IDevvitService.cs` (interface method) + both implementations:
   - `DevvitServiceBuild.cs` — real HTTP call via `UnityWebRequest`.
   - `DevvitServiceMock.cs` — editor-only fake so in-Editor testing still works.
4. **Wire the call site** wherever in the game you want to trigger it (usually `SaveDataManager.cs` or a UI script).
5. **Type-check both sides:**
   ```bash
   cd devvit-ski && npm run check        # TypeScript
   # Unity: compilation happens on focus; watch the Editor console
   ```
6. **Test both paths:**
   - In-Editor with the mock (fast).
   - `npm run dev` + open the dev-subreddit post (real).

If you're adding a **new Reddit permission**, declare it in `devvit.json` under `permissions.reddit.asUser` and mention it in your PR — it's visible to users on install.

---

## Commands

| Command | What it does |
|---|---|
| `npm run dev` | Client + server + `devvit playtest`, all watching. |
| `npm run build` | One-shot build of client and server. |
| `npm run deploy` | `build` + `devvit upload` — pushes a new app version. |
| `npm run launch` | `deploy` + `devvit publish` — submits for Reddit review. |
| `npm run login` | `devvit login` — authenticate the CLI. |
| `npm run type-check` | TypeScript project-references check. |
| `npm run check` | Alias for `type-check`. |

---

## Build and deploy

Full deploy from a clean clone:

1. Build Unity WebGL (two-pass; see [root README](../README.md#exporting-unity-for-devvit)) and copy output to `src/client/public/Build/`.
2. `npm run build` — produces `dist/client/` and `dist/server/`.
3. `npm run deploy` — uploads a new version of the app to Devvit.
4. `npm run launch` — publishes for Reddit review (only needed when you want the app publicly installable).

Between steps 1 and 2, eyeball `dist/client/public/Build/` — if it's missing the `.unityweb` files or the `.framework.js`, the Unity export didn't complete. The two-pass quirk is the most common source of broken deploys.

---

## Troubleshooting

**`devvit playtest` says "subreddit not found" or "not a moderator".**
`devvit.json` → `dev.subreddit` must be a sub you moderate. Create one at [reddit.com/subreddits/create](https://www.reddit.com/subreddits/create) and add yourself as moderator.

**`npm run login` succeeds but `dev` still fails with auth errors.**
Your Devvit token may be stale. Run `devvit logout && npm run login`.

**Unity build works in Editor but post iframe is blank.**
Almost always the two-pass WebGL export — framework.js ended up compressed, or `.unityweb` files ended up uncompressed. Redo the export per the [root README](../README.md#exporting-unity-for-devvit) and verify the file extensions in `src/client/public/Build/`.

**Endpoint returns 200 but Unity crashes parsing the response.**
TS and C# types are out of sync. Diff `src/shared/types/api.ts` against `Assets/Scripts/Devvit/Runtime/DevvitTypes.cs` — especially field names (case-sensitive) and optionals.

**`context.userId` is undefined in a handler.**
You're probably on an endpoint Reddit invoked without a user (e.g. an internal lifecycle hook). Only user-initiated routes get a `userId`.

**Changes to `devvit.json` don't take effect.**
Stop `npm run dev` and start it again — `devvit playtest` picks up manifest changes only on restart.

---

## Contributing

See [`CONTRIBUTING.md`](../CONTRIBUTING.md) for repo-wide setup, code style, and PR guidelines.

PR-specific expectations for this subproject:

- Run `npm run check` before opening the PR.
- If you changed `src/shared/types/api.ts`, update `DevvitTypes.cs` in the same PR and note both files in the description.
- If you changed `devvit.json` permissions, call it out in the PR title.
- Keep server handlers thin — push logic into helpers under `src/server/core/` when a handler gets past ~30 lines.

---

## License

Code in this directory is under the [MIT License](../LICENSE). Game assets (models, textures, audio) are under the [Asset License — Game Use Only](../LICENSE-ASSETS). See the [root README](../README.md#license) for the full story.
