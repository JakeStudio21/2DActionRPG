using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class AnimationSpriteSheetGenerator : EditorWindow
{
    [MenuItem("Tools/Sprites/Animation Sprite Sheet Generator")]
    public static void ShowWindow()
    {
        GetWindow<AnimationSpriteSheetGenerator>("Sprite Sheet Generator");
    }

    // UI Fields
    private string sourceFolderPath = "";
    private int columns = 4;
    private int rows = 3;
    private int frameWidth = 256;
    private int frameHeight = 256;
    private bool isSequentialMode = true;
    private string[] customFrameNames = new string[12]; // Default 4x3 = 12 frames
    private int frameInterval = 1; // Frame interval for Auto Fill Sequential
    private Vector2 scrollPosition;
    
    // Internal
    private readonly string[] directionNames = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
    private string sourceFolderName = "";

    void OnGUI()
    {
        GUILayout.Label("Animation Sprite Sheet Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Source Folder Selection
        EditorGUILayout.LabelField("Source Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Source Folder:", GUILayout.Width(100));
        EditorGUILayout.TextField(sourceFolderPath);
        if (GUILayout.Button("Browse", GUILayout.Width(80)))
        {
            SelectSourceFolder();
        }
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(sourceFolderName))
        {
            EditorGUILayout.HelpBox($"Detected folder: {sourceFolderName}", MessageType.Info);
        }

        EditorGUILayout.Space();

        // Grid Size Settings
        EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Columns:", GUILayout.Width(80));
        int newColumns = EditorGUILayout.IntField(columns, GUILayout.Width(60));
        EditorGUILayout.LabelField("Rows:", GUILayout.Width(60));
        int newRows = EditorGUILayout.IntField(rows, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        // Update grid size and resize custom frame names array if needed
        if (newColumns != columns || newRows != rows)
        {
            columns = Mathf.Max(1, newColumns);
            rows = Mathf.Max(1, newRows);
            int totalFrames = columns * rows;
            System.Array.Resize(ref customFrameNames, totalFrames);
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Frame Width:", GUILayout.Width(100));
        frameWidth = EditorGUILayout.IntField(frameWidth, GUILayout.Width(60));
        EditorGUILayout.LabelField("Frame Height:", GUILayout.Width(100));
        frameHeight = EditorGUILayout.IntField(frameHeight, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox($"Output Size: {frameWidth * columns} x {frameHeight * rows}", MessageType.None);

        EditorGUILayout.Space();

        // Frame Selection Mode
        EditorGUILayout.LabelField("Frame Selection", EditorStyles.boldLabel);
        isSequentialMode = EditorGUILayout.Toggle("Sequential Mode", isSequentialMode);

        if (isSequentialMode)
        {
            EditorGUILayout.HelpBox("Sequential: frame_0001, frame_0002, frame_0003...", MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField("Custom Frame Names:", EditorStyles.boldLabel);
            
            // Scroll view for custom frame input fields
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            
            int totalFrames = columns * rows;
            for (int i = 0; i < totalFrames; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Frame {i + 1}:", GUILayout.Width(80));
                
                if (customFrameNames[i] == null) customFrameNames[i] = $"frame_{(i + 1):D4}";
                customFrameNames[i] = EditorGUILayout.TextField(customFrameNames[i]);
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();

            // Frame interval setting for Auto Fill
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Frame Interval:", GUILayout.Width(100));
            frameInterval = EditorGUILayout.IntField(frameInterval, GUILayout.Width(60));
            frameInterval = Mathf.Max(1, frameInterval); // Ensure minimum value of 1
            EditorGUILayout.LabelField($"(Every {frameInterval} frame{(frameInterval > 1 ? "s" : "")})", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            
            if (frameInterval > 1)
            {
                EditorGUILayout.HelpBox($"Example: Interval={frameInterval} → frame_0001, frame_{(1 + frameInterval):D4}, frame_{(1 + frameInterval * 2):D4}... + last frame", MessageType.Info);
            }

            // Quick fill buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Auto Fill Sequential"))
            {
                AutoFillSequentialFrames(totalFrames);
            }
            if (GUILayout.Button("Clear All"))
            {
                for (int i = 0; i < totalFrames; i++)
                {
                    customFrameNames[i] = "";
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();

        // Output Info
        EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);
        if (!string.IsNullOrEmpty(sourceFolderName))
        {
            EditorGUILayout.HelpBox($"Output files will be saved as:\n" +
                                   $"• {sourceFolderName}_N_Sheet.png\n" +
                                   $"• {sourceFolderName}_NE_Sheet.png\n" +
                                   $"• ... (all 8 directions)", MessageType.Info);
        }

        EditorGUILayout.Space();

        // Validation and Generate Button
        bool canGenerate = !string.IsNullOrEmpty(sourceFolderPath) && ValidateSourceFolder();
        
        GUI.enabled = canGenerate;
        if (GUILayout.Button("Generate Sprite Sheets", GUILayout.Height(40)))
        {
            GenerateSpriteSheets();
        }
        GUI.enabled = true;

        if (!canGenerate && !string.IsNullOrEmpty(sourceFolderPath))
        {
            EditorGUILayout.HelpBox("Selected folder must contain 8 direction subfolders (N, NE, E, SE, S, SW, W, NW)", MessageType.Warning);
        }
    }

    private void SelectSourceFolder()
    {
        string selectedPath = EditorUtility.OpenFolderPanel("Select Animation Folder", Application.dataPath, "");
        if (!string.IsNullOrEmpty(selectedPath))
        {
            // Convert absolute path to relative path from Assets folder
            if (selectedPath.StartsWith(Application.dataPath))
            {
                sourceFolderPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
            }
            else
            {
                sourceFolderPath = selectedPath;
            }

            // Extract folder name for output filename
            sourceFolderName = Path.GetFileName(sourceFolderPath);
        }
    }

    private bool ValidateSourceFolder()
    {
        if (string.IsNullOrEmpty(sourceFolderPath)) return false;

        foreach (string direction in directionNames)
        {
            string dirPath = Path.Combine(sourceFolderPath, direction);
            if (!Directory.Exists(dirPath))
            {
                return false;
            }
        }
        return true;
    }

    private void GenerateSpriteSheets()
    {
        if (!ValidateSourceFolder())
        {
            EditorUtility.DisplayDialog("Error", "Please select a valid source folder with 8 direction subfolders.", "OK");
            return;
        }

        try
        {
            int totalDirections = directionNames.Length;
            int currentDirection = 0;

            foreach (string direction in directionNames)
            {
                currentDirection++;
                EditorUtility.DisplayProgressBar("Generating Sprite Sheets", 
                    $"Processing {direction} direction... ({currentDirection}/{totalDirections})", 
                    (float)currentDirection / totalDirections);

                if (GenerateSpriteSheetForDirection(direction))
                {
                    Debug.Log($"Generated sprite sheet for direction: {direction}");
                }
                else
                {
                    Debug.LogWarning($"Failed to generate sprite sheet for direction: {direction}");
                }
            }

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Success", "All sprite sheets generated successfully!", "OK");
            AssetDatabase.Refresh();
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Error", $"Failed to generate sprite sheets: {e.Message}", "OK");
            Debug.LogError($"Sprite sheet generation error: {e}");
        }
    }

    private bool GenerateSpriteSheetForDirection(string direction)
    {
        string directionPath = Path.Combine(sourceFolderPath, direction);
        if (!Directory.Exists(directionPath)) return false;

        // Get frame names to process
        List<string> frameNames = GetFrameNames();
        if (frameNames.Count == 0) return false;

        // Load textures
        List<Texture2D> frameTextures = new List<Texture2D>();
        foreach (string frameName in frameNames)
        {
            string framePath = Path.Combine(directionPath, frameName + ".png");
            if (File.Exists(framePath))
            {
                // Convert to relative path for AssetDatabase
                string relativePath = framePath.Replace(Application.dataPath, "Assets");
                relativePath = relativePath.Replace("\\", "/");
                
                Texture2D texture = LoadTextureWithReadWriteEnabled(relativePath);
                if (texture != null)
                {
                    frameTextures.Add(texture);
                }
                else
                {
                    Debug.LogWarning($"Could not load texture: {relativePath}");
                    // Create empty texture as placeholder
                    frameTextures.Add(CreateEmptyTexture(frameWidth, frameHeight));
                }
            }
            else
            {
                Debug.LogWarning($"Frame not found: {framePath}");
                // Create empty texture as placeholder
                frameTextures.Add(CreateEmptyTexture(frameWidth, frameHeight));
            }
        }

        // Generate sprite sheet
        Texture2D spriteSheet = CreateSpriteSheet(frameTextures);
        if (spriteSheet == null) return false;

        // Save sprite sheet
        string outputFileName = $"{sourceFolderName}_{direction}_Sheet.png";
        string outputPath = Path.Combine(sourceFolderPath, outputFileName);
        
        byte[] pngData = spriteSheet.EncodeToPNG();
        File.WriteAllBytes(outputPath, pngData);

        // Cleanup
        Object.DestroyImmediate(spriteSheet);
        foreach (var tex in frameTextures)
        {
            if (tex.name == "EmptyFrame" || tex.name.EndsWith("_Copy")) // Destroy our created textures
            {
                Object.DestroyImmediate(tex);
            }
        }

        return true;
    }

    private List<string> GetFrameNames()
    {
        List<string> frameNames = new List<string>();
        int totalFrames = columns * rows;

        if (isSequentialMode)
        {
            for (int i = 1; i <= totalFrames; i++)
            {
                frameNames.Add($"frame_{i:D4}");
            }
        }
        else
        {
            for (int i = 0; i < totalFrames; i++)
            {
                if (!string.IsNullOrEmpty(customFrameNames[i]))
                {
                    frameNames.Add(customFrameNames[i]);
                }
            }
        }

        return frameNames;
    }

    private Texture2D CreateSpriteSheet(List<Texture2D> frameTextures)
    {
        int sheetWidth = frameWidth * columns;
        int sheetHeight = frameHeight * rows;
        
        Texture2D spriteSheet = new Texture2D(sheetWidth, sheetHeight, TextureFormat.RGBA32, false);
        
        // Fill with transparent pixels
        Color[] clearPixels = new Color[sheetWidth * sheetHeight];
        for (int i = 0; i < clearPixels.Length; i++)
        {
            clearPixels[i] = Color.clear;
        }
        spriteSheet.SetPixels(clearPixels);

        // Place frames in grid
        for (int i = 0; i < frameTextures.Count && i < (columns * rows); i++)
        {
            int x = i % columns;
            int y = rows - 1 - (i / columns); // Flip Y to match typical sprite sheet layout
            
            Texture2D frame = frameTextures[i];
            if (frame != null)
            {
                // Resize frame if necessary
                Texture2D resizedFrame = ResizeTexture(frame, frameWidth, frameHeight);
                
                // Copy pixels to sprite sheet
                Color[] framePixels = resizedFrame.GetPixels();
                spriteSheet.SetPixels(x * frameWidth, y * frameHeight, frameWidth, frameHeight, framePixels);
                
                // Cleanup resized texture if it's different from original
                if (resizedFrame != frame)
                {
                    Object.DestroyImmediate(resizedFrame);
                }
            }
        }

        spriteSheet.Apply();
        return spriteSheet;
    }

    private Texture2D ResizeTexture(Texture2D source, int width, int height)
    {
        if (source.width == width && source.height == height)
        {
            return source;
        }

        Texture2D resized = new Texture2D(width, height, TextureFormat.RGBA32, false);
        
        // Simple resize using nearest neighbor
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;
                float v = (float)y / height;
                
                int sourceX = Mathf.FloorToInt(u * source.width);
                int sourceY = Mathf.FloorToInt(v * source.height);
                
                sourceX = Mathf.Clamp(sourceX, 0, source.width - 1);
                sourceY = Mathf.Clamp(sourceY, 0, source.height - 1);
                
                Color pixel = source.GetPixel(sourceX, sourceY);
                resized.SetPixel(x, y, pixel);
            }
        }
        
        resized.Apply();
        return resized;
    }

    private Texture2D LoadTextureWithReadWriteEnabled(string assetPath)
    {
        // Get texture importer
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"Could not get TextureImporter for: {assetPath}");
            return null;
        }

        // Check if Read/Write is already enabled and set proper compression
        bool needsReimport = false;
        if (!importer.isReadable)
        {
            importer.isReadable = true;
            needsReimport = true;
        }
        
        // Set texture compression to None for better compatibility
        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            needsReimport = true;
        }

        // Apply import settings if changed
        if (needsReimport)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        // Load the texture
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (texture == null)
        {
            Debug.LogError($"Failed to load texture after setting Read/Write: {assetPath}");
            return null;
        }

        // Create a copy to avoid modifying the original asset
        Texture2D copy = new Texture2D(texture.width, texture.height, texture.format, false);
        try
        {
            copy.SetPixels(texture.GetPixels());
            copy.Apply();
            copy.name = texture.name + "_Copy";
            return copy;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to copy texture pixels: {assetPath}, Error: {e.Message}");
            Object.DestroyImmediate(copy);
            return null;
        }
    }

    private Texture2D CreateEmptyTexture(int width, int height)
    {
        Texture2D emptyTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        emptyTexture.name = "EmptyFrame";
        
        Color[] clearPixels = new Color[width * height];
        for (int i = 0; i < clearPixels.Length; i++)
        {
            clearPixels[i] = Color.clear;
        }
        
        emptyTexture.SetPixels(clearPixels);
        emptyTexture.Apply();
        
        return emptyTexture;
    }

    private void AutoFillSequentialFrames(int totalFrames)
    {
        if (totalFrames <= 0) return;

        // Get the actual last frame number from the source folder
        int lastFrameNumber = GetLastFrameNumber();
        
        if (totalFrames == 1)
        {
            // If only one frame, use the last frame
            customFrameNames[0] = $"frame_{lastFrameNumber:D4}";
        }
        else
        {
            // Fill frames with interval, ensuring last slot gets the actual last frame
            for (int i = 0; i < totalFrames - 1; i++)
            {
                int frameNumber = 1 + (i * frameInterval);
                customFrameNames[i] = $"frame_{frameNumber:D4}";
            }
            
            // Always put the actual last frame in the final slot
            customFrameNames[totalFrames - 1] = $"frame_{lastFrameNumber:D4}";
        }
    }

    private int GetLastFrameNumber()
    {
        if (string.IsNullOrEmpty(sourceFolderPath)) return 36; // Default fallback
        
        // Check the first available direction folder to find the highest frame number
        foreach (string direction in directionNames)
        {
            string dirPath = Path.Combine(sourceFolderPath, direction);
            if (Directory.Exists(dirPath))
            {
                var pngFiles = Directory.GetFiles(dirPath, "frame_*.png")
                                       .Select(Path.GetFileNameWithoutExtension)
                                       .Where(name => name.StartsWith("frame_"))
                                       .ToList();
                
                if (pngFiles.Count > 0)
                {
                    int maxFrameNumber = 0;
                    foreach (string fileName in pngFiles)
                    {
                        // Extract number from "frame_XXXX" format
                        string numStr = fileName.Substring(6); // Remove "frame_" prefix
                        if (int.TryParse(numStr, out int frameNum))
                        {
                            maxFrameNumber = Mathf.Max(maxFrameNumber, frameNum);
                        }
                    }
                    
                    if (maxFrameNumber > 0)
                    {
                        return maxFrameNumber;
                    }
                }
            }
        }
        
        return 36; // Default fallback if no frames found
    }
}
