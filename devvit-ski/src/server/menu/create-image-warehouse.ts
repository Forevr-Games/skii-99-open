import { Router, Response } from "express";
import { context } from "@devvit/web/server";
import { UiResponse } from "@devvit/web/shared";
import { createPost } from "../core/post";

/**
 * Menu action to create an image warehouse post with multiple image uploads.
 */
export const registerCreateImageWarehouse = (router: Router): void => {
  router.post('/internal/menu/create-image-warehouse', async (req, res: Response<UiResponse>): Promise<void> => {
    try {
      console.log('[Image Warehouse] Menu action triggered');
      console.log('[Image Warehouse] Request body:', JSON.stringify(req.body, null, 2));

      // If form data is submitted, create the post
      if (req.body && req.body.image1URL) {
        console.log('[Image Warehouse] Form submitted, creating post');

        // Collect all image URLs into an array, filtering out empty ones
        const images: string[] = [];
        for (let i = 1; i <= 70; i++) {
          const url = req.body[`image${i}URL`];
          if (url) {
            images.push(url);
          }
        }

        // Simple post data: just an array of image URLs
        const postData = { images };

        console.log('[Image Warehouse] Post data:', JSON.stringify(postData, null, 2));

        // Create post with image data
        const post = await createPost('Image Warehouse', postData);
        console.log(`[Image Warehouse] Post created successfully: ${post.id}`);

        // Redirect to the new post
        res.json({
          navigateTo: `https://reddit.com/r/${context.subredditName}/comments/${post.id}`,
        });
        return;
      }

      // Show the form (first time the menu action is clicked)
      console.log('[Image Warehouse] Showing form to user');
      res.json({
        showForm: {
          name: 'createImageWarehouseForm',
          form: {
            title: 'Create Image Warehouse',
            fields: Array.from({ length: 70 }, (_, i) => ({
              type: 'image' as const,
              name: `image${i + 1}URL`,
              label: `Image ${i + 1}`,
              required: false,
            })),
            acceptLabel: 'Create Image Warehouse'
          }
        }
      });
    } catch (error) {
      console.error(`[Image Warehouse] Error: ${error}`);
      res.json({
        showToast: `Failed to create image warehouse: ${error instanceof Error ? error.message : 'Unknown error'}`,
      });
    }
  });
};
