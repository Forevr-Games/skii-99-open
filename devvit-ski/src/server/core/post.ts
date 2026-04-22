/**
 * Custom Post Creation Helper
 *
 * Wraps Devvit's `reddit.submitCustomPost()` API with game-specific logic.
 *
 * What is a "custom post"?
 *   A Devvit custom post is a Reddit post that renders your app's UI (the Unity
 *   game) instead of a link or text body. Users see the game when they open the
 *   post. Custom posts are created via `reddit.submitCustomPost()`.
 *
 * Key concepts:
 *
 *   postData:
 *     Arbitrary JSON you can embed in the post at creation time. This data is
 *     available in `context.postData` on every subsequent server request that
 *     originates from this post. Used here to store challenge data (original
 *     player's score, distance, etc.) so that challengers see the target when
 *     they open the post.
 *     IMPORTANT: The field MUST be named `postData` — using `data` or any other
 *     name will cause it to be silently ignored by Devvit.
 *
 *   runAs — "USER" vs "APP":
 *     "USER": The post is attributed to the logged-in Reddit user. Requires
 *             `userGeneratedContent` to be provided (Reddit policy for content
 *             posted on behalf of a user). The user must have granted this app
 *             the SUBMIT_POST permission (declared in devvit.json).
 *     "APP":  The post is attributed to the app's service account. No UGC
 *             required. Used for auto-created posts (e.g. on-app-install).
 *
 *   userGeneratedContent:
 *     When posting as a user, Reddit requires that the post include content
 *     that the user explicitly generated (text, image URL, etc.). This is a
 *     platform-level content policy requirement, not a Devvit quirk.
 */
import { reddit } from "@devvit/web/server";
import { UserGeneratedContent } from "../../shared/types/api";

/**
 * Creates a new custom Reddit post in the current subreddit.
 *
 * @param title - The post title displayed on Reddit
 * @param asUser - If true, posts as the logged-in user (requires userGeneratedContent)
 * @param userGeneratedContent - Required when asUser=true; the user-facing content
 * @param gameData - Arbitrary JSON object embedded in the post as postData;
 *                   accessible via context.postData in all future server requests
 *                   from this post
 */
export const createPost = async (
  title: string = "unity-starter",
  asUser?: boolean,
  userGeneratedContent?: UserGeneratedContent,
  gameData?: Record<string, unknown>
) => {
  // Typed as a plain object so TypeScript enforces that callers pass gameData
  // in the correct (fourth) position. Passing it as the second argument would
  // silently coerce the object to the boolean asUser flag.
  const postOptions: {
    title: string;
    runAs?: string;
    userGeneratedContent?: UserGeneratedContent;
    postData?: Record<string, unknown>;
  } = {
    title: title,
  };

  // When posting as the authenticated user, we must include:
  //   1. runAs: "USER" — tells Devvit to attribute the post to the user
  //   2. userGeneratedContent — the user's content (text, images)
  // Without both, Devvit will reject the request or fall back to posting as APP.
  if (asUser !== undefined && asUser) {
    postOptions.runAs = "USER";
    const ugc = userGeneratedContent || { text: "" };
    postOptions.userGeneratedContent = ugc;
  }

  // Embed custom data in the post. This becomes context.postData on all
  // subsequent server requests from this post.
  // CRITICAL: Must use "postData" as the key name — "data" will not work.
  if (gameData !== undefined) {
    postOptions.postData = gameData;
  }

  console.log('[createPost] Creating post:', title, asUser ? '(as user)' : '(as app)');

  const result = await reddit.submitCustomPost(postOptions);

  console.log('[createPost] Post created with id:', result.id);

  return result;
};
