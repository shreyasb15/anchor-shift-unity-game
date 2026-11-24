# Anchor Shift

A minimal 2D grid puzzle for Unity 6000.5.4f1. The Unity project is in `AnchorShift/`.

## Play

Open `AnchorShift/` in Unity Hub, open `Assets/Scenes/AnchorShift.unity`, and press Play.
The scene is also the sole enabled scene in Build Settings.

- Click a colored block to anchor it. Click it again to release it.
- Arrow keys or WASD move every free block one cell, if space permits.
- Z undoes the last move or anchor change. R restarts the level.
- Enter advances after matching all three blocks to their outlined targets.

There are eight levels. Progress is session-only; restarting Play begins at level one.
Level definitions are in `Assets/AnchorShift/Resources/levels.json`, separate from the scene.
The EditMode suite in `Assets/AnchorShift/Tests/EditMode` checks collisions, anchors,
undo, and level solvability with breadth-first search.
