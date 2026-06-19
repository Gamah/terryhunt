# Terry Hunt

A minimal s&box sketch: you stand in first person on a large floor, a "Terry"
(citizen) spawns in front of you, and clicking it with the crosshair deletes it.

## Run

Open `terryhunt.sbproj` in the s&box editor and press Play (startup scene is
`scenes/main.scene`). Left click to delete the Terry.

## How it works

The scene file is deliberately tiny — just a sun, a 2D skybox, a camera, and a
`Game` object holding `GameManager`. Everything else is built in code on start,
following the rotaliate-client pattern (non-uniform scale in scene JSON is
unreliable, so geometry is generated from C#).

| File | Role |
|------|------|
| `Code/GameManager.cs` | Builds the floor, spawns the first-person player, spawns a Terry in front of it. |
| `Code/Terry.cs` | A clickable citizen: model + capsule collider, tagged `terry`. |
| `Code/TerryHunter.cs` | On the player. Draws the crosshair; on left click, raycasts from the eye and destroys any `Terry` it hits. |

The player uses the engine's built-in `PlayerController` with `ThirdPerson = false`,
so it drives the scene `Camera` for first-person view and mouselook.

## Ideas to extend

- Respawn a new Terry (or several) after one is deleted — turn it into a score game.
- Add a kill effect / sound on delete.
- Networked multiplayer hunt (bump `MaxPlayers` and spawn a player per connection).
