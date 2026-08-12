# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

A Unity FPS in which bot characters are trained with Unity ML-Agents (self-play PPO) to move, aim, and shoot each other. A single `MLController` (an ML-Agents `Agent`) drives the exact same movement and weapon code that the human `PlayerController` uses — the two only differ in where input comes from — so gameplay code changes affect both humans and trained bots.

## Environment

- Unity **6000.4.1f1**, URP, the new Input System, `com.unity.ml-agents` **4.0.2** (see `Packages/manifest.json`).
- `ml-agents/` at the repo root is a local, git-ignored clone of the Unity ML-Agents toolkit (Python trainer + envs). It is not tracked by this repo's git history, so treat edits there as local/ephemeral scaffolding for running `mlagents-learn`, not versioned project source.
## MCP server (use it)

This repo has `mcp-unity` set up (`.mcp.json`, package `com.gamelovers.mcp-unity`), which gives Claude Code a live connection into the running Unity Editor via the `mcp__mcp-unity__*` tools: scene hierarchy/GameObject read+write, component updates, material creation/assignment, transform edits, prefab creation, play-mode control, and console log reads.

**Use these tools whenever the Editor is open and the task touches the scene, GameObjects, components, materials, or prefabs — instead of describing edits for the user to make by hand, or guessing at scene state.** Concretely: inspect real scene/GameObject state with `get_scene_info`/`get_gameobject`/`get_scenes_hierarchy` before assuming a hierarchy; make the edit directly with the matching `mcp__mcp-unity__*` tool; check `get_console_logs` after a change or a script recompile instead of asking the user to paste errors. Only fall back to asking the user to do it manually if the Editor isn't running or a tool call fails.

## Common commands

There is no CLI build/lint/test step wired into this repo — no asmdef, no test assembly, and no build scripts are checked in. Editor and build actions go through the Unity Editor itself or the `mcp__mcp-unity__*` tools when it's running.

Training (run from `ml-agents/`, against a headless Windows build at `Builds/Machine learning FPS.exe`):

```
mlagents-learn config/MLPlayer.yaml --run-id=<run-name>
```

The curriculum config variants (`MLPlayer_Curriculum0-3.yaml`, `cl1.yaml`, `cl2.yaml`) all target the same `MLPlayer` behavior name but change hyperparameters, `num_envs`, and `max_steps` as training progresses — check those fields before picking one to resume/extend. Trained policies land in `Assets/Data/Brains/*.onnx` and get assigned to the agent's Behavior Parameters component in the Editor.

## Architecture

All project code lives under `Assets/Scripts/MachineLearningFPS/`, split by namespace-matching folder: `Character/`, `Environment/`, `WeaponSystem/`, `UI/`, `Camera/`.

### Shared human/agent control path

- `FPSMovement` (CharacterController-based) is the single movement implementation used by both humans and bots — crouch, jump, look, and move all go through `SetInput(moveInput, lookInput, jumpInput, crouchInput)`.
- `IInputProvider` abstracts the input source. `PlayerInputProvider` reads the Input System for humans; `MLController` reads the same interface inside `Heuristic()` (for manual testing / recording demonstrations) and otherwise writes `ActionBuffers` into `FPSMovement`/`WeaponController` during `OnActionReceived()` when running as a trained policy.
- `WeaponController` + `Weapon` + `WeaponStats` (ScriptableObject, under `Assets/Data/Weapons/`) implement shared hitscan shooting (`Physics.SphereCastNonAlloc`) used by both the player and bots.

### ML-Agents loop

- `MLController.CollectObservations` builds the observation vector: local velocity, view direction, grounded/crouch flags, weapon readiness + one-hot equipped weapon, normalized health, last-known enemy direction, and time since the enemy was last seen. `WriteDiscreteActionMask` disables jump/crouch/weapon-swap actions that the active curriculum stage hasn't unlocked yet.
- `MLRewardManager` (a required sibling component on the agent) owns all reward shaping — sight/aim/approach rewards, kill/death/truce rewards, movement penalties. It reads every enable-flag and scale from `EpisodeController.Curriculum`, an `MLCurriculumSettings` ScriptableObject (instances under `Assets/Data/Curriculum/`), instead of hardcoding values. New reward terms should follow that pattern so curricula stay data-driven and swappable.
- `EpisodeController` is the per-arena orchestrator: it subscribes to `Health.OnDeath`/`OnDamageTaken` for every agent it owns, applies kill/death rewards, and resets the episode (`ResetEpisode` → `ArenaController.ResetArena`, plus `BattleRoyaleZone.ResetZone` / `KingOfTheHillZone.ResetZone`) on a death or once `_maxEpisodeSteps` is hit.
- `ArenaController` handles spawn placement (predefined points or randomized-in-bounds) and obstacle respawn (`ObstacleController`) between episodes.
- Game-mode add-ons — `BattleRoyaleZone` (shrinking-ring damage/penalty), `KingOfTheHillZone` (capture-point reward), `EnvironmentRewardTrigger` (one-shot pickup-style triggers) — are optional per-arena components; `EpisodeController`/`MLRewardManager` null-check before using them, so an arena can opt out of any mode.
- `TrainingController` just aggregates and logs average episode reward periodically; it's a diagnostic, not part of the reward path.

### Spectating / debugging a running session

`SpectatorManager` (on the main camera) cycles between a free-fly camera (`FreeCam`) and per-bot first-person views; `PerspectiveController` hides/shows a character's own model via layer swapping so the active view doesn't see its own body. `UIController` and `HUDConsole` are two independent HUD layers: `UIController` drives the game-facing HUD (health bars, team score, shoot cooldown) off static events (`Health.OnHealthChanged`, `EpisodeController.OnPlayerKilled`, `MovementToUI.OnMovementStateChanged`), while `HUDConsole`/`MLEpisodeTelemetry` is a free-form debug readout of the currently-viewed agent's observations and cumulative reward, useful for live curriculum tuning.

### Curriculum-driven behavior

Nearly every reward term and unlockable action (jump, crouch, vertical look, weapon swap) is gated behind a bool+amount pair on `MLCurriculumSettings`. Multiple `.asset` instances exist for different training stages (`Assets/Data/Curriculum/1.asset`, `2+.asset`, etc., paired with matching `ml-agents/config/*.yaml` files). Start a new stage by pointing `EpisodeController._curriculum` at a new/duplicated asset rather than editing values shared by an in-progress run.
