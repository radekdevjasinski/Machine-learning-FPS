using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using MachineLearningFPS.Character;
using MachineLearningFPS.MachineLearning;

namespace MachineLearningFPS.Environment
{
    public class EvaluationStatsTracker : MonoBehaviour
    {
        [Header("Agents")]
        [SerializeField] private MLController _team0Agent;
        [SerializeField] private MLController _team1Agent;

        [Header("Session Settings")]
        [SerializeField] private bool _writeCsvToFile = true;
        [SerializeField] private string _csvFileNamePrefix = "eval_session";

        private string _csvFileName;

        private class BotRoundStats
        {
            public int ShotsFired;
            public int ShotsHit;
            public float DamageDealt;
            public float? FirstSightTime;
            public float? KillTime;
            public bool GotKill;
            public bool Died;
            public float? FightDurationSeconds;
        }

        private class RoundRecord
        {
            public int RoundNumber;
            public MatchOutcome Outcome;
            public float DurationSeconds;
            public readonly BotRoundStats Team0 = new BotRoundStats();
            public readonly BotRoundStats Team1 = new BotRoundStats();
        }

        private Health _team0Health;
        private Health _team1Health;
        private MLRewardManager _team0Reward;
        private MLRewardManager _team1Reward;
        private MatchController _matchController;

        private readonly List<RoundRecord> _completedRounds = new List<RoundRecord>();
        private RoundRecord _currentRound;
        private float _roundStartTime;
        private bool _sessionFinished;
        private bool _team0HitPending;
        private bool _team1HitPending;

        private void Awake()
        {
            _team0Health = _team0Agent.GetComponent<Health>();
            _team1Health = _team1Agent.GetComponent<Health>();
            _team0Reward = _team0Agent.GetComponent<MLRewardManager>();
            _team1Reward = _team1Agent.GetComponent<MLRewardManager>();
            _matchController = FindAnyObjectByType<MatchController>();
            _csvFileName = BuildNextCsvFileName();

            _roundStartTime = Time.time;
            _currentRound = new RoundRecord();
        }

        private string BuildNextCsvFileName()
        {
            int sessionNumber = 1;
            string fileName;

            while (File.Exists(Path.Combine(Application.persistentDataPath, fileName = $"{_csvFileNamePrefix}{sessionNumber}.csv")))
            {
                sessionNumber++;
            }

            return fileName;
        }

        private void OnEnable()
        {
            EpisodeController.OnEpisodeReset += HandleRoundStarted;
            MatchController.OnRoundConcluded += HandleRoundConcluded;
            MatchController.OnMatchConcluded += HandleMatchConcluded;

            _team0Agent.OnAgentShot += HandleShotTeam0;
            _team1Agent.OnAgentShot += HandleShotTeam1;

            _team0Health.OnDamageTaken += HandleDamage;
            _team1Health.OnDamageTaken += HandleDamage;
            _team0Health.OnDeath += HandleDeath;
            _team1Health.OnDeath += HandleDeath;

            _team0Reward.OnEnemyFirstSighted += HandleFirstSightedTeam0;
            _team1Reward.OnEnemyFirstSighted += HandleFirstSightedTeam1;
        }

        private void OnDisable()
        {
            EpisodeController.OnEpisodeReset -= HandleRoundStarted;
            MatchController.OnRoundConcluded -= HandleRoundConcluded;
            MatchController.OnMatchConcluded -= HandleMatchConcluded;

            _team0Agent.OnAgentShot -= HandleShotTeam0;
            _team1Agent.OnAgentShot -= HandleShotTeam1;

            _team0Health.OnDamageTaken -= HandleDamage;
            _team1Health.OnDamageTaken -= HandleDamage;
            _team0Health.OnDeath -= HandleDeath;
            _team1Health.OnDeath -= HandleDeath;

            _team0Reward.OnEnemyFirstSighted -= HandleFirstSightedTeam0;
            _team1Reward.OnEnemyFirstSighted -= HandleFirstSightedTeam1;
        }

        private void HandleRoundStarted()
        {
            _roundStartTime = Time.time;
            _currentRound = new RoundRecord();
            _team0HitPending = false;
            _team1HitPending = false;
        }

        private void HandleRoundConcluded(MatchOutcome outcome)
        {
            if (_sessionFinished) return;

            _currentRound.RoundNumber = _completedRounds.Count + 1;
            _currentRound.Outcome = outcome;
            _currentRound.DurationSeconds = Time.time - _roundStartTime;
            FinalizeFightDuration(_currentRound.Team0);
            FinalizeFightDuration(_currentRound.Team1);

            _completedRounds.Add(_currentRound);
            WriteCsv();
        }

        private void HandleMatchConcluded(MatchOutcome winner)
        {
            _sessionFinished = true;
            WriteCsv();
            Time.timeScale = 0f;
        }

        private static void FinalizeFightDuration(BotRoundStats stats)
        {
            if (stats.GotKill && stats.FirstSightTime.HasValue && stats.KillTime.HasValue)
            {
                stats.FightDurationSeconds = stats.KillTime.Value - stats.FirstSightTime.Value;
            }
        }

        private void HandleShotTeam0()
        {
            _currentRound.Team0.ShotsFired++;
            if (_team0HitPending)
            {
                _currentRound.Team0.ShotsHit++;
                _team0HitPending = false;
            }
        }

        private void HandleShotTeam1()
        {
            _currentRound.Team1.ShotsFired++;
            if (_team1HitPending)
            {
                _currentRound.Team1.ShotsHit++;
                _team1HitPending = false;
            }
        }

        private void HandleFirstSightedTeam0()
        {
            if (!_currentRound.Team0.FirstSightTime.HasValue)
                _currentRound.Team0.FirstSightTime = Time.time - _roundStartTime;
        }

        private void HandleFirstSightedTeam1()
        {
            if (!_currentRound.Team1.FirstSightTime.HasValue)
                _currentRound.Team1.FirstSightTime = Time.time - _roundStartTime;
        }

        private void HandleDamage(GameObject victim, GameObject attacker, float amount)
        {
            if (attacker == null) return;

            if (attacker == _team0Agent.gameObject)
            {
                _team0HitPending = true;
                _currentRound.Team0.DamageDealt += amount;
            }
            else if (attacker == _team1Agent.gameObject)
            {
                _team1HitPending = true;
                _currentRound.Team1.DamageDealt += amount;
            }
        }

        private void HandleDeath(GameObject victim, GameObject killer)
        {
            if (victim == _team0Agent.gameObject) _currentRound.Team0.Died = true;
            else if (victim == _team1Agent.gameObject) _currentRound.Team1.Died = true;

            if (killer == _team0Agent.gameObject)
            {
                _currentRound.Team0.GotKill = true;
                _currentRound.Team0.KillTime = Time.time - _roundStartTime;
            }
            else if (killer == _team1Agent.gameObject)
            {
                _currentRound.Team1.GotKill = true;
                _currentRound.Team1.KillTime = Time.time - _roundStartTime;
            }
        }

        private void WriteCsv()
        {
            if (!_writeCsvToFile) return;

            StringBuilder csv = new StringBuilder();
            csv.AppendLine("Round,Outcome,DurationSeconds," +
                "Team0_ShotsFired,Team0_ShotsHit,Team0_DamageDealt,Team0_TimeToFirstSight,Team0_FightDuration,Team0_GotKill,Team0_Died," +
                "Team1_ShotsFired,Team1_ShotsHit,Team1_DamageDealt,Team1_TimeToFirstSight,Team1_FightDuration,Team1_GotKill,Team1_Died");

            foreach (RoundRecord round in _completedRounds)
            {
                csv.AppendLine(string.Join(",",
                    round.RoundNumber.ToString(CultureInfo.InvariantCulture),
                    round.Outcome.ToString(),
                    round.DurationSeconds.ToString("F2", CultureInfo.InvariantCulture),
                    FormatBot(round.Team0),
                    FormatBot(round.Team1)));
            }

            string path = Path.Combine(Application.persistentDataPath, _csvFileName);
            File.WriteAllText(path, csv.ToString());
        }

        private static string FormatBot(BotRoundStats stats)
        {
            return string.Join(",",
                stats.ShotsFired.ToString(CultureInfo.InvariantCulture),
                stats.ShotsHit.ToString(CultureInfo.InvariantCulture),
                stats.DamageDealt.ToString("F2", CultureInfo.InvariantCulture),
                stats.FirstSightTime.HasValue ? stats.FirstSightTime.Value.ToString("F2", CultureInfo.InvariantCulture) : "",
                stats.FightDurationSeconds.HasValue ? stats.FightDurationSeconds.Value.ToString("F2", CultureInfo.InvariantCulture) : "",
                stats.GotKill.ToString(),
                stats.Died.ToString());
        }

        private void OnGUI()
        {
            string roundsToWinText = _matchController != null ? _matchController.RoundsToWin.ToString(CultureInfo.InvariantCulture) : "?";

            GUILayout.BeginArea(new Rect(10, 10, 420, 400), GUI.skin.box);
            GUILayout.Label($"Eval session: round {_completedRounds.Count} (first to {roundsToWinText} wins)" + (_sessionFinished ? " (FINISHED)" : ""));

            if (_completedRounds.Count > 0)
            {
                GUILayout.Label(BuildMatchSummary());
                GUILayout.Space(8);
                GUILayout.Label("Team 0:\n" + BuildBotSummary(0));
                GUILayout.Space(8);
                GUILayout.Label("Team 1:\n" + BuildBotSummary(1));
            }

            GUILayout.EndArea();
        }

        private string BuildMatchSummary()
        {
            int draws = 0;
            float totalDuration = 0f;

            foreach (RoundRecord round in _completedRounds)
            {
                if (round.Outcome == MatchOutcome.Draw) draws++;
                totalDuration += round.DurationSeconds;
            }

            float drawPercent = 100f * draws / _completedRounds.Count;
            float avgDuration = totalDuration / _completedRounds.Count;

            return $"Draws: {drawPercent:F1}%\nAvg round time: {avgDuration:F1}s";
        }

        private string BuildBotSummary(int teamId)
        {
            int kills = 0, deaths = 0, shotsFired = 0, shotsHit = 0;
            float totalDamage = 0f, totalTimeToFirstSight = 0f, totalFightDuration = 0f;
            int fightSamples = 0;

            foreach (RoundRecord round in _completedRounds)
            {
                BotRoundStats stats = teamId == 0 ? round.Team0 : round.Team1;

                if (stats.GotKill) kills++;
                if (stats.Died) deaths++;
                shotsFired += stats.ShotsFired;
                shotsHit += stats.ShotsHit;
                totalDamage += stats.DamageDealt;

                // Never found the enemy this round -> treat as "took the whole round to find them".
                totalTimeToFirstSight += stats.FirstSightTime ?? round.DurationSeconds;

                if (stats.FightDurationSeconds.HasValue)
                {
                    totalFightDuration += stats.FightDurationSeconds.Value;
                    fightSamples++;
                }
            }

            float hitPercent = shotsFired > 0 ? 100f * shotsHit / shotsFired : 0f;
            float avgDamage = totalDamage / _completedRounds.Count;
            float avgTimeToFirstSight = totalTimeToFirstSight / _completedRounds.Count;
            float avgFightDuration = fightSamples > 0 ? totalFightDuration / fightSamples : 0f;

            return $"K/D: {kills}/{deaths}\nHit%: {hitPercent:F1}%\nAvg damage/match: {avgDamage:F2}\n" +
                   $"Avg time to find enemy: {avgTimeToFirstSight:F1}s\nAvg fight duration: {avgFightDuration:F1}s";
        }
    }
}
