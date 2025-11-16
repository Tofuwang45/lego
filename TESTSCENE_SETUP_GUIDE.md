# TestScene Setup Guide

## Overview

The TestScene has been created with all essential MR components needed to run an interactive lego brick experience immediately. This guide explains what's included and how to add the UI and History features.

---

## What's Included in TestScene

The scene is ready to build and deploy with these components:

### Core MR Components

1. **XR Origin** - The player's position and tracking origin
   - Location: Root of scene
   - Contains: Camera Offset and Main Camera
   - Component: `XROrigin` - manages tracking space
   - Component: `HandSubsystemManager` - enables hand tracking
   - Component: `InputActionManager` - manages XR input actions

2. **Main Camera** - The player's view
   - Location: Under XR Origin → Camera Offset
   - Component: `Camera` - renders the scene (Clear Flags: Solid Color with alpha 0 for passthrough)
   - Component: `TrackedPoseDriver` - tracks headset position/rotation
   - Component: `AudioListener` - handles spatial audio

3. **XR Interaction Manager** - Manages all XR interactions
   - Location: Root of scene
   - Component: `XRInteractionManager` - coordinates all interactors and interactables

4. **Interactive Lego Brick** - A grabbable lego brick
   - Location: Positioned at (0, 1.5, 0.5) in front of player
   - Component: `XRGrabInteractable` - makes it grabbable with hands
   - Component: `Rigidbody` - physics simulation (gravity off, mass 0.1)
   - Component: `BoxCollider` - collision detection
   - Model: References Legos.fbx from LegoBricks folder

5. **Directional Light** - Scene lighting
   - Location: Root of scene
   - Provides consistent lighting for the scene

---

## How to Build and Run

### Quick Start

1. **Open the Scene**
   - In Unity, navigate to: `Assets/Scenes/TestScene.unity`
   - Double-click to open it

2. **Configure Build Settings**
   - Go to: File → Build Settings
   - Ensure TestScene is in the build scenes list
   - Platform should be set to Android (for Meta Quest) or your target MR platform

3. **Build and Deploy**
   - Connect your MR headset (Meta Quest 2/3/Pro)
   - Click "Build And Run"
   - Put on the headset and you should see a lego brick floating in front of you
   - Use your hands to grab and move the brick

### What You Can Do

- **Grab the brick**: Reach out with your hand and pinch when your hand touches the brick
- **Move the brick**: While grabbing, move your hand to reposition it
- **Release the brick**: Release the pinch gesture to let go

---

## Adding UI Features

The codebase includes complete UI systems. Here's how to add them to TestScene:

### Option 1: Add Hand Menu (Recommended for MR)

1. **Add the Hand Menu Prefab**
   ```
   Location: Assets/MRTemplateAssets/Prefabs/UI/HandMenuSetupVariant.prefab
   ```
   - Drag this prefab into your scene hierarchy
   - It will automatically attach to the user's hand

2. **What You Get**
   - Hand-anchored menu that follows your palm
   - Buttons for common actions
   - Toggle switches for features
   - Pre-configured UI interactions

3. **Customize the Menu** (Optional)
   - Select HandMenuSetupVariant in hierarchy
   - Navigate to child objects to find UI elements
   - Modify button text, icons, or add new buttons
   - Wire button events to your custom scripts

### Option 2: Add Tutorial/Coaching UI

1. **Add the GoalManager**
   ```
   Script Location: Assets/MRTemplateAssets/Scripts/GoalManager.cs
   ```
   - Create an empty GameObject in the scene
   - Name it "Tutorial Manager"
   - Add Component → GoalManager

2. **Add the CoachingUI Prefab**
   ```
   Location: Assets/MRTemplateAssets/Prefabs/UI/CoachingUI.prefab
   ```
   - Drag into scene hierarchy
   - This provides tutorial overlays and instructions

3. **Configure Tutorial Steps**
   - Select the Tutorial Manager object
   - In GoalManager component:
     - Set Goal 0: "Find a surface" (encourages plane detection)
     - Set Goal 1: "Tap to place brick" (teaches interaction)
     - Set Goal 2: "Tutorial complete" (completion message)

4. **Wire Events**
   - Connect GoalManager events to CoachingUI
   - Example: When goal completes → Show next instruction

### Option 3: Add Custom UI Canvas

1. **Create World Space Canvas**
   - Right-click in Hierarchy → UI → Canvas
   - Set Render Mode to "World Space"
   - Position it in front of the player (e.g., position: 0, 1.5, 2)
   - Scale: Set to 0.001 for all axes (makes it a reasonable size)

2. **Add UI Elements**
   - Right-click on Canvas → UI → Button (for buttons)
   - Right-click on Canvas → UI → Text (for text displays)
   - Customize as needed

3. **Make UI Interactable in XR**
   - Select the Canvas
   - Add Component → `XR UI Input Module` (from XR Interaction Toolkit)
   - Add Component → `Tracked Device Graphic Raycaster`
   - Now buttons will respond to hand pokes and controller rays

---

## Adding History/Persistence Features

The codebase includes a complete persistence system. Here's how to add it:

### Step 1: Add AR Plane Detection (Required for Anchors)

1. **Add ARFeatureController**
   ```
   Script Location: Assets/MRTemplateAssets/Scripts/ARFeatureController.cs
   ```
   - Create empty GameObject: "AR Manager"
   - Add Component → ARFeatureController
   - This enables plane detection, passthrough, and occlusion

2. **Configure Features**
   - In ARFeatureController inspector:
     - Enable Plane Detection: ✓
     - Enable Passthrough: ✓
     - Enable Occlusion: ✓ (optional, for realistic depth)

### Step 2: Add Object Spawning and Tracking

1. **Add SpawnedObjectsManager**
   ```
   Script Location: Assets/MRTemplateAssets/Scripts/SpawnedObjectsManager.cs
   ```
   - Create empty GameObject: "Object Manager"
   - Add Component → SpawnedObjectsManager

2. **Configure the Manager**
   - **Spawned Objects Parent**: Create an empty GameObject called "Spawned Objects Container" and assign it
   - **Object Prefab**: Drag your Interactive Lego Brick prefab here
   - **Save File Name**: "LegoSaveData" (or custom name)

3. **Add UI for Spawning** (Optional but recommended)
   - Add a button to your UI (see UI section above)
   - Button OnClick → SpawnedObjectsManager.SpawnObject()
   - This lets users spawn new bricks

### Step 3: Add Save/Load UI

1. **Create Save/Load Buttons**
   - Add buttons to your canvas:
     - "Save All Bricks"
     - "Load Saved Bricks"
     - "Clear All Bricks"

2. **Wire Button Events**
   ```
   Save Button OnClick → SpawnedObjectsManager.SaveSpawnedObjects()
   Load Button OnClick → SpawnedObjectsManager.LoadSpawnedObjects()
   Clear Button OnClick → SpawnedObjectsManager.DeleteAllSavedObjects()
   ```

3. **Add Status Display** (Optional)
   - Add a Text element to show saved object count
   - In SpawnedObjectsManager:
     - Find the "Saved Objects Text" field
     - Assign your Text component
   - This will auto-update to show "Saved: X objects"

### Step 4: Test Persistence

1. **Build and Run**
   - Deploy to your headset
   - Spawn some bricks and position them
   - Tap "Save All Bricks"

2. **Verify Save**
   - Close the app completely
   - Reopen the app
   - Tap "Load Saved Bricks"
   - Your bricks should reappear in the same positions!

---

## Advanced: Making Bricks Persistent on Spawn

If you want every brick to automatically save its position when placed:

1. **Modify the Lego Brick Prefab**
   - Select the Interactive Lego Brick in the scene
   - Add Component → `ARAnchor` (from AR Foundation)
   - This anchors it to the real world

2. **Auto-Save on Placement**
   - Create a custom script or modify SpawnedObjectsManager
   - Subscribe to the XRGrabInteractable.selectExited event
   - When the user releases the brick, call SaveSpawnedObjects()

Example code snippet:
```csharp
public class AutoSaveBrick : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;
    private SpawnedObjectsManager objectManager;

    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        objectManager = FindObjectOfType<SpawnedObjectsManager>();

        grabInteractable.selectExited.AddListener(OnReleased);
    }

    void OnReleased(SelectExitEventArgs args)
    {
        // Auto-save when user releases the brick
        objectManager?.SaveSpawnedObjects();
    }
}
```

---

## Prefab Reference

Here are the key prefabs you can drag into TestScene:

### UI Prefabs
- `HandMenuSetupVariant.prefab` - Hand-anchored menu
- `CoachingUI.prefab` - Tutorial overlay
- `Tap Visualization.prefab` - Visual feedback for taps
- `MainMenuUI.prefab` - Full-screen menu

### Interaction Prefabs
- `XR Controller Left.prefab` - Left hand controller visuals
- `XR Controller Right.prefab` - Right hand controller visuals
- `MR Interaction Setup.prefab` - Complete XR rig with hands

### Object Prefabs
- `Legos.fbx` - The lego brick model

All prefabs are located in: `Assets/MRTemplateAssets/Prefabs/`

---

## Script Reference

Key scripts for extending functionality:

### Core MR Scripts
- `HandSubsystemManager.cs` - Manages hand tracking
- `ARFeatureController.cs` - Controls AR features (planes, passthrough, occlusion)
- `OcclusionManager.cs` - Handles depth occlusion

### Persistence Scripts
- `SpawnedObjectsManager.cs` - Tracks and saves/loads objects
- `SaveAndLoadAnchorDataToFile.cs` - Low-level file persistence

### UI Scripts
- `GoalManager.cs` - Tutorial progression system
- `CoachingUI.cs` - Tutorial UI controller

All scripts are located in: `Assets/MRTemplateAssets/Scripts/`

---

## Troubleshooting

### Brick isn't grabbable
- Verify XR Interaction Manager is in the scene
- Check that the brick has XRGrabInteractable component
- Ensure the brick has a Collider (BoxCollider)
- Make sure hand tracking is enabled (HandSubsystemManager on XR Origin)

### Can't see the brick
- Check if the brick is positioned in front of the camera (0, 1.5, 0.5)
- Verify camera Clear Flags are set correctly
- Make sure the brick has a MeshRenderer component

### Save/Load not working
- Verify ARAnchor components are present on saved objects
- Check that ARFeatureController is in scene and enabled
- Ensure plane detection is enabled
- Check file permissions on device

### UI buttons not responding
- Add XR UI Input Module to Canvas
- Add Tracked Device Graphic Raycaster to Canvas
- Set Canvas to World Space render mode
- Check that EventSystem is in the scene

---

## Next Steps

1. **Open TestScene** in Unity
2. **Build and test** the basic grabbable brick
3. **Add UI** following the instructions above
4. **Add persistence** following the instructions above
5. **Customize** the experience to your needs

The scene is production-ready and can be extended with any of the features documented above. All components are modular and can be added incrementally.

---

## Additional Resources

- Unity XR Interaction Toolkit documentation
- AR Foundation documentation
- Meta Quest development guides

For issues or questions, refer to the codebase analysis documents:
- `CODEBASE_ANALYSIS.md` - Detailed technical analysis
- `QUICK_REFERENCE.md` - Quick code snippets and locations
- `PROJECT_SUMMARY.md` - High-level overview
