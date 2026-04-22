# Devvit Menu Action Examples

This folder contains supplementary examples of **Devvit menu actions** — they are not required for the core ski game to function.

Menu actions appear in Reddit's right-click context menu (on posts or the subreddit) and allow moderators or users to trigger server-side actions from within Reddit's UI, without opening a post.

---

## Files

| File | What it demonstrates |
|---|---|
| `create-custom-post.ts` | Creating a custom post from a menu action (minimal example) |
| `create-test-post.ts` | Creating a test post; useful for mod tooling |
| `create-quiz.ts` | A complex menu form with multiple input fields, validation, and structured post data |
| `create-image-warehouse.ts` | Creating a post with image content |

These are registered in `src/server/index.ts` at the bottom of the file.

---

## How Menu Actions Work in Devvit

Menu actions are declared in `devvit.json` under `postEntryPoints` or `subredditEntryPoints`. Each entry point specifies:
- Where the menu item appears (post, subreddit, etc.)
- The label shown to the user
- The server endpoint to call when triggered

The `registerCreate*` functions in these files attach the corresponding Express route handlers. The handlers can show a form, validate input, and create Reddit content.

---

## Using These as a Starting Point

To add your own menu action:
1. Create a new file in this folder (e.g., `create-my-feature.ts`)
2. Export a `registerMyFeature(router)` function that adds your route
3. Import and call it in `src/server/index.ts`
4. Add the corresponding entry point in `devvit.json`

See `create-quiz.ts` for an example with form validation and structured data.
