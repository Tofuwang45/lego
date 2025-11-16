# NearFarInteractor Support Update

## Summary

All VR UI scripts have been updated to support **BOTH** `NearFarInteractor` and `XRRayInteractor`, with automatic detection and priority given to the modern `NearFarInteractor`.

## Your Question Answered

**Q**: "Should I alter the code to use Near-Far Interactor, or switch the right hand to use XR Interactor?"

**A**: ✅ **Code has been updated to support BOTH automatically!**

You don't need to do anything - the system will:
1. **Auto-detect** which interactor type you have
2. **Prioritize** NearFarInteractor (modern approach)
3. **Fall back** to XRRayInteractor if needed

## What is NearFarInteractor?

`NearFarInteractor` is the **newer, better replacement** for `XRRayInteractor` in XR Interaction Toolkit 3.2.1+.

### Key Advantages

| Feature | XRRayInteractor | NearFarInteractor |
|---------|----------------|-------------------|
| **Interaction Range** | Far only (ray casting) | Near AND Far |
| **UI Support** | ✓ Yes | ✓ Yes (better) |
| **Hand Tracking** | Limited | ✓ Optimized |
| **Flexibility** | Ray-based only | Adapts to distance |
| **Modern Templates** | Legacy | ✓ Default |
| **Recommended** | Old projects | ✓ New projects |

### From Unity Documentation

> "Use this or Near-Far Interactor, not both."
> — ControllerInputActionManager.cs, Line 35

This confirms they're **alternatives**, with NearFarInteractor being the modern choice.

## What Changed in the Code

### 1. ForearmSlateUI.cs

**Added dual interactor support:**
```csharp
// Before
public XRRayInteractor rightHandRayInteractor;

// After
public XRRayInteractor rightHandRayInteractor;
public NearFarInteractor rightHandNearFarInteractor;
```

**Auto-detection prioritizes modern approach:**
```csharp
private void FindRightHandInteractor()
{
    // Try NearFarInteractor first (modern approach)
    var nearFarInteractors = FindObjectsByType<NearFarInteractor>(...);
    if (found) return;

    // Fallback to XRRayInteractor (legacy)
    var rayInteractors = FindObjectsByType<XRRayInteractor>(...);
}
```

### 2. GridLayoutManager.cs, RecentsManager.cs, BlockButton.cs

All updated to accept **both types**:
```csharp
// New signature with optional parameters
public void Initialize(
    BlockCatalogData catalog,
    XRRayInteractor rayInt = null,        // Optional
    NearFarInteractor nearFarInt = null   // Optional
)
```

### 3. BlockButton.cs - Ghost Preview

Works with either interactor:
```csharp
private void UpdateGhostPosition()
{
    // Try NearFarInteractor first
    if (nearFarInteractor != null)
    {
        RaycastHit hit;
        if (nearFarInteractor.TryGetCurrent3DRaycastHit(out hit))
        {
            // Position ghost...
            return;
        }
    }

    // Fallback to XRRayInteractor
    if (rayInteractor != null)
    {
        // Use ray interactor...
    }
}
```

### 4. DeleteMode.cs

Both interactors supported for deletion:
```csharp
private bool IsDeleteInputPressed()
{
    // Try NearFarInteractor first
    if (nearFarInteractor != null)
    {
        return nearFarInteractor.selectInput.ReadValue() > 0.5f;
    }

    // Fallback
    if (rayInteractor != null)
    {
        return rayInteractor.selectInput.ReadValue() > 0.5f;
    }

    return false;
}
```

## How It Works

### Auto-Detection Flow

```
ForearmSlateUI.Start()
    ↓
FindRightHandInteractor()
    ↓
Search for NearFarInteractor → Found? ✓ Use it
    ↓ Not found
Search for XRRayInteractor → Found? ✓ Use it
    ↓ Not found
Log warning (neither found)
```

### Usage Priority

When both are present (unlikely but handled):
1. **NearFarInteractor** is used (modern)
2. XRRayInteractor is ignored
3. No conflicts

## Compatibility Matrix

| Your Setup | What Happens |
|------------|--------------|
| **NearFarInteractor only** (Unity MR Template) | ✅ Auto-detected and used |
| **XRRayInteractor only** (Legacy setup) | ✅ Auto-detected and used |
| **Both present** | ✅ NearFarInteractor prioritized |
| **Neither present** | ⚠️ Warning logged, manual assignment needed |

## Testing Your Setup

### Console Messages

When the system starts, you'll see one of:
```
"ForearmSlateUI: Found NearFarInteractor on right hand"
```
or
```
"ForearmSlateUI: Found XRRayInteractor on right hand"
```

This confirms auto-detection worked!

### Manual Assignment (Optional)

If auto-detection fails, you can manually assign in Inspector:

**For NearFarInteractor:**
1. Select `ForearmSlateCanvas`
2. Find `Forearm Slate UI` component
3. Drag your right hand's NearFarInteractor to `Right Hand Near Far Interactor` field

**For XRRayInteractor:**
1. Same component
2. Drag to `Right Hand Ray Interactor` field instead

## Benefits of This Approach

### ✅ Future-Proof
- Works with current Unity templates
- Works with future XRI updates
- No breaking changes needed

### ✅ Backward Compatible
- Existing XRRayInteractor setups still work
- No need to modify old projects
- Graceful degradation

### ✅ Zero Configuration
- Auto-detection "just works"
- No manual setup required
- Intelligent priority system

### ✅ Flexible
- Supports hand tracking (NearFarInteractor)
- Supports controller rays (both types)
- Adapts to your XR rig automatically

## Updated Documentation

The **DETAILED_UNITY_SETUP.md** guide still applies:
- Auto-detection handles interactor finding
- Manual assignment optional (if auto-detect fails)
- All wiring instructions remain the same

## Migration from Old Code

If you had old code that only supported XRRayInteractor:

**Before:**
```csharp
public XRRayInteractor rayInteractor;

public void Initialize(BlockCatalogData catalog, XRRayInteractor interactor)
{
    rayInteractor = interactor;
}
```

**After (backward compatible):**
```csharp
public XRRayInteractor rayInteractor;
public NearFarInteractor nearFarInteractor;  // Added

public void Initialize(BlockCatalogData catalog,
                      XRRayInteractor rayInt = null,        // Optional now
                      NearFarInteractor nearFarInt = null)  // Added
{
    rayInteractor = rayInt;
    nearFarInteractor = nearFarInt;
}
```

**Old code calling Initialize still works!**
```csharp
// Still valid - optional parameters
manager.Initialize(catalog, myRayInteractor);

// New way - both types
manager.Initialize(catalog, myRayInt, myNearFarInt);

// New way - just NearFarInteractor
manager.Initialize(catalog, null, myNearFarInt);
```

## Recommendations

### For New Projects
✅ **Use NearFarInteractor** (Unity MR template default)
- Auto-detected by the VR UI system
- Better for hand tracking
- More flexible interaction

### For Existing Projects
✅ **Keep XRRayInteractor** (no changes needed)
- Still fully supported
- Auto-detected automatically
- No migration required

### For Best Results
✅ **Let auto-detection handle it**
- Don't manually assign unless needed
- Trust the priority system
- Check console for confirmation

## Troubleshooting

### "Could not find right hand interactor" Warning

**Cause**: Neither interactor type found

**Solutions**:
1. Verify your XR rig has a right hand controller
2. Check the controller has either NearFarInteractor or XRRayInteractor component
3. Manually assign in Inspector if auto-detect fails

### UI Not Responding to Clicks

**Check**:
1. Console shows interactor was found
2. `Tracked Device Graphic Raycaster` on canvas
3. Event Camera set on canvas (may auto-assign)

### Wrong Interactor Being Used

**If you want to force a specific type**:
1. Disable auto-detection by manually assigning
2. Only assign the one you want to use
3. Leave the other field empty

## Conclusion

**You don't need to choose!** The system now works with **both** NearFarInteractor and XRRayInteractor automatically. Your Unity MR template's NearFarInteractor will be detected and used without any configuration.

The update prioritizes the modern approach while maintaining full backward compatibility with legacy setups.

---

**Commit**: 0e858fdc - "Add NearFarInteractor support alongside XRRayInteractor"
**Status**: ✅ Pushed to branch
**Compatibility**: Unity 6000.2.12f1 + XRI 3.2.1
