using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
public class MultOfFourImageAdjuster
{
  [MenuItem("Tools/Adjust MultOfFour Images")]
  public static void AdjustMultOfFourImages()
  {
    //get selected objects in the project window
    Object[] selectedObjects = Selection.objects;
    List<Texture2D> texturesToAdjust = new List<Texture2D>();
    //filter selected objects to only include textures
    foreach (Object obj in selectedObjects)
    {
      if (obj is Texture2D)
      {
        texturesToAdjust.Add(obj as Texture2D);
      }
    }

    //show a dialog to confirm the adjustment
    if (texturesToAdjust.Count == 0)
    {
      EditorUtility.DisplayDialog("No Textures Selected", "Please select one or more textures to adjust.", "OK");
      return;
    }
    if (!EditorUtility.DisplayDialog("Adjust Textures", $"This will adjust {texturesToAdjust.Count} textures to have dimensions that are multiples of 4 by adding transparent pixels. Do you want to proceed?", "Yes", "No"))
    {
      return;
    }

    //adjust each texture by:
    //1. calculating how many pixels to add to the width and height to make them multiples of 4
    //2. creating a new texture with the new dimensions
    //3. copying the original texture pixels to the new texture
    //4. filling the added pixels with transparent color
    //do it in a way that uses editor tools so the textures dont need to be read/write enabled
    //use: byte[] tmp = sceneLightRampTexture.GetRawTextureData();
    //Texture2D tmpTexture = new Texture2D(128, 1);
    //tmpTexture.LoadRawTextureData(tmp);
    //for this
    foreach (Texture2D texture in texturesToAdjust)
    {
      string path = AssetDatabase.GetAssetPath(texture);
      TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

      if (importer == null)
      {
        Debug.LogWarning($"Could not get TextureImporter for {texture.name}");
        continue;
      }

      // Get the original imported resolution
      int originalWidth = 0;
      int originalHeight = 0;
      importer.GetSourceTextureWidthAndHeight(out originalWidth, out originalHeight);

      // Calculate new dimensions as multiples of 4
      int newWidth = Mathf.CeilToInt(originalWidth / 4f) * 4;
      int newHeight = Mathf.CeilToInt(originalHeight / 4f) * 4;

      if (newWidth == originalWidth && newHeight == originalHeight)
      {
        Debug.Log($"{texture.name} is already a multiple of 4 ({originalWidth}x{originalHeight})");
        continue; // already a multiple of 4
      }

      bool wasReadable = importer.isReadable;
      int originalMaxSize = importer.maxTextureSize;

      try
      {
        // Temporarily enable readable and set max size to accommodate new dimensions
        importer.isReadable = true;
        importer.maxTextureSize = Mathf.Max(newWidth, newHeight, 2048);
        importer.SaveAndReimport();

        // Read the original pixels
        Color[] pixels = texture.GetPixels();

        // Create new texture with adjusted dimensions
        Texture2D newTexture = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
        Color[] newPixels = new Color[newWidth * newHeight];

        // Copy original pixels
        for (int y = 0; y < originalHeight; y++)
        {
          for (int x = 0; x < originalWidth; x++)
          {
            newPixels[y * newWidth + x] = pixels[y * originalWidth + x];
          }
        }

        // Fill bottom padding with transparent pixels
        for (int y = originalHeight; y < newHeight; y++)
        {
          for (int x = 0; x < newWidth; x++)
          {
            newPixels[y * newWidth + x] = Color.clear;
          }
        }

        // Fill right padding with transparent pixels
        for (int y = 0; y < originalHeight; y++)
        {
          for (int x = originalWidth; x < newWidth; x++)
          {
            newPixels[y * newWidth + x] = Color.clear;
          }
        }

        newTexture.SetPixels(newPixels);
        newTexture.Apply();

        // Encode and overwrite the original file
        byte[] pngData = newTexture.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, pngData);

        Debug.Log($"Adjusted {texture.name} from {originalWidth}x{originalHeight} to {newWidth}x{newHeight}");
      }
      finally
      {
        // Restore original importer settings
        importer.isReadable = wasReadable;
        importer.maxTextureSize = originalMaxSize;
        importer.SaveAndReimport();
      }
    }

  }


  //menuitem to apply the unity imported texture to the actual png file
  //eg if a file is 1024 but imported as 256, this will apply the 256 texture to the png file so it can be used in other programs with the correct dimensions
  [MenuItem("Tools/Apply Imported Texture To PNG")]
  public static void ApplyImportedTextureToPNG()
  {
    Object[] selectedObjects = Selection.objects;
    List<Texture2D> texturesToApply = new List<Texture2D>();
    foreach (Object obj in selectedObjects)
    {
      if (obj is Texture2D)
      {
        texturesToApply.Add(obj as Texture2D);
      }
    }

    if (texturesToApply.Count == 0)
    {
      EditorUtility.DisplayDialog("No Textures Selected", "Please select one or more textures to apply.", "OK");
      return;
    }

    if (!EditorUtility.DisplayDialog("Apply Textures", $"This will apply the imported texture data to the original PNG files for {texturesToApply.Count} textures. Do you want to proceed?", "Yes", "No"))
    {
      return;
    }

    foreach (Texture2D texture in texturesToApply)
    {
      string path = AssetDatabase.GetAssetPath(texture);
      TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

      if (importer == null)
      {
        Debug.LogWarning($"Could not get TextureImporter for {texture.name}");
        continue;
      }

      bool wasReadable = importer.isReadable;

      try
      {
        // Temporarily enable readable
        if (!wasReadable)
        {
          importer.isReadable = true;
          importer.SaveAndReimport();
        }

        int width = texture.width;
        int height = texture.height;

        // Create an uncompressed copy to encode (compressed formats can't be encoded directly)
        Texture2D uncompressedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        uncompressedTexture.SetPixels(texture.GetPixels());
        uncompressedTexture.Apply();

        byte[] pngData = uncompressedTexture.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, pngData);

        Debug.Log($"Applied imported texture of {texture.name} with dimensions {width}x{height} to PNG file.");
      }
      finally
      {
        // Restore original readable setting
        if (importer.isReadable != wasReadable)
        {
          importer.isReadable = wasReadable;
          importer.SaveAndReimport();
        }
      }
    }
  }
}
