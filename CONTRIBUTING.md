# Contributing to Ski99

Thank you for your interest in contributing! This guide covers how to set up the development environment for both the Unity game and the Devvit server.

---

## Development Environment Setup

### 1. Unity Game

**Requirements:** Unity 6.2 (6000.2.8f1) via [Unity Hub](https://unity.com/download)

```bash
# Open the project in Unity Hub
# Project path: Ski_Unity_Project/
```

- Open the scene at `Assets/Scenes/Ski.unity`
- Press **Play** to run with mock Reddit data
- Configure mock data via **Reddit > Devvit Mock Config** in the menu bar

The mock service (`DevvitServiceMock.cs`) runs in-editor only and simulates all API responses locally — no Reddit account or network connection required.

### 2. Devvit Server

**Requirements:** Node.js 18+, Devvit CLI

```bash
npm install -g devvit
devvit login        # authenticate with your Reddit account

cd devvit-ski
npm install
npm run dev         # starts local dev server
```

To test on Reddit: open a post in your dev subreddit and the local server serves the game.

---

## Testing Your Changes

### Unity changes (C# game code)
- Press Play in the Unity Editor
- The mock service (`DevvitServiceMock.cs`) provides all fake Reddit data
- Check the Console for `[DevvitServiceMock]` and `[SaveDataManager]` log output
- Use **Reddit > Devvit Mock Config** to change mock username, score, and post data

### Server changes (TypeScript)
- `npm run dev` in `devvit-ski/` watches for changes and hot-reloads
- Open the post on Reddit to test the updated server

### End-to-end (Unity + Devvit together)
1. Build Unity for WebGL (see README for two-step build process)
2. Copy build output to `devvit-ski/src/client/public/Build/`
3. Run `npm run dev` and open the post on Reddit

---

## Project Structure Overview

```
devvit-ski/src/server/   ← All Devvit/Reddit API code lives here
  index.ts               ← Main API routes (start here)
  core/post.ts           ← Post creation helper
  core/comment.ts        ← Comment submission helper
  menu/                  ← Supplementary Devvit menu action examples

Assets/Scripts/Devvit/   ← Unity-side Reddit integration
  Runtime/               ← Production + mock service implementations
  Editor/                ← Unity Editor tooling (mock config window)
```

---

## Code Style

- **TypeScript:** Follow existing formatting (Prettier config in `devvit-ski/.prettierrc`)
- **C#:** Follow Unity C# conventions; use XML doc comments on public methods
- **Comments:** Explain *why*, not just *what* — especially for Devvit/Reddit-specific patterns

---

## Submitting Changes

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature-name`
3. Make your changes
4. Test in both Unity Editor (mock) and on Reddit (`npm run dev`)
5. Open a pull request with a description of what changed and why
