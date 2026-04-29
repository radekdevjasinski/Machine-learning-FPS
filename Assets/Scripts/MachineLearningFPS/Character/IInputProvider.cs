using UnityEngine;

namespace MachineLearningFPS.Character
{
    public interface IInputProvider
    {
        Vector2 MoveInput { get; }
        Vector2 LookInput { get; }
        bool JumpInput { get; }
        bool CrouchInput { get; }

        bool ShootInput { get; }
        bool ShootTriggered { get; }
        int WeaponSelectInput { get; }
        bool Weapon1Triggered { get; }
        bool Weapon2Triggered { get; }
        bool Weapon3Triggered { get; }
    }
}