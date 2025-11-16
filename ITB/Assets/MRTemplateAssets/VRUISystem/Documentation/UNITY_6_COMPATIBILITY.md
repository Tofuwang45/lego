# Unity 6 Compatibility Updates

## Overview

This document describes the updates made to ensure compatibility with Unity 6000.2.12f1 and XR Interaction Toolkit 3.2.1.

## Unity 6 / XR Interaction Toolkit 3.2.1 API Changes

### Key Namespace Changes

| Component | Old Namespace (Unity 2022/XRI 2.x) | New Namespace (Unity 6/XRI 3.2.1) |
|-----------|-----------------------------------|-----------------------------------|
| `LazyFollow` | Not available / custom | `UnityEngine.XR.Interaction.Toolkit.UI` |
| `XROrigin` | `UnityEngine.XR.Interaction.Toolkit` | `Unity.XR.CoreUtils` |
| `XRRayInteractor` | `UnityEngine.XR.Interaction.Toolkit` | `UnityEngine.XR.Interaction.Toolkit.Interactors` |
| `XRController` | Deprecated | Use Input Actions instead |

### Find Object API Changes

| Old API (Unity 2022) | New API (Unity 6) |
|----------------------|-------------------|
| `FindObjectOfType<T>()` | `FindFirstObjectByType<T>()` |
| `FindObjectsOfType<T>()` | `FindObjectsByType<T>(FindObjectsSortMode.None)` |

## Files Updated

All scripts in the VR UI System have been updated:

### Core Scripts
- **ForearmSlateUI.cs**
  - Added `Unity.XR.CoreUtils` for `XROrigin`
  - Added `UnityEngine.XR.Interaction.Toolkit.Interactors` for `XRRayInteractor`
  - Removed `XRController` dependency (deprecated)
  - Updated `FindObjectOfType` → `FindFirstObjectByType`
  - Updated `FindObjectsOfType` → `FindObjectsByType`

### UI Scripts
- **StatsPanel.cs**
  - Added `UnityEngine.XR.Interaction.Toolkit.UI` for `LazyFollow`

- **BlockButton.cs**
  - Updated namespace from `UnityEngine.XR.Interaction.Toolkit` to `UnityEngine.XR.Interaction.Toolkit.Interactors`

- **GridLayoutManager.cs**
  - Updated namespace from `UnityEngine.XR.Interaction.Toolkit` to `UnityEngine.XR.Interaction.Toolkit.Interactors`

### Interaction Scripts
- **DeleteMode.cs**
  - Updated namespace for `XRRayInteractor`
  - Removed deprecated `XRController` usage
  - Updated input detection to use `rayInteractor.selectInput.ReadValue()` instead of `inputDevice.TryGetFeatureValue()`

## Deprecated API Replacements

### XRController Input Detection (Old)
```csharp
var controller = rayInteractor.GetComponent<XRController>();
if (controller != null)
{
    controller.inputDevice.TryGetFeatureValue(
        UnityEngine.XR.CommonUsages.triggerButton,
        out bool triggerPressed
    );
    return triggerPressed;
}
```

### Modern Input Detection (New)
```csharp
if (rayInteractor != null)
{
    // Check if the select input (trigger) is pressed
    float selectValue = rayInteractor.selectInput.ReadValue();
    return selectValue > 0.5f;
}
```

## Package Versions

The VR UI system is compatible with:

- **Unity**: 6000.2.12f1
- **XR Interaction Toolkit**: 3.2.1
- **XR Hands**: 1.7.0
- **AR Foundation**: 6.2.0
- **XR Core Utils**: (included with XR Interaction Toolkit 3.2.1)

## Testing Checklist

After updating, verify:

- [ ] All scripts compile without errors
- [ ] No namespace not found errors
- [ ] LazyFollow component can be added to GameObjects
- [ ] XROrigin is found correctly in scene
- [ ] XRRayInteractor references work correctly
- [ ] Input detection works (trigger press for delete mode)
- [ ] No deprecation warnings in console

## Migration Notes

If you're upgrading an existing project from Unity 2022.3 or earlier:

1. **Update packages** to Unity 6 compatible versions
2. **Replace Find APIs** throughout codebase:
   - Search for `FindObjectOfType` → Replace with `FindFirstObjectByType`
   - Search for `FindObjectsOfType` → Replace with `FindObjectsByType`

3. **Update XR namespaces**:
   - Add `using Unity.XR.CoreUtils;` where using `XROrigin`
   - Add `using UnityEngine.XR.Interaction.Toolkit.Interactors;` where using `XRRayInteractor`
   - Add `using UnityEngine.XR.Interaction.Toolkit.UI;` where using `LazyFollow`

4. **Replace XRController input**:
   - Use `interactor.selectInput.ReadValue()` instead of `controller.inputDevice.TryGetFeatureValue()`

## Common Errors and Solutions

### Error: "The type or namespace name 'LazyFollow' could not be found"
**Solution**: Add `using UnityEngine.XR.Interaction.Toolkit.UI;`

### Error: "The type or namespace name 'XROrigin' could not be found"
**Solution**: Add `using Unity.XR.CoreUtils;`

### Error: "The type or namespace name 'XRRayInteractor' could not be found"
**Solution**: Add `using UnityEngine.XR.Interaction.Toolkit.Interactors;`

### Error: "'Object' does not contain a definition for 'FindObjectOfType'"
**Solution**: Use `FindFirstObjectByType<T>()` instead

### Error: "XRController is obsolete"
**Solution**: Use Input Actions and `interactor.selectInput` instead of `XRController.inputDevice`

## References

- [Unity 6 Release Notes](https://unity.com/releases/unity-6)
- [XR Interaction Toolkit 3.2.1 Documentation](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.2/manual/index.html)
- [XR Interaction Toolkit Migration Guide](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.2/manual/migration-guide.html)

## Version History

- **v1.0** (2025-11-15): Initial implementation with Unity 2022.3 API
- **v1.1** (2025-11-16): Updated for Unity 6000.2.12f1 compatibility

---

**Last Updated**: 2025-11-16
**Unity Version**: 6000.2.12f1
**XR Interaction Toolkit**: 3.2.1
