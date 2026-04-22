import { Router, Response } from "express";
import { context } from "@devvit/web/server";
import { UiResponse } from "@devvit/web/shared";
import { createPost } from "../core/post";

/**
 * Menu action to create a test post with hardcoded sample quiz data.
 * This is useful for quick testing without needing to fill out a form.
 */
export const registerCreateTestPost = (router: Router): void => {
  router.post('/internal/menu/create-test-post', async (_req, res: Response<UiResponse>): Promise<void> => {
    try {
      // Sample quiz data matching the Unity game's expected format
      const sampleGameData = {
        "id": "test-quiz-001",
        "name": "Sample Price Guessing Quiz",
        "description": "Test your knowledge of everyday prices!",
        "quiz": {
          "currencyItem": {
            "name": "US Dollar",
            "value": 1.0,
            "desc": "Standard US currency",
            "imageURL": ""
          },
          "guessItems": [
            {
              "name": "Coffee",
              "value": 5.0,
              "desc": "A cup of coffee at a cafe",
              "imageURL": ""
            },
            {
              "name": "Movie Ticket",
              "value": 15.0,
              "desc": "Average movie theater ticket",
              "imageURL": ""
            }
          ]
        }
      };

      console.log('[Create Test Post] Creating post with sample data...');

      // Create post with sample data
      const post = await createPost("Test Quiz - Sample Data", sampleGameData);

      console.log(`[Create Test Post] Post created successfully: ${post.id}`);

      // Menu actions must return navigateTo to redirect user
      res.json({
        navigateTo: `https://reddit.com/r/${context.subredditName}/comments/${post.id}`,
      });
    } catch (error) {
      console.error(`[Create Test Post] Error: ${error}`);
      // On error, show toast
      res.json({
        showToast: `Failed to create post: ${error instanceof Error ? error.message : 'Unknown error'}`,
      });
    }
  });
};
