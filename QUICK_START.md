# TestScene Quick Start

## Immediate Build Instructions

### 1. Open Scene
- Unity → Assets/Scenes/TestScene.unity

### 2. Build Settings
- File → Build Settings
- Platform: Android
- Add TestScene to build

### 3. Deploy
- Connect Meta Quest
- Build And Run

### 4. Experience
- Put on headset
- See floating lego brick
- Pinch with hand to grab
- Move hand to reposition
- Release pinch to drop

---

## What's Working Right Now

✅ XR Origin with hand tracking
✅ Main camera with passthrough support
✅ Interactive lego brick (grab/move/release)
✅ Physics simulation
✅ XR Interaction system

---

## Add UI in 5 Minutes

### Method 1: Hand Menu (Easiest)
1. Drag `Assets/MRTemplateAssets/Prefabs/UI/HandMenuSetupVariant.prefab` into scene
2. Done! Menu attaches to your palm

### Method 2: Tutorial System
1. Create empty GameObject → Add `GoalManager` component
2. Drag `Assets/MRTemplateAssets/Prefabs/UI/CoachingUI.prefab` into scene
3. Configure tutorial steps in GoalManager

---

## Add Persistence in 5 Minutes

### Setup
1. Create empty GameObject "AR Manager"
2. Add component: `ARFeatureController`
3. Enable: Plane Detection, Passthrough

4. Create empty GameObject "Object Manager"
5. Add component: `SpawnedObjectsManager`
6. Assign: Object Prefab = Interactive Lego Brick

### Usage
```
SpawnedObjectsManager.SaveSpawnedObjects() - Save all bricks
SpawnedObjectsManager.LoadSpawnedObjects() - Load saved bricks
```

---

## File Locations

**Scene**: `Assets/Scenes/TestScene.unity`
**Lego Model**: `Assets/LegoBricks/Legos.fbx`
**Scripts**: `Assets/MRTemplateAssets/Scripts/`
**Prefabs**: `Assets/MRTemplateAssets/Prefabs/`
**Full Guide**: `TESTSCENE_SETUP_GUIDE.md`

---

## Scene Hierarchy

```
TestScene
├── Directional Light (lighting)
├── XR Interaction Manager (manages interactions)
├── XR Origin (player rig)
│   ├── Camera Offset
│   │   └── Main Camera (player view)
└── Interactive Lego Brick (grabbable object)
```

---

## Troubleshooting

**Can't grab brick?**
→ Check hand tracking is enabled on XR Origin

**Can't see brick?**
→ Check brick position (0, 1.5, 0.5)

**No UI interaction?**
→ Add XR UI Input Module to Canvas

---

## Next Steps

1. Build and test basic scene ✓
2. Add hand menu (5 min)
3. Add persistence (5 min)
4. Customize experience

Full documentation: `TESTSCENE_SETUP_GUIDE.md`
