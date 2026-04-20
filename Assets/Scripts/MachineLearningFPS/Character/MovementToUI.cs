using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MachineLearningFPS.Character
{
    public class MovementToUI : MonoBehaviour
    {
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Transform targetHead;

        public static event Action<Transform, string> OnMovementStateChanged;

        private int _lastStateHash = -1;

        private void OnEnable()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }
        }

        private void Update()
        {
            if (characterController == null) return;

            int newStateHash = GetCurrentStateHash();

            if (newStateHash != _lastStateHash)
            {
                _lastStateHash = newStateHash;

                switch (newStateHash)
                {
                    case 0:
                        OnMovementStateChanged?.Invoke(targetHead, "Jumping");
                        break;
                    case 1:
                        OnMovementStateChanged?.Invoke(targetHead, "Crouching");
                        break;
                    case 2:
                        OnMovementStateChanged?.Invoke(targetHead, "Walking");
                        break;
                    case 3:
                    default:
                        OnMovementStateChanged?.Invoke(targetHead, "Idle");
                        break;

                }
            }
        }

        private int GetCurrentStateHash()
        {
            if (!characterController.isGrounded)
            {
                return 0;
            }

            if (characterController.height < 1.2f)
            {
                return 1;
            }

            if (characterController.velocity.magnitude > 0.1f)
            {
                return 2;
            }

            return 3;
        }
    }
}
