# Build History System - Implementation Guide

## Overview

This system tracks VR brick assembly in real-time, supporting complex scenarios like "bridging" where one brick connects to multiple bricks below. The data can be exported to generate AI-powered assembly instructions.

## System Architecture

### Core Components

1. **BrickIdentifier.cs** - Assigns unique IDs to each brick
2. **BuildStep.cs** - Data structure for a single assembly step
3. **BuildHistoryManager.cs** - Singleton that manages the complete build history
4. **BrickScanner.cs** - Detects multi-brick connections using raycasting
5. **SocketEventLogger.cs** - Bridges XR Interaction Toolkit with the history system
6. **BuildHistoryUI.cs** - (Optional) UI for displaying and exporting history

## Step-by-Step Setup

### Step 1: Prepare Your Brick Prefabs

Each brick prefab needs the following components:

1. **Add BrickIdentifier**
   - Attach `BrickIdentifier.cs` to your brick prefab
   - Configure properties:
     - `studsLength`: Number of studs along length (e.g., 2 for a 2x1 brick)
     - `studsWidth`: Number of studs along width (e.g., 1 for a 2x1 brick)
     - `brickColor`: Color name (e.g., "Red", "Blue")
   - The `uniqueID` will be auto-generated at runtime

2. **Add BrickScanner**
   - Attach `BrickScanner.cs` to your brick prefab
   - Configure tube points (two methods):

   **Method A: Manual Setup (Recommended)**
   - Create empty GameObjects as children of your brick
   - Name them "TubePoint_0", "TubePoint_1", etc.
   - Position them at the bottom of each stud/tube location
   - Add these transforms to the `bottomTubePoints` list

   **Method B: Auto-Generation (Quick Start)**
   - Leave `bottomTubePoints` empty
   - In the Inspector, right-click the BrickScanner component
   - Select "Create Tube Points From Dimensions"
   - This generates points based on `studsLength` and `studsWidth`

3. **Configure XR Components**
   - Ensure your brick has `XRGrabInteractable` for grabbing
   - Add colliders for physics interaction
   - Set appropriate layers (important for raycasting)

### Step 2: Set Up Sockets

For each socket (attachment point on top of bricks):

1. **Add XRSocketInteractor**
   - Create a child GameObject named "Socket" on top of each stud
   - Add `XRSocketInteractor` component
   - Configure interaction layers (crucial - prevents sockets from grabbing hands!)
   - Set socket position at the stud top

2. **Add SocketEventLogger**
   - Attach `SocketEventLogger.cs` to the same GameObject with XRSocketInteractor
   - The script will auto-detect the socket component
   - Configure:
     - `scanDelay`: 0.1 seconds (allows physics to settle)
     - `buildSpaceReference`: Optional - assign your build platform transform

### Step 3: Create the Build History Manager

1. **Create Manager GameObject**
   - In your scene, create an empty GameObject named "BuildHistoryManager"
   - Attach `BuildHistoryManager.cs`
   - The script uses singleton pattern, so it will persist across scenes

2. **Configure Settings**
   - `maxHistorySize`: 0 for unlimited, or set a limit
   - `enableDebugLog`: Check for development, uncheck for production

### Step 4: (Optional) Add UI Display

If you want to visualize the history in VR:

1. **Create a Canvas**
   - Add a World Space Canvas to your scene
   - Position it where you want the display

2. **Add UI Elements**
   - Create a TextMeshProUGUI for history display
   - Create buttons for:
     - Refresh Display
     - Generate AI Prompt
     - Clear History
   - Create another TextMeshProUGUI for AI prompt output

3. **Attach BuildHistoryUI**
   - Add `BuildHistoryUI.cs` to your Canvas
   - Link all UI elements in the Inspector
   - Set `autoUpdateInterval` (e.g., 2 seconds) or 0 to disable

## How It Works

### The "Stud Scan" Algorithm

When a brick snaps into a socket:

1. **Physics Connection**: The XRSocketInteractor physically attaches the brick
2. **Event Trigger**: `SocketEventLogger` receives `OnSocketSnap` event
3. **Delay**: System waits briefly (0.1s) for physics to settle
4. **Scan**: `BrickScanner` casts rays down from each tube point
5. **Detection**: Rays detect all bricks below (solving the "bridge scenario")
6. **Logging**: A new `BuildStep` is created with:
   - Brick ID and name
   - List of ALL connected parent brick IDs
   - Position and rotation data
7. **Storage**: `BuildHistoryManager` adds the step to the history

### Bridge Scenario Example

```
Scenario: Placing a 3x1 brick across three 1x1 bricks

Step 1: Place 1x1_Red at position (0, 0, 0)
  - connectedParentIDs: [] (foundation)

Step 2: Place 1x1_Green at position (0.008, 0, 0)
  - connectedParentIDs: [] (foundation)

Step 3: Place 1x1_Blue at position (0.016, 0, 0)
  - connectedParentIDs: [] (foundation)

Step 4: Place 3x1_Yellow across all three
  - connectedParentIDs: [ID_Red, ID_Green, ID_Blue]
  - Description: "Bridged 3x1_Yellow across bricks: ID_Red, ID_Green, ID_Blue"
```

## Exporting Data

### Generate AI Prompt

```csharp
string prompt = BuildHistoryManager.Instance.GenerateAIPrompt();
```

Example output:
```
Generate step-by-step assembly instructions for the following brick construction:

Build Steps:
1. Placed 2x4_Red as the foundation brick.
2. Attached 2x4_Blue on top of brick ID_123.
3. Bridged 3x1_Yellow across bricks: ID_123, ID_456, ID_789.

Please create clear, concise assembly instructions that a user could follow to recreate this construction.
```

### Export as JSON

```csharp
string json = BuildHistoryManager.Instance.ExportToJSON();
```

This creates a structured JSON with all build steps, suitable for API calls to OpenAI or other LLMs.

## Best Practices

### Performance

1. **Layer Masks**: Configure `brickLayerMask` on BrickScanner to only raycast against brick layers
2. **Scan Distance**: Keep `scanDistance` small (0.05 units) to minimize unnecessary raycasts
3. **Tube Points**: Only create tube points where actual connections occur

### Debugging

1. **Enable Debug Rays**: Check `showDebugRays` on BrickScanner to visualize raycasts in Scene view
2. **Enable Debug Logging**: Check `enableDebugLog` on both SocketEventLogger and BuildHistoryManager
3. **Test Scanning**: Right-click BrickScanner component → "Test Scan" to verify connections
4. **Print History**: Right-click BuildHistoryManager → "Print History" to see all steps

### Interaction Layers

Critical for preventing issues:

1. Create separate layers:
   - "Bricks" - for brick GameObjects
   - "Sockets" - for socket interactors
   - "Default" - for hands/controllers

2. Configure XRSocketInteractor:
   - Interaction Layer Mask: ONLY "Bricks" layer
   - This prevents sockets from trying to grab controllers

3. Configure XRGrabInteractable:
   - Interaction Layer Mask: Include "Default" for hand grabbing

## Troubleshooting

### "No connected bricks found"

- Check that brick colliders are on the correct layer
- Verify `brickLayerMask` includes the brick layer
- Increase `scanDistance` slightly
- Enable `showDebugRays` to see where rays are casting

### "Sockets grab my hands"

- Check XRSocketInteractor Interaction Layer Mask
- Ensure hands/controllers are NOT on the "Bricks" layer

### "History not updating"

- Verify BuildHistoryManager exists in scene
- Check that SocketEventLogger is attached to socket GameObjects
- Enable debug logging to see event triggers

### "Bridge detection misses some bricks"

- Verify tube points are correctly positioned
- Check that multiple bricks have proper colliders
- Increase tube point count or adjust positions

## Advanced: Integration with OpenAI API

To send build history to OpenAI for guide generation:

```csharp
using UnityEngine.Networking;
using System.Collections;

public class OpenAIIntegration : MonoBehaviour
{
    private string apiKey = "your-openai-api-key";
    private string apiUrl = "https://api.openai.com/v1/chat/completions";

    public void GenerateGuide()
    {
        StartCoroutine(SendToOpenAI());
    }

    private IEnumerator SendToOpenAI()
    {
        // Get the prompt from history manager
        string prompt = BuildHistoryManager.Instance.GenerateAIPrompt();

        // Create request payload
        string jsonPayload = $@"{{
            ""model"": ""gpt-4"",
            ""messages"": [
                {{""role"": ""user"", ""content"": ""{prompt}""}}
            ]
        }}";

        // Send request
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("AI Response: " + request.downloadHandler.text);
            // Parse and display the response
        }
        else
        {
            Debug.LogError("Error: " + request.error);
        }
    }
}
```

## Summary

Your build history system is now ready! The key components work together to:

1. **Track** every brick placement automatically
2. **Detect** complex multi-brick connections (bridge scenario)
3. **Store** chronological build data
4. **Export** formatted prompts for AI guide generation

For a 24-hour hackathon, this system provides the foundation needed for Pillar 3 (State Tracking) of your VR Assembly Simulator.
