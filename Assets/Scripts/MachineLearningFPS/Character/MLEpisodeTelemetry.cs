using System;
using MachineLearningFPS.UI;
using Unity.MLAgents;
using UnityEngine;

namespace MachineLearningFPS.Character
{
    [RequireComponent(typeof(MLController), typeof(CharacterController))]
    public class MLEpisodeTelemetry : MonoBehaviour
    {
        private MLController _mlController;
        private CharacterController _characterController;
        private HUDConsole _hudConsole;
        private FPSMovement _movementBody;


        private float _walkDistance;
        private int _shotsFired;
        private float _airTime;
        private float _crouchTime;
        private float[] _weaponActiveTime = new float[3];

        private Vector3 _lastPosition;

        private void Awake()
        {
            _mlController = GetComponent<MLController>();
            _characterController = GetComponent<CharacterController>();
            _movementBody = GetComponent<FPSMovement>();
        }
        private void OnEnable()
        {
            _mlController.OnEpisodeEnded += HandleEpisodeEnded;
            _mlController.OnAgentShot += HandleAgentShot;
            _mlController.OnAgentKilledEnemy += HandleKill;
            _mlController.OnAgentDied += HandleDeath;
        }

        private void Start()
        {
            _lastPosition = transform.position;
            _hudConsole = HUDConsole.Instance;
        }

        private void OnDisable()
        {
            _mlController.OnEpisodeEnded -= HandleEpisodeEnded;
            _mlController.OnAgentShot -= HandleAgentShot;
            _mlController.OnAgentKilledEnemy -= HandleKill;
            _mlController.OnAgentDied -= HandleDeath;
        }

        private void Update()
        {
            // walk Distance Tracking
            Vector3 currentPos = transform.position;
            currentPos.y = 0;
            Vector3 lastPosFlat = new Vector3(_lastPosition.x, 0, _lastPosition.z);
            _walkDistance += Vector3.Distance(lastPosFlat, currentPos);
            _lastPosition = transform.position;

            // air Time Tracking
            if (!_characterController.isGrounded) _airTime += Time.deltaTime;

            // crouch Time Tracking
            if (_characterController.height < 1.2f) _crouchTime += Time.deltaTime;

            // weapon Active Time Tracking
            if (_mlController.Weapon != null && _mlController.Weapon.CurrentWeaponIndex >= 0 && _mlController.Weapon.CurrentWeaponIndex < _weaponActiveTime.Length)
            {
                _weaponActiveTime[_mlController.Weapon.CurrentWeaponIndex] += Time.deltaTime;
            }
            SendInformationToConsole();
        }

        private void SendInformationToConsole()
        {
            if (_hudConsole == null) return;
            Vector3 localVelocity = transform.InverseTransformDirection(_characterController.velocity);
            Vector3 localVelocityFlat = new Vector3(localVelocity.x / Mathf.Max(_movementBody.MoveSpeed, 1f), 0, localVelocity.z / Mathf.Max(_movementBody.MoveSpeed, 1f));
            _hudConsole.UpdateValue(_movementBody.HeadTransform, "Position", transform.position);
            _hudConsole.UpdateValue(_movementBody.HeadTransform, "Velocity", localVelocityFlat);
            _hudConsole.UpdateValue(_movementBody.HeadTransform, "Grounded", _characterController.isGrounded);
            _hudConsole.UpdateValue(_movementBody.HeadTransform, "Crouching", _characterController.height < 1.2f);
            _hudConsole.UpdateValue(_movementBody.HeadTransform, "Shoot Readiness", _mlController.Weapon.ShootReadinessPercentage);
            _hudConsole.UpdateValue(_movementBody.HeadTransform, "Active Weapon", _mlController.Weapon.CurrentWeaponIndex >= 0 ? $"Weapon {_mlController.Weapon.CurrentWeaponIndex}" : "-1");
            float reward = _mlController.GetCumulativeReward();
            _hudConsole.UpdateValue(_movementBody.HeadTransform, "Reward", reward);
        }

        private void HandleEpisodeEnded()
        {
            Academy.Instance.StatsRecorder.Add("Agent Movement/Distance Walked", _walkDistance, StatAggregationMethod.Average);
            //Debug.Log($"Distance Walked: {_walkDistance}");
            Academy.Instance.StatsRecorder.Add("Agent Movement/Air Time", _airTime, StatAggregationMethod.Average);
            //Debug.Log($"Air Time: {_airTime}");
            Academy.Instance.StatsRecorder.Add("Agent Movement/Crouch Time", _crouchTime, StatAggregationMethod.Average);
            //Debug.Log($"Crouch Time: {_crouchTime}");
            Academy.Instance.StatsRecorder.Add("Agent Combat/Shots Fired", _shotsFired, StatAggregationMethod.Average);
            //Debug.Log($"Shots Fired: {_shotsFired}");

            for (int i = 0; i < _weaponActiveTime.Length; i++)
            {
                Academy.Instance.StatsRecorder.Add($"Agent Combat/Weapon {i} Active Time", _weaponActiveTime[i], StatAggregationMethod.Average);
            }

            // reset metrics for the new episode
            _walkDistance = 0f;
            _shotsFired = 0;
            _airTime = 0f;
            _crouchTime = 0f;
            Array.Clear(_weaponActiveTime, 0, _weaponActiveTime.Length);
            _lastPosition = transform.position;
        }

        private void HandleAgentShot() => _shotsFired++;

        private void HandleKill() => Academy.Instance.StatsRecorder.Add($"Team {_mlController.TeamID}/Total Kills", 1f, StatAggregationMethod.Sum);
        private void HandleDeath() => Academy.Instance.StatsRecorder.Add($"Team {_mlController.TeamID}/Total Deaths", 1f, StatAggregationMethod.Sum);
    }
}