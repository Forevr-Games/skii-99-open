/**
 * Comment Submission Helper
 *
 * Wraps Devvit's `reddit.submitComment()` API.
 *
 * How comments work in Devvit:
 *   `reddit.submitComment()` posts a comment to Reddit. The `id` field is the
 *   thing being replied to — it can be either:
 *     - A post ID (e.g. "t3_abc123")    → creates a top-level comment on the post
 *     - A comment ID (e.g. "t1_def456") → creates a reply to that comment
 *
 * runAs — "USER" vs "APP":
 *   "USER": The comment is attributed to the logged-in Reddit user. The user
 *           must have granted this app the SUBMIT_COMMENT permission (declared
 *           in devvit.json). Used when the player shares their score.
 *   "APP":  The comment is attributed to the app's service account. Used for
 *           system comments like the stickied score template on challenge posts.
 *
 * The sticky comment pattern used in this game:
 *   1. Player creates a challenge post (CreateCustomPost, asUser=true)
 *   2. App immediately posts a comment on that post (asUser=false) formatted as:
 *      "Score: 1234 | Distance: 567m"
 *   3. Challengers reply to this comment with their own scores
 *   4. This keeps all challenge responses threaded under one comment
 */
import { reddit } from "@devvit/web/server";

/**
 * Submits a comment to a Reddit post or as a reply to an existing comment.
 *
 * @param text - The comment text content
 * @param replyToId - The post or comment ID to reply to (e.g. "t3_abc123")
 * @param asUser - If true, posts as the logged-in user; otherwise posts as the app
 */
export const submitComment = async (text: string, replyToId: string, asUser?: boolean) => {
  const postOptions: any = {
    text: text,
    id: replyToId,
  };

  // runAs controls who the comment is attributed to on Reddit.
  // Always set explicitly to avoid relying on Devvit's default behavior.
  if (asUser !== undefined && asUser) {
    postOptions.runAs = "USER";
  } else {
    postOptions.runAs = "APP";
  }

  const result = await reddit.submitComment(postOptions);

  console.log('[submitComment] Comment submitted with id:', result.id);

  return result;
};
