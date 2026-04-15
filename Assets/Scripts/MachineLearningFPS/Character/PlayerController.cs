using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

using MachineLearningFPS.WeaponSystem;

namespace MachineLearningFPS.Character
{
    [RequireComponent(typeof(FPSMovement))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Input Actions")]
        public InputActionReference moveAction;
        public InputActionReference lookAction;
        public InputActionReference jumpAction;
        public InputActionReference fireAction;
        public InputActionReference crouchAction;
        public InputActionReference selectWeapon1;
        public InputActionReference selectWeapon2;
        public InputActionReference selectWeapon3;

        [Header("References")]
        private FPSMovement _movementBody;
        [SerializeField] private WeaponController _laserWeapon;

        void Awake()
        {
            _movementBody = GetComponent<FPSMovement>();
            _laserWeapon = GetComponentInChildren<WeaponController>();
            moveAction?.action.Enable();
            lookAction?.action.Enable();
            jumpAction?.action.Enable();
            fireAction?.action.Enable();
            crouchAction?.action.Enable();

            selectWeapon1?.action.Enable();
            selectWeapon2?.action.Enable();
            selectWeapon3?.action.Enable();
        }

        void Update()
        {
            Vector2 moveInput = moveAction.action.ReadValue<Vector2>();
            Vector2 lookInput = lookAction.action.ReadValue<Vector2>();
            bool jump = jumpAction.action.IsPressed();
            bool crouch = crouchAction.action.IsPressed();

            _movementBody.SetInput(moveInput, lookInput, jump, crouch);


            if (fireAction.action.triggered)
            {
                _laserWeapon.Shoot();
            }
            if (selectWeapon1.action.triggered)
            {
                _laserWeapon.EquipWeapon(0);
            }
            if (selectWeapon2.action.triggered)
            {
                _laserWeapon.EquipWeapon(1);
            }
            if (selectWeapon3.action.triggered)
            {
                _laserWeapon.EquipWeapon(2);
            }
        }
    }
}
