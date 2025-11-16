# VR Forearm Slate UI Setup Guide

This guide explains how to set up and use the VR Forearm Slate UI system for your Lego builder application.

## 📁 File Locations

All VR UI system files are organized in:
```
ITB/Assets/MRTemplateAssets/VRUISystem/
├── Scripts/
│   ├── Core/               # ForearmSlateUI, BlockUsageTracker, UndoSystem
│   ├── Data/               # BlockCatalogData
│   ├── UI/                 # TabSystem, GridLayoutManager, BlockButton, etc.
│   ├── Interaction/        # DeleteMode
│   └── Features/           # AIGuideButton
├── Prefabs/                # Unity prefabs (create in editor)
├── Materials/              # Materials for ghost preview, highlights
└── Documentation/          # This file and implementation summary
```

See `../README.md` for detailed folder structure explanation.

## Overview

The VR UI system implements a professional, ergonomic interface for VR Lego building based on best practices:

- **Forearm Slate**: A tablet-like UI attached to your left forearm for block selection
- **Tabbed Categories**: Easy navigation through different block types (Bricks, Plates, Slopes, etc.)
- **Ghost Preview**: See blocks before placing them
- **Stats Panel**: Track usage statistics with expandable view
- **Recents Hotbar**: Quick access to recently used blocks
- **Delete Mode**: Toggle deletion mode with visual feedback
- **Undo System**: Undo last actions
- **AI Guide**: Button to trigger AI building suggestions

## Architecture

### Core Components

1. **BlockCatalogData.cs** - ScriptableObject that stores all available Lego blocks
2. **BlockUsageTracker.cs** - Singleton that tracks block placement statistics
3. **ForearmSlateUI.cs** - Main controller that attaches UI to left hand
4. **TabSystem.cs** - Manages category tab switching
5. **GridLayoutManager.cs** - Creates 3x3 grid of block buttons
6. **BlockButton.cs** - Interactive block selection with ghost preview
7. **RecentsManager.cs** - Manages hotbar of recent blocks
8. **StatsPanel.cs** - Displays usage statistics
9. **DeleteMode.cs** - Handles block deletion functionality
10. **UndoSystem.cs** - Manages undo/redo operations
11. **AIGuideButton.cs** - Triggers AI assistance

## Setup Instructions

### Step 1: Create Block Catalog

1. In Unity, right-click in your Project window
2. Navigate to **Create → VR Lego → Block Catalog Data**
3. Name it "BlockCatalog"
4. Fill in the block definitions:
   ```
   - Block Name: "2x4 Brick"
   - Block ID: "brick_2x4"
   - Prefab: [Drag your Lego prefab here]
   - Icon: [Assign a sprite icon]
   - Category: Bricks
   - Default Scale: (1, 1, 1)
   - Available Colors: [Add colors like Red, Blue, Yellow]
   ```

### Step 2: Create Manager GameObjects

1. Create an empty GameObject named "VR_UI_Managers"
2. Add the following components:
   - **BlockUsageTracker**
   - **UndoSystem**

### Step 3: Create the Forearm Slate Canvas

1. Create a new Canvas (Right-click → UI → Canvas)
2. Name it "ForearmSlateCanvas"
3. Add the **ForearmSlateUI** component
4. Configure the canvas:
   ```
   Canvas:
   - Render Mode: World Space
   - Scale: (0.001, 0.001, 0.001)
   - Rect Transform Width: 200
   - Rect Transform Height: 150

   ForearmSlateUI:
   - Left Hand Controller: [Assign from XR Origin]
   - Position Offset: (0.1, 0.05, 0.1)
   - Rotation Offset: (45, 0, 0)
   - Block Catalog: [Assign your BlockCatalog asset]
   - Right Hand Ray Interactor: [Assign from XR Origin]
   ```

### Step 4: Create Tab Bar

1. Inside ForearmSlateCanvas, create a Panel named "TabBar"
2. Add a Horizontal Layout Group
3. Create buttons for each category:
   - Create Button → Name it "BricksTab"
   - Duplicate for "PlatesTab", "SlopesTab", etc.
4. Add **TabSystem** component to TabBar
5. Configure each tab in the TabSystem inspector:
   ```
   Tabs:
   - Category: Bricks
   - Button: [Assign BricksTab Button]
   - Label: [Assign TextMeshPro component]
   - Background: [Assign Button Image]
   ```

### Step 5: Create Grid Container

1. Inside ForearmSlateCanvas, create a Panel named "GridContainer"
2. Add a **Grid Layout Group**:
   ```
   - Cell Size: (60, 60)
   - Spacing: (5, 5)
   - Constraint: Fixed Column Count
   - Constraint Count: 3
   ```
3. Add **GridLayoutManager** component
4. Configure:
   ```
   - Grid Container: [Self reference]
   - Block Button Prefab: [Will create next]
   - Rows: 3
   - Columns: 3
   ```

### Step 6: Create Block Button Prefab

1. Create a Button in a temporary location
2. Name it "BlockButtonPrefab"
3. Structure:
   ```
   BlockButtonPrefab (Button)
   ├── Icon (Image)
   ├── Name (TextMeshPro)
   └── ColorDots (Empty Transform)
       └── ColorDotPrefab (Button with Image)
   ```
4. Add **BlockButton** component
5. Assign references:
   ```
   - Icon Image: [Assign Icon]
   - Name Label: [Assign Name TextMeshPro]
   - Button: [Self Button]
   - Color Dots Container: [Assign ColorDots]
   - Color Dot Prefab: [Create a small button for color selection]
   - Ghost Distance: 0.3
   ```
6. Drag to Project to create prefab
7. Delete from scene

### Step 7: Create Recents Bar

1. Inside ForearmSlateCanvas, create a Panel named "RecentsBar"
2. Add a Horizontal Layout Group
3. Add **RecentsManager** component
4. Configure:
   ```
   - Recents Container: [Self reference]
   - Recent Block Button Prefab: [Use BlockButtonPrefab]
   - Max Recents: 6
   - Ray Interactor: [Assign right hand ray]
   ```

### Step 8: Create Stats Panel

1. Create a new Canvas named "StatsPanelCanvas"
2. Structure:
   ```
   StatsPanelCanvas
   ├── Background Panel
   ├── TotalCountText (TextMeshPro)
   ├── TopBlocksContainer (Vertical Layout Group)
   ├── FullListContainer (Scroll View with Vertical Layout)
   └── ExpandButton (Button)
   ```
3. Add **StatsPanel** component
4. Configure:
   ```
   - Total Count Text: [Assign TotalCountText]
   - Top Blocks Container: [Assign TopBlocksContainer]
   - Full List Container: [Assign FullListContainer's Content]
   - Stat Line Prefab: [Create a TextMeshPro prefab]
   - Expand Button: [Assign ExpandButton]
   - Use Lazy Follow: true
   ```
5. Save as prefab

### Step 9: Add Delete Mode

1. On ForearmSlateCanvas, create a Toggle named "DeleteModeToggle"
2. Add **DeleteMode** component to ForearmSlateCanvas
3. Configure:
   ```
   - Delete Mode Toggle: [Assign the toggle]
   - Ray Interactor: [Assign right hand ray]
   - Delete Ray Color: Red
   - Normal Ray Color: Blue
   - Max Delete Distance: 10
   ```

### Step 10: Add AI Guide Button

1. On ForearmSlateCanvas, create a Button named "AIGuideButton"
2. Add **AIGuideButton** component
3. Configure:
   ```
   - Guide Button: [Self Button]
   - Button Label: [Assign TextMeshPro]
   - Loading Indicator: [Create a spinner GameObject]
   - API Endpoint: [Your AI API URL if applicable]
   ```

### Step 11: Wire Everything Together

1. On ForearmSlateCanvas → ForearmSlateUI component:
   ```
   - Tab System: [Assign TabBar's TabSystem]
   - Grid Manager: [Assign GridContainer's GridLayoutManager]
   - Recents Manager: [Assign RecentsBar's RecentsManager]
   - Stats Panel Prefab: [Assign StatsPanelCanvas prefab]
   ```

2. Make ForearmSlateCanvas a child of Left Hand Controller in your XR Origin:
   ```
   XR Origin
   └── Camera Offset
       └── LeftHand Controller
           └── ForearmSlateCanvas
   ```

## Usage

### Building in VR

1. **Select a Block**:
   - Look at your left forearm to see the block palette
   - Point your right hand ray at a tab to switch categories
   - Point at a block button to see a ghost preview
   - Click a color dot to select color
   - Pull trigger to spawn the block

2. **Use Recents**:
   - Recently used blocks appear in the top bar
   - Click them for quick access

3. **View Stats**:
   - Look at your left wrist to see the stats panel
   - Shows top 3 most used blocks
   - Click expand to see all blocks

4. **Delete Mode**:
   - Toggle the delete switch on the palette
   - Ray turns red
   - Point at a block and pull trigger to delete

5. **Undo**:
   - Click the Undo button to reverse last action

6. **AI Guide**:
   - Click the AI Guide button for building suggestions
   - Processing indicator shows while loading

## Customization

### Adding More Blocks

1. Open your BlockCatalog ScriptableObject
2. Expand "All Blocks" list
3. Click "+" to add a new entry
4. Fill in the block data

### Changing Colors

Edit the TabSystem component:
- Active Tab Color: Color for selected tab
- Inactive Tab Color: Color for unselected tabs

### Adjusting Position

Edit ForearmSlateUI component:
- Position Offset: Move the slate
- Rotation Offset: Rotate the slate
- Slate Scale: Change overall size

## Troubleshooting

### Slate not appearing
- Ensure ForearmSlateCanvas is a child of LeftHand Controller
- Check that Canvas Render Mode is "World Space"
- Verify scale is set correctly (0.001, 0.001, 0.001)

### Ray not interacting with UI
- Add a "Tracked Device Graphic Raycaster" to your Canvas
- Ensure right hand has an XR Ray Interactor component
- Check that UI elements have the correct layer

### Ghost preview not showing
- Verify Ghost Material is assigned in BlockButton
- Check that Block Prefab has renderers
- Ensure Ray Interactor is assigned

### Blocks not tracking stats
- Verify BlockUsageTracker GameObject exists in scene
- Check that it's not disabled
- Ensure BlockButton calls RecordBlockPlacement

## Performance Tips

1. **Limit ghost preview quality**: Use low-poly models for ghost previews
2. **Optimize UI**: Use sprite atlases for icons
3. **Lazy loading**: Load stats panel only when needed
4. **Pool objects**: Consider object pooling for frequently spawned blocks

## Best Practices

1. **Ergonomics**: Test positioning with actual VR headset
2. **Readability**: Use large, clear icons and text (minimum 16pt)
3. **Feedback**: Provide visual/haptic feedback for all interactions
4. **Performance**: Keep UI updates to < 16ms per frame
5. **Accessibility**: Support both hand tracking and controllers

## API Reference

### BlockCatalogData

```csharp
public List<BlockData> GetBlocksByCategory(BlockCategory category)
public BlockData GetBlockById(string blockId)
```

### BlockUsageTracker

```csharp
public void RecordBlockPlacement(string blockId, string blockName, Color color)
public List<BlockUsage> GetTopUsedBlocks(int count = 3)
public List<BlockUsage> GetRecentBlocks()
public int GetTotalBlockCount()
```

### UndoSystem

```csharp
public void RecordPlacement(GameObject placedObject)
public void RecordDeletion(GameObject deletedObject)
public void Undo()
public bool CanUndo()
```

### ForearmSlateUI

```csharp
public void SetSlateActive(bool active)
public void ToggleSlate()
```

### DeleteMode

```csharp
public void SetDeleteMode(bool active)
```

## Credits

Based on VR UI best practices:
- Forearm Slate pattern (from Oculus/Meta design guidelines)
- Ghost preview technique (from Unity XR Interaction Toolkit)
- Lazy Follow positioning (from SteamVR)

## License

This UI system is part of your VR Lego Builder project.
