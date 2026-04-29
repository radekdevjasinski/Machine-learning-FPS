using UnityEngine;
using UnityEngine.InputSystem;

namespace MachineLearningFPS.Character
{
    public class PlayerInputProvider : MonoBehaviour, IInputProvider
    {
        [Header("Input Actions")]
        public InputActionReference moveAction;
        public InputActionReference lookAction;
        public InputActionReference jumpAction;
        public InputActionReference shootAction;
        public InputActionReference crouchAction;
        public InputActionReference selectWeapon1;
        public InputActionReference selectWeapon2;
        public InputActionReference selectWeapon3;

        private void OnEnable()
        {
            moveAction?.action.Enable();
            lookAction?.action.Enable();
            jumpAction?.action.Enable();
            shootAction?.action.Enable();
            crouchAction?.action.Enable();
            selectWeapon1?.action.Enable();
            selectWeapon2?.action.Enable();
            selectWeapon3?.action.Enable();
        }

        private void OnDisable()
        {
            moveAction?.action.Disable();
            lookAction?.action.Disable();
            jumpAction?.action.Disable();
            shootAction?.action.Disable();
            crouchAction?.action.Disable();
            selectWeapon1?.action.Disable();
            selectWeapon2?.action.Disable();
            selectWeapon3?.action.Disable();
        }

        public Vector2 MoveInput => moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 LookInput => lookAction != null ? lookAction.action.ReadValue<Vector2>() : Vector2.zero;
        public bool JumpInput => jumpAction != null && jumpAction.action.IsPressed();
        public bool CrouchInput => crouchAction != null && crouchAction.action.IsPressed();
        public bool ShootInput => shootAction != null && shootAction.action.IsPressed();
        public bool ShootTriggered => shootAction != null && shootAction.action.triggered;

        public int WeaponSelectInput => selectWeapon1 != null && selectWeapon1.action.IsPressed() ? 0 :
                                        selectWeapon2 != null && selectWeapon2.action.IsPressed() ? 1 :
                                        selectWeapon3 != null && selectWeapon3.action.IsPressed() ? 2 : -1;

        public bool Weapon1Triggered => selectWeapon1 != null && selectWeapon1.action.triggered;
        public bool Weapon2Triggered => selectWeapon2 != null && selectWeapon2.action.triggered;
        public bool Weapon3Triggered => selectWeapon3 != null && selectWeapon3.action.triggered;
    }
}