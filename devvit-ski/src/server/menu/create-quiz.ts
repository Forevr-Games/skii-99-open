import { Router, Response } from 'express';
import { context } from '@devvit/web/server';
import { UiResponse } from '@devvit/web/shared';
import { createPost } from '../core/post';

/**
 * Menu action to create a quiz post with a single form containing all fields.
 */
export const registerCreateQuiz = (router: Router): void => {
  router.post(
    '/internal/menu/create-quiz-v2',
    async (req, res: Response<UiResponse>): Promise<void> => {
      try {
        console.log('[Create Quiz] Menu action triggered');

        // If form data is submitted, create the post
        if (req.body && req.body.quizName) {
          console.log('[Create Quiz] Form submitted, creating post');

          const {
            quizName,
            quizId,
            quizDescription,
            // Currency item fields
            currencyName,
            currencyValue,
            currencyDesc,
            currencyImageURL,
            // Guess item fields (1-10)
            guessItemName1,
            guessItemValue1,
            guessItemDesc1,
            guessItemImageURL1,
            guessItemName2,
            guessItemValue2,
            guessItemDesc2,
            guessItemImageURL2,
            guessItemName3,
            guessItemValue3,
            guessItemDesc3,
            guessItemImageURL3,
            guessItemName4,
            guessItemValue4,
            guessItemDesc4,
            guessItemImageURL4,
            guessItemName5,
            guessItemValue5,
            guessItemDesc5,
            guessItemImageURL5,
            guessItemName6,
            guessItemValue6,
            guessItemDesc6,
            guessItemImageURL6,
            guessItemName7,
            guessItemValue7,
            guessItemDesc7,
            guessItemImageURL7,
            guessItemName8,
            guessItemValue8,
            guessItemDesc8,
            guessItemImageURL8,
            guessItemName9,
            guessItemValue9,
            guessItemDesc9,
            guessItemImageURL9,
            guessItemName10,
            guessItemValue10,
            guessItemDesc10,
            guessItemImageURL10,
          } = req.body;

          // Build the game data structure
          const gameData = {
            id: quizId || `quiz-${Date.now()}`,
            name: quizName,
            description: quizDescription || '',
            quiz: {
              currencyItem: {
                name: currencyName || 'US Dollar',
                value: parseFloat(currencyValue) || 1.0,
                desc: currencyDesc || '',
                imageURL: currencyImageURL || '',
              },
              guessItems: [
                {
                  name: guessItemName1,
                  value: parseFloat(guessItemValue1),
                  desc: guessItemDesc1 || '',
                  imageURL: guessItemImageURL1 || '',
                },
                {
                  name: guessItemName2,
                  value: parseFloat(guessItemValue2),
                  desc: guessItemDesc2 || '',
                  imageURL: guessItemImageURL2 || '',
                },
                {
                  name: guessItemName3,
                  value: parseFloat(guessItemValue3),
                  desc: guessItemDesc3 || '',
                  imageURL: guessItemImageURL3 || '',
                },
                {
                  name: guessItemName4,
                  value: parseFloat(guessItemValue4),
                  desc: guessItemDesc4 || '',
                  imageURL: guessItemImageURL4 || '',
                },
                {
                  name: guessItemName5,
                  value: parseFloat(guessItemValue5),
                  desc: guessItemDesc5 || '',
                  imageURL: guessItemImageURL5 || '',
                },
                {
                  name: guessItemName6,
                  value: parseFloat(guessItemValue6),
                  desc: guessItemDesc6 || '',
                  imageURL: guessItemImageURL6 || '',
                },
                {
                  name: guessItemName7,
                  value: parseFloat(guessItemValue7),
                  desc: guessItemDesc7 || '',
                  imageURL: guessItemImageURL7 || '',
                },
                {
                  name: guessItemName8,
                  value: parseFloat(guessItemValue8),
                  desc: guessItemDesc8 || '',
                  imageURL: guessItemImageURL8 || '',
                },
                {
                  name: guessItemName9,
                  value: parseFloat(guessItemValue9),
                  desc: guessItemDesc9 || '',
                  imageURL: guessItemImageURL9 || '',
                },
                {
                  name: guessItemName10,
                  value: parseFloat(guessItemValue10),
                  desc: guessItemDesc10 || '',
                  imageURL: guessItemImageURL10 || '',
                },
              ].filter((item) => item.name),
            },
          };

          // Create post with quiz data as the fourth argument (gameData).
          // createPost signature: (title, asUser?, userGeneratedContent?, gameData?)
          // Passing gameData as the second argument would silently treat it as the
          // boolean asUser flag. Always pass gameData in the fourth position.
          const post = await createPost(quizName, false, undefined, gameData);
          console.log(`[Create Quiz] Post created successfully: ${post.id}`);

          // Redirect to the new post
          res.json({
            navigateTo: `https://reddit.com/r/${context.subredditName}/comments/${post.id}`,
          });
          return;
        }

        // Show the form (first time the menu action is clicked)
        res.json({
          showForm: {
            name: 'createQuizForm',
            form: {
              title: 'Create Quiz',
              fields: [
                // Quiz info
                {
                  type: 'string',
                  name: 'quizName',
                  label: 'Quiz Name',
                  required: true,
                },
                {
                  type: 'paragraph',
                  name: 'quizDescription',
                  label: 'Quiz Description',
                  required: true,
                },
                {
                  type: 'string',
                  name: 'quizId',
                  label: 'Quiz ID (optional, auto-generated if blank)',
                },
                // Currency item
                {
                  type: 'string',
                  name: 'currencyName',
                  label: 'Currency Name',
                  helpText: 'e.g., "US Dollar", "Gold Bar"',
                  required: true,
                },
                {
                  type: 'number',
                  name: 'currencyValue',
                  label: 'Currency Value',
                  defaultValue: 1.0,
                },
                {
                  type: 'string',
                  name: 'currencyDesc',
                  label: 'Currency Description',
                },
                {
                  type: 'image', // This tells the form to expect an image
                  name: 'currencyImageURL',
                  label: 'Currency Image URL',
                  required: true,
                },
                // Guess Item 1
                {
                  type: 'string',
                  name: 'guessItemName1',
                  label: 'Guess Item 1 Name',
                  helpText: 'e.g., "Coffee", "Movie Ticket"',
                  required: true,
                },
                {
                  type: 'number',
                  name: 'guessItemValue1',
                  label: 'Guess Item 1 Value',
                  helpText: 'The actual value to guess',
                  required: true,
                },
                { type: 'string', name: 'guessItemDesc1', label: 'Guess Item 1 Description' },
                {
                  type: 'image',
                  name: 'guessItemImageURL1',
                  label: 'Guess Item 1 Image',
                  required: true,
                },
                // Guess Item 2
                { type: 'string', name: 'guessItemName2', label: 'Guess Item 2 Name' },
                { type: 'number', name: 'guessItemValue2', label: 'Guess Item 2 Value' },
                { type: 'string', name: 'guessItemDesc2', label: 'Guess Item 2 Description' },
                { type: 'image', name: 'guessItemImageURL2', label: 'Guess Item 2 Image' },
                // Guess Item 3
                { type: 'string', name: 'guessItemName3', label: 'Guess Item 3 Name' },
                { type: 'number', name: 'guessItemValue3', label: 'Guess Item 3 Value' },
                { type: 'string', name: 'guessItemDesc3', label: 'Guess Item 3 Description' },
                { type: 'image', name: 'guessItemImageURL3', label: 'Guess Item 3 Image' },
                // Guess Item 4
                { type: 'string', name: 'guessItemName4', label: 'Guess Item 4 Name' },
                { type: 'number', name: 'guessItemValue4', label: 'Guess Item 4 Value' },
                { type: 'string', name: 'guessItemDesc4', label: 'Guess Item 4 Description' },
                { type: 'image', name: 'guessItemImageURL4', label: 'Guess Item 4 Image' },
                // Guess Item 5
                { type: 'string', name: 'guessItemName5', label: 'Guess Item 5 Name' },
                { type: 'number', name: 'guessItemValue5', label: 'Guess Item 5 Value' },
                { type: 'string', name: 'guessItemDesc5', label: 'Guess Item 5 Description' },
                { type: 'image', name: 'guessItemImageURL5', label: 'Guess Item 5 Image' },
                // Guess Item 6
                { type: 'string', name: 'guessItemName6', label: 'Guess Item 6 Name' },
                { type: 'number', name: 'guessItemValue6', label: 'Guess Item 6 Value' },
                { type: 'string', name: 'guessItemDesc6', label: 'Guess Item 6 Description' },
                { type: 'image', name: 'guessItemImageURL6', label: 'Guess Item 6 Image' },
                // Guess Item 7
                { type: 'string', name: 'guessItemName7', label: 'Guess Item 7 Name' },
                { type: 'number', name: 'guessItemValue7', label: 'Guess Item 7 Value' },
                { type: 'string', name: 'guessItemDesc7', label: 'Guess Item 7 Description' },
                { type: 'image', name: 'guessItemImageURL7', label: 'Guess Item 7 Image' },
                // Guess Item 8
                { type: 'string', name: 'guessItemName8', label: 'Guess Item 8 Name' },
                { type: 'number', name: 'guessItemValue8', label: 'Guess Item 8 Value' },
                { type: 'string', name: 'guessItemDesc8', label: 'Guess Item 8 Description' },
                { type: 'image', name: 'guessItemImageURL8', label: 'Guess Item 8 Image' },
                // Guess Item 9
                { type: 'string', name: 'guessItemName9', label: 'Guess Item 9 Name' },
                { type: 'number', name: 'guessItemValue9', label: 'Guess Item 9 Value' },
                { type: 'string', name: 'guessItemDesc9', label: 'Guess Item 9 Description' },
                { type: 'image', name: 'guessItemImageURL9', label: 'Guess Item 9 Image' },
                // Guess Item 10
                { type: 'string', name: 'guessItemName10', label: 'Guess Item 10 Name' },
                { type: 'number', name: 'guessItemValue10', label: 'Guess Item 10 Value' },
                { type: 'string', name: 'guessItemDesc10', label: 'Guess Item 10 Description' },
                { type: 'image', name: 'guessItemImageURL10', label: 'Guess Item 10 Image' },
              ],
              acceptLabel: 'Create Quiz',
            },
          },
        });
      } catch (error) {
        console.error(`[Create Quiz] Error: ${error}`);
        res.json({
          showToast: `Failed to create quiz: ${
            error instanceof Error ? error.message : 'Unknown error'
          }`,
        });
      }
    }
  );
};
