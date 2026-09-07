# Machine Learning FPS

A first-person shooter where the bots aren't scripted. They're trained. Built in Unity with ML-Agents, the same characters humans control are also trainable AI agents that learn to move, aim, and shoot through self-play reinforcement learning (PPO).

## What's going on here

Every bot runs on the exact same movement and weapon code a human player uses. The only difference is where the input comes from: a human presses keys, an agent reads a trained policy. Change the gameplay code and it affects both sides at once, since there's no separate "AI version" to keep in sync.

Bots learn in stages through a curriculum: early on they're limited to basic movement and aiming, and as training progresses, more actions (jumping, crouching, weapon swapping) and reward signals unlock. Each stage is its own configuration, so you can tune difficulty and behavior without touching a run that's already in progress.

## Tech stack

- **Unity 6000.4.1f1** with URP and the new Input System
- **Unity ML-Agents 4.0.2** for training and inference
