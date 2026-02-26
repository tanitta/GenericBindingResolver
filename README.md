# GenericBindingResolver

[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](https://github.com/tanitta/GenericBindingResolver/blob/main/LICENSE)

GenericBindingResolver is a Unity Editor tool that records object-reference bindings on a target component as stable hierarchy paths, then re-applies them when the scene loads. It is designed to reduce prefab merge conflicts and keep bindings intact across team workflows.

## Features

- Collects all `ObjectReference` properties from a target component
- Stores references as hierarchy paths with sibling indices
- Re-applies bindings automatically when a scene opens
- Supports GameObject and Component references
- Optional one-click collect + apply-to-prefab workflow

## Requirements

- Unity 2022.3 or later
- Editor-only (no runtime behavior)

## Installation (UPM)

This package can be installed from Git URL using Unity Package Manager.

Add the Git URL in Package Manager, or add it directly to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "net.tanitta.generic-binding-resolver": "https://github.com/tanitta/GenericBindingResolver.git"
  }
}
```

## Quick Start

1. Add `GenericBindingResolver` to a GameObject that has the component you want to track.
2. Assign the target component to `_targetComponent` in the Inspector.
3. Click **Collect Bindings** to record references into the resolver.
4. Apply the prefab changes as usual, or use **Collect Bindings And Apply Prefab** if you want to apply immediately.

When a scene opens, the resolver automatically re-applies the saved bindings to the target component.

## Workflow Notes

- The resolver scans serialized properties on the target component and records any non-null object references.
- Binding data is stored as a hierarchy path that includes sibling indices to disambiguate same-name objects.
- References are re-linked by matching the saved path to the active scene objects.

## Limitations

- Editor-only (`UNITY_EDITOR`); there is no runtime binding.
- Prefab Mode is not supported for collect/apply.
- Only scene object references are supported; assets outside the scene are ignored.

## License

Apache-2.0. See `LICENSE` for details.
