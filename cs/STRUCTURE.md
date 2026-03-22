CS Directory Structure
----------------------

This project groups C# source files under `cs/`. To keep things clear and maintainable,
we now organize files into subfolders by purpose. Use this file as a quick reference.

Top-level
- cs/: main C# source folder

Helpers / Common
- cs/Utils/: extension methods and small helpers that wrap Godot Variant/Array/Dictionary interop
  - DictionaryExtensions.cs : TryGetAs<T>, TryGetInt, GetIntOrDefault, ConversionLogger
  - ArrayExtensions.cs      : OfType<T> to enumerate typed items from Godot.Array

Actions / Gameplay
- cs/Actions/: Action base classes and context objects
  - Action.cs        : base Action class (RefCounted) — Activate(ActionContext) overload and Dictionary shim
  - ActionContext.cs : strongly-typed wrapper for Dictionary game_state

Gameplay / UI / Nodes
- cs/: other scene scripts grouped by feature (e.g., Card.cs, Character.cs, PlayableCard.cs, Main.cs)

Notes
- Keep helpers in `cs/Utils` to avoid Variant casting at call sites — use the extension methods.
- Action subclasses should override `Activate(ActionContext ctx)` to receive typed data.
- When adding new utilities or base classes, create a new subfolder under `cs/` and update this STRUCTURE.md.

