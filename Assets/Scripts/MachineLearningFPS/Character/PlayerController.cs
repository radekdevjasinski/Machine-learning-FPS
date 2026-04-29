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
        [Header("References")]
        private FPSMovement _movementBody;
        [SerializeField] private WeaponController _laserWeapon;
        private IInputProvider _inputProvider;

        void Awake()
        {
            _movementBody = GetComponent<FPSMovement>();
            _laserWeapon = GetComponentInChildren<WeaponController>();
            _inputProvider = GetComponent<IInputProvider>();
        }

        void Update()
        {
            if (_inputProvider == null) return;

            Vector2 moveInput = _inputProvider.MoveInput;
            Vector2 lookInput = _inputProvider.LookInput;
            bool jump = _inputProvider.JumpInput;
            bool crouch = _inputProvider.CrouchInput;

            _movementBody.SetInput(moveInput, lookInput, jump, crouch);

            if (_inputProvider.ShootTriggered)
            {
                _laserWeapon.Shoot();
            }
            if (_inputProvider.Weapon1Triggered)
            {
                _laserWeapon.EquipWeapon(0);
            }
            if (_inputProvider.Weapon2Triggered)
            {
                _laserWeapon.EquipWeapon(1);
            }
            if (_inputProvider.Weapon3Triggered)
            {
                _laserWeapon.EquipWeapon(2);
            }
        }
    }
}
