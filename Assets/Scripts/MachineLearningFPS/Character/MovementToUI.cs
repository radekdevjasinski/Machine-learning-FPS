using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using MachineLearningFPS.Environment;

namespace MachineLearningFPS.Character
{
    public class MovementToUI : MonoBehaviour
    {
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Transform targetHead;

        public static event Action<Transform, string> OnMovementStateChanged;

        private Health _health;
        private bool _isDead;
        private int _lastStateHash = -1;

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            if (_health != null) _health.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            if (_health != null) _health.OnDeath -= HandleDeath;
        }

        private void HandleDeath(GameObject victim, GameObject killer)
        {
            _isDead = true;
        }

        public void ResetState()
        {
            _isDead = false;
            _lastStateHash = 3;
            OnMovementStateChanged?.Invoke(targetHead, "idle");
        }

        private void Update()
        {
            if (characterController == null) return;
            if (MatchController.InputBlocked || Time.timeScale == 0f) return;

            int newStateHash = GetCurrentStateHash();

            if (newStateHash != _lastStateHash)
            {
                _lastStateHash = newStateHash;

                switch (newStateHash)
                {
                    case 0:
                        OnMovementStateChanged?.Invoke(targetHead, "jumping");
                        break;
                    case 1:
                        OnMovementStateChanged?.Invoke(targetHead, "crouching");
                        break;
                    case 2:
                        OnMovementStateChanged?.Invoke(targetHead, "walking");
                        break;
                    case 4:
                        OnMovementStateChanged?.Invoke(targetHead, "dead");
                        break;
                    case 3:
                    default:
                        OnMovementStateChanged?.Invoke(targetHead, "idle");
                        break;

                }
            }
        }

        private int GetCurrentStateHash()
        {
            if (_isDead)
            {
                return 4;
            }

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
