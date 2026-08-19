using System.Collections.Generic;
using UnityEngine;
using MachineLearningFPS.Character;
using System;

namespace MachineLearningFPS.Environment
{
    public class EpisodeController : MonoBehaviour
    {
        [Header("Episode Settings")]
        [SerializeField] private List<MLController> _agents;
        [SerializeField] private int _maxEpisodeSteps = 1500;
        public int MaxEpisodeSteps => _maxEpisodeSteps;
        public float TimeRemainingSeconds => Mathf.Max(0f, (_maxEpisodeSteps - _currentEpisodeSteps) * Time.fixedDeltaTime);
        private int _currentEpisodeSteps = 0;
        private bool _timeUp = false;

        [Header("References")]
        [SerializeField] private ArenaController _arenaController;
        [SerializeField] private BattleRoyaleZone _battleRoyaleZone;
        [SerializeField] private KingOfTheHillZone _kingOfTheHillZone;


        [Header("Curriculum Settings")]
        [SerializeField] private MLCurriculumSettings _curriculum;
        public MLCurriculumSettings Curriculum => _curriculum;

        private MatchController _matchController;

        public static event Action OnEpisodeReset;
        public event Action<float> OnAgentEpisodeEnd;

        private void Start()
        {
            _matchController = FindAnyObjectByType<MatchController>();

            List<Health> healthList = new List<Health>();
            foreach (var agent in _agents)
            {
                if (agent.TryGetComponent(out Health health))
                {
                    health.OnDeath += HandleAgentDeath;
                    health.OnDamageTaken += HandleAgentDamage;
                    healthList.Add(health);
                }
            }

            _matchController.RunTransition(MatchOutcome.None, ResetEnvironment);
            if (_battleRoyaleZone != null && _curriculum.EnableBattleRoyaleZone)
            {
                float episodeDurationSeconds = _maxEpisodeSteps * Time.fixedDeltaTime;
                float shrinkDuration = _curriculum.ShrinkDurationInEpisodePercent / 100f * episodeDurationSeconds;
                _battleRoyaleZone.InitializeZone(
                    healthList,
                    _curriculum.BattleRoyaleDamageEnabled,
                    _curriculum.BattleRoyalePenaltyAmount,
                    shrinkDuration
                );
            }
        }

        private void OnDestroy()
        {
            foreach (var agent in _agents)
            {
                if (agent != null)
                {
                    if (agent.TryGetComponent(out Health health))
                    {
                        health.OnDeath -= HandleAgentDeath;
                        health.OnDamageTaken -= HandleAgentDamage;
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            _currentEpisodeSteps++;

            if (_currentEpisodeSteps >= _maxEpisodeSteps)
            {
                _timeUp = true;
                _matchController.RunTransition(MatchOutcome.Draw, PerformReset);
            }
        }


        private void HandleAgentDeath(GameObject victim, GameObject killer)
        {
            MatchOutcome outcome = MatchOutcome.Draw;

            if (killer != null && killer.TryGetComponent(out MLController killerML))
            {
                killerML.ApplyKillReward();
                outcome = killerML.TeamID == 0 ? MatchOutcome.Team0Win : MatchOutcome.Team1Win;
            }
            else if (victim != null)
            {
                foreach (var otherAgent in _agents)
                {
                    if (otherAgent != null && otherAgent.gameObject != victim)
                    {
                        otherAgent.ApplyWonByZoneReward();
                        outcome = otherAgent.TeamID == 0 ? MatchOutcome.Team0Win : MatchOutcome.Team1Win;
                    }
                }
            }

            if (victim != null && victim.TryGetComponent(out MLController victimML))
            {
                victimML.ApplyDeathPenalty();
            }

            _matchController.RunTransition(outcome, PerformReset);
        }

        private void HandleAgentDamage(GameObject victim, GameObject attacker, float amount)
        {
            if (attacker != null && attacker.TryGetComponent(out MLController attackerML))
            {
                attackerML.AddReward(amount * _curriculum.DealingDamageRewardScale);
            }

            if (victim != null && victim.TryGetComponent(out MLController victimML))
            {
                victimML.AddReward(-amount * _curriculum.TakingDamagePenaltyScale);
                if (attacker != null)
                {
                    victimML.UpdateEnemyPositionFromDamage(attacker.transform.position);
                }
            }
        }

        private void PerformReset()
        {
            _currentEpisodeSteps = 0;

            foreach (var agent in _agents)
            {
                if (_timeUp && _curriculum.EnableTrucePenalty)
                {
                    agent.AddReward(_curriculum.TrucePenaltyAmount);
                }
                float episodeReward = agent.GetCumulativeReward();
                OnAgentEpisodeEnd?.Invoke(episodeReward);
                agent.EndEpisode();
            }

            _timeUp = false;
            ResetEnvironment();

            OnEpisodeReset?.Invoke();
        }

        private void ResetEnvironment()
        {
            foreach (var agent in _agents)
            {
                if (agent.TryGetComponent(out Health health)) health.ResetHealth();
                if (agent.TryGetComponent(out FPSMovement movement)) movement.ResetMovement();
                if (agent.TryGetComponent(out CharacterColor color)) color.ApplyTeamColor(agent.TeamID);
                if (agent.TryGetComponent(out MovementToUI movementUI)) movementUI.ResetState();
                agent.ResetActionState();
                agent.Weapon?.ResetWeapon();
                agent.GetComponentInChildren<CharacterVisuals>()?.ResetToIdle();
            }

            if (_arenaController != null)
            {
                _arenaController.ResetArena(_agents);
            }
            else
            {
                Debug.LogWarning("[EpisodeController] ArenaController is not assigned.");
            }

            if (_battleRoyaleZone != null)
            {
                if (_curriculum.EnableBattleRoyaleZone) _battleRoyaleZone.ResetZone();
            }
            else
            {
                Debug.LogWarning("[EpisodeController] BattleRoyaleZone is not assigned.");
            }
            if (_kingOfTheHillZone != null)
            {
                _kingOfTheHillZone.ResetZone();
            }
            else
            {
                //Debug.LogWarning("[EpisodeController] KingOfTheHillZone is not assigned.");
            }
        }
    }
}
