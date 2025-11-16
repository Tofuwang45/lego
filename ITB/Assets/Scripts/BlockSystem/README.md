# VR Block Selection System for Quest 3
## Simplified Wrist Menu Implementation for Unity 6000.2.12f1

This is a streamlined VR block selection system designed for rapid deployment at hackathons. It provides a 3x3 grid of selectable LEGO blocks with undo functionality using Unity's MR Template wrist menu.

---

## Features

- **3x3 Block Grid**: Simple grid interface for selecting up to 9 different LEGO blocks
- **Wrist Menu Integration**: Uses Unity's built-in XR Interaction Toolkit hand menu system
- **One-Click Spawning**: Tap a block button to spawn it in front of you
- **Undo System**: Simple undo button to remove the last placed block
- **Quest 3 Optimized**: Built for Unity 6000.2.12f1 with modern XR/MR libraries

---

## Quick Setup Guide

### Step 1: Create a Block Catalog

1. In Unity, right-click in the Project window
2. Navigate to: **Create > Lego Builder > Block Catalog**
3. Name it `MyBlockCatalog`
4. Select the newly created asset
5. In the Inspector, assign up to 9 blocks:
   - **Block Name**: Display name (e.g., "2x4 Brick")
   - **Block Prefab**: Your LEGO block prefab
   - **Icon**: Optional sprite/icon for the button
   - **Button Color**: Fallback color if no icon is provided

### Step 2: Create the Wrist Menu UI

#### Option A: Use Existing Hand Menu (Recommended)

1. In your scene hierarchy, locate or create: **XR Origin (XR Rig)**
2. Add the **HandMenuRig** prefab as a child:
   - Find it at: `Assets/Samples/XR Interaction Toolkit/3.2.1/Hands Interaction Demo/Prefabs/HandMenuRig.prefab`
   - Or create a new GameObject named "HandMenuRig"

3. Create a **Canvas** as a child of the "Follow GameObject" in HandMenuRig:
   ```
   HandMenuRig
   └─ Follow GameObject
      └─ BlockSelectionCanvas (NEW)
   ```

4. Configure the Canvas:
   - **Render Mode**: World Space
   - **Width**: 400
   - **Height**: 500
   - **Scale**: 0.001, 0.001, 0.001
   - **Position**: (0, 0, 0.05) - slightly in front of wrist
   - **Rotation**: (0, 0, 0)

#### Option B: Create Custom World Space Canvas

If you don't have HandMenuRig, create a simple world-space canvas:

1. Create GameObject > UI > Canvas
2. Set Render Mode to **World Space**
3. Attach it to your left controller/hand
4. Scale to 0.001, 0.001, 0.001
5. Position at wrist: (0, 0, 0.1)

### Step 3: Build the UI Hierarchy

Create this hierarchy under your Canvas:

```
BlockSelectionCanvas
├─ Panel (Background)
│  └─ VerticalLayout
│     ├─ Title (TextMeshPro)
│     ├─ BlockGrid (GameObject)
│     │  ├─ GridLayoutGroup component
│     │  └─ BlockGridUI component
│     └─ UndoButton
│        ├─ Button component
│        └─ Text (TextMeshPro): "Undo"
└─ WristMenuController component
```

**Detailed Setup:**

1. **Panel** (Background):
   - Add Component: **Image** (semi-transparent background)
   - Add Component: **Vertical Layout Group**
     - Padding: 10, 10, 10, 10
     - Spacing: 15
     - Child Force Expand: Width & Height

2. **Title** (Optional):
   - Add Component: **TextMeshProUGUI**
   - Text: "Select Block"
   - Font Size: 24
   - Alignment: Center

3. **BlockGrid**:
   - Add Component: **Grid Layout Group**
     - Cell Size: 100, 100
     - Spacing: 10, 10
     - Constraint: Fixed Column Count = 3
   - Add Component: **BlockGridUI** (our script)
     - Assign **Block Catalog** (from Step 1)
     - Assign **Block Button Prefab** (create in next step)
     - Assign **Button Container**: this GameObject
     - Assign **Grid Layout**: this Grid Layout Group component

4. **UndoButton**:
   - Add Component: **Button**
   - Add child: **Text** (TextMeshProUGUI): "Undo"
   - Size: 380 x 80

### Step 4: Create the Block Button Prefab

1. Create a new GameObject: **BlockButtonPrefab**
2. Add Component: **Button**
3. Add Component: **BlockButton** (our script)
4. Create this child hierarchy:

```
BlockButtonPrefab
├─ Background (Image) - ButtonBackground reference
├─ Icon (Image) - IconImage reference
└─ Name (TextMeshProUGUI) - NameText reference (optional)
```

5. Configure the BlockButton component:
   - **Button**: Drag the Button component
   - **Icon Image**: Drag the Icon Image
   - **Name Text**: Drag the Name TextMeshProUGUI (optional)
   - **Background Image**: Drag the Background Image

6. **Save as Prefab**:
   - Drag BlockButtonPrefab into your Project window
   - Delete from hierarchy
   - Assign this prefab to **BlockGridUI > Block Button Prefab**

### Step 5: Add System Components

1. Create an empty GameObject in your scene: **"BlockSystems"**
2. Add these components:
   - **BlockSpawner**
     - Main Camera: Assign your Main Camera
     - Spawn Distance: 0.5 (meters in front of user)
     - Make Interactable: ✓ (checked)
     - Block Scale: 0.1 (adjust as needed)

   - **SimpleUndoSystem**
     - Max Undo History: 20

3. On your **Canvas**, add component:
   - **WristMenuController**
     - Undo Button: Assign your Undo Button
     - Block Grid UI: Assign your BlockGrid GameObject's BlockGridUI component

### Step 6: Configure XR Origin

Ensure your XR Origin has:

1. **XRInteractionManager** in the scene
2. **Event System** with **XR UI Input Module**
3. Left and Right hand controllers with:
   - **XR Ray Interactor** (for UI interaction)
   - **XR Direct Interactor** (for grabbing blocks)

### Step 7: Test in Unity

1. Enter Play Mode (preferably with XR Device Simulator or actual Quest 3)
2. Look at your left wrist - the menu should appear
3. Point your right controller ray at a block button
4. Click/select to spawn a block in front of you
5. Click Undo to remove the last block

---

## Script Reference

### Core Scripts

| Script | Purpose |
|--------|---------|
| `BlockData.cs` | Data structure for a single block type |
| `BlockCatalog.cs` | ScriptableObject holding up to 9 blocks |
| `BlockSpawner.cs` | Singleton that spawns blocks in VR space |
| `SimpleUndoSystem.cs` | Singleton that tracks and undos placements |
| `BlockButton.cs` | UI button that spawns a specific block |
| `BlockGridUI.cs` | Manages the 3x3 grid of buttons |
| `WristMenuController.cs` | Main controller for wrist menu interactions |

### Key Classes

#### BlockData
```csharp
public string blockName;        // Display name
public GameObject blockPrefab;  // Prefab to spawn
public Sprite icon;            // Button icon (optional)
public Color buttonColor;      // Button color if no icon
```

#### BlockCatalog
```csharp
// Create via: Create > Lego Builder > Block Catalog
public BlockData[] Blocks;     // Max 9 blocks
public BlockData GetBlock(int index);
```

#### BlockSpawner
```csharp
public static BlockSpawner Instance;
public GameObject SpawnBlock(BlockData blockData);
public void SetSpawnDistance(float distance);
```

#### SimpleUndoSystem
```csharp
public static SimpleUndoSystem Instance;
public void RegisterSpawnedObject(GameObject obj);
public bool Undo();
public bool CanUndo;
public int UndoCount;
```

---

## Customization

### Changing Spawn Distance
```csharp
BlockSpawner.Instance.SetSpawnDistance(1.0f); // 1 meter
```

### Changing Grid Size
Edit `BlockGridUI` in Inspector:
- Columns: 3 (for 3x3)
- Cell Size: Adjust button size
- Spacing: Adjust gap between buttons

### Making Blocks Non-Grabbable
Uncheck **Make Interactable** on BlockSpawner component.

### Adding More Blocks
Create a new BlockCatalog with more entries, but only the first 9 will display.

---

## Troubleshooting

### Blocks Not Spawning
- ✅ Check BlockCatalog has prefabs assigned
- ✅ Verify BlockSpawner has Main Camera assigned
- ✅ Check Console for errors

### UI Not Visible
- ✅ Ensure Canvas is World Space
- ✅ Check Canvas scale is 0.001, 0.001, 0.001
- ✅ Verify layer is set to UI (Layer 5)
- ✅ Check XRUIInputModule is on EventSystem

### Can't Click Buttons
- ✅ Ensure XR Ray Interactor is on controller
- ✅ Check Canvas has Graphic Raycaster
- ✅ Verify buttons have Image component
- ✅ Check interaction layers match

### Undo Not Working
- ✅ Verify SimpleUndoSystem exists in scene
- ✅ Check spawned objects are being registered
- ✅ Look for Console errors

### Blocks Too Big/Small
Adjust **Block Scale** on BlockSpawner (default: 0.1)

### Menu Not Following Wrist
- ✅ Ensure HandMenuRig is properly configured
- ✅ Check TrackedPoseDriver on palm anchors
- ✅ Verify hand tracking is enabled on Quest 3

---

## Performance Tips

1. **Limit Undo History**: Set to 10-15 for better performance
2. **Optimize Block Prefabs**: Keep poly count low, use simple colliders
3. **Use Object Pooling**: For advanced users, implement pooling in BlockSpawner
4. **Disable Unused Features**: Remove optional UI elements if not needed

---

## Integration with Existing Code

### Spawn from External Script
```csharp
using LegoBuilder.BlockSystem;

BlockData customBlock = new BlockData {
    blockName = "Custom Block",
    blockPrefab = myPrefab
};

GameObject spawned = BlockSpawner.Instance.SpawnBlock(customBlock);
```

### Listen for Spawns
Modify `BlockSpawner.SpawnBlock()` to invoke an event:
```csharp
public UnityEvent<GameObject> OnBlockSpawned;

// In SpawnBlock:
OnBlockSpawned?.Invoke(spawnedBlock);
```

---

## Quest 3 Specific Settings

### Build Settings
1. **Platform**: Android
2. **Texture Compression**: ASTC
3. **Minimum API Level**: Android 10.0 (API 29)
4. **Target API Level**: Android 13.0 (API 33)

### XR Plugin Management
1. Enable **Oculus** (Meta Quest)
2. Oculus Settings:
   - Stereo Rendering Mode: Multiview
   - Target Devices: Quest 3

### Project Settings
- **Color Space**: Linear (recommended for VR)
- **Graphics API**: Vulkan (first), OpenGLES3
- **Rendering**: URP (Universal Render Pipeline)

---

## File Structure

```
Assets/
└─ Scripts/
   └─ BlockSystem/
      ├─ BlockData.cs
      ├─ BlockCatalog.cs
      ├─ BlockSpawner.cs
      ├─ SimpleUndoSystem.cs
      ├─ BlockButton.cs
      ├─ BlockGridUI.cs
      ├─ WristMenuController.cs
      └─ README.md (this file)
```

---

## License

This implementation is provided as-is for hackathon use. Feel free to modify and extend for your project.

---

## Support

For issues or questions:
1. Check Console for error messages
2. Verify all Inspector references are assigned
3. Test with XR Device Simulator first
4. Ensure Quest 3 has hand tracking enabled

---

**Happy Building! 🧱**
