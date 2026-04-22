import { Router, Response } from 'express';
import { context } from '@devvit/web/server';
import { UiResponse } from '@devvit/web/shared';
import { createPost } from '../core/post';

/**
 * Menu action to create a custom post with user-provided quiz data via a form.
 * Shows a form with fields for title and game data JSON.
 */
export const registerCreateCustomPost = (router: Router): void => {
  router.post(
    '/internal/menu/create-custom-post',
    async (req, res: Response<UiResponse>): Promise<void> => {
      try {
        console.log('[Create Custom Post] Menu action triggered');

        // If form data is submitted, create the post
        if (req.body && req.body.title && req.body.gameData) {
          console.log('[Create Custom Post] Form submitted, creating post');

          const title = req.body.title;
          let gameData;

          // Parse the gameData JSON string
          try {
            gameData = JSON.parse(req.body.gameData);
          } catch (parseError) {
            console.error('[Create Custom Post] Invalid JSON in gameData:', parseError);
            res.json({
              showToast: `Invalid JSON in game data: ${
                parseError instanceof Error ? parseError.message : 'Unknown error'
              }`,
            });
            return;
          }

          // Create post with custom data as the fourth argument (gameData).
          // createPost signature: (title, asUser?, userGeneratedContent?, gameData?)
          // Passing gameData as the second argument would silently treat it as the
          // boolean asUser flag. Always pass gameData in the fourth position.
          const post = await createPost(title, false, undefined, gameData);
          console.log(`[Create Custom Post] Post created successfully: ${post.id}`);

          // Redirect to the new post
          res.json({
            navigateTo: `https://reddit.com/r/${context.subredditName}/comments/${post.id}`,
          });
          return;
        }

        // Show the form (first time the menu action is clicked)
        res.json({
          showForm: {
            name: 'createCustomPostForm',
            form: {
              title: 'Create Post from JSON',
              fields: [
                {
                  type: 'string',
                  name: 'title',
                  label: 'Post Title',
                },
                {
                  type: 'paragraph',
                  name: 'gameData',
                  label: 'Game Data (JSON)',
                },
              ],
              acceptLabel: 'Create Post',
            },
          },
        });
      } catch (error) {
        console.error(`[Create Custom Post] Error: ${error}`);
        res.json({
          showToast: `Failed to create custom post: ${
            error instanceof Error ? error.message : 'Unknown error'
          }`,
        });
      }
    }
  );
};
