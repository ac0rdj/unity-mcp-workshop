# Live demo prompts (all tested on the presenter machine, Oct 31)

Speak these with Wispr Flow. Run from `claude` inside the ParkourBase folder.

## Part I: "It sees" (≈10 s)
```
List the root objects in the open Unity scene and tell me if there are any console errors.
```

## Part II: rock scatter (≈1 min)
Open `Assets/Scenes/RockDemo.unity` first.
```
Scatter 40 rocks across the Grass plane as children of the Rocks object. Use squashed spheres and cubes with a grey material.
```
Then react out loud:
```
Too uniform. Give them random scale between 0.5 and 2.5, random Y rotation, and cluster some of them together.
```
> ⚠️ Gotcha seen in testing: one save call wrote to the wrong path. If you want to keep the empty scene for a re-run, **don't save** (or say "don't save the scene").
> Finished reference: `Assets/Scenes/Examples/RockDemo_After.unity`.

## Part II: real art in 30 s (Blender → Unity)
Blender must be open with the MCP server started.
```
Use Blender to grab a CC0 boulder from Poly Haven, export it as FBX with embedded textures into Assets/Art/Models, then place three of them around the Start platform in Unity.
```

## Part II: docs lookup
```
Use context7 to check the Unity docs for CharacterController.Move, then explain why our player keeps momentum in the air.
```

## Track starter prompts (put these on the challenge slide)
- 🎨 **Art:** "Grab 3 CC0 props from Poly Haven that fit a floating-island course and decorate the Props object. Don't block the jump paths."
- ⚙️ **Mechanics:** "Add 20 spinning coins along the route under Collectibles, plus a coin counter on the HUD. Put a few on risky blaster arcs."
- 🧹 **Polish:** "When the blaster fires, add a spark burst, an FOV kick and a camera shake. Generate a whoomp sound in code."

Finished examples for each track: `Assets/Scenes/Examples/Track_Art|Track_Mechanics|Track_Polish.unity`.
