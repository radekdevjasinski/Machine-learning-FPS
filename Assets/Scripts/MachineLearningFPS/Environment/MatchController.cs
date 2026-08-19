using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MachineLearningFPS.Environment
{
    public enum MatchStage
    {
        Prepare,
        Gameplay,
        RoundEnd,
        Ending
    }
    public enum MatchOutcome
    {
        None,
        Draw,
        Team0Win,
        Team1Win
    }

    public class MatchController : MonoBehaviour
    {
        [SerializeField] private float _prepareSeconds = 3f;
        [SerializeField] private float _roundEndSeconds = 3f;
        [SerializeField] private int _roundsToWin = 5;
        [SerializeField] private float _matchEndDelaySeconds = 5f;
        [SerializeField] private string _mainMenuSceneName = "main menu";

        private int _roundNumber = 0;
        private bool _isTransitioning;

        public static bool InputBlocked { get; private set; }
        public static event Action<string, float> OnStageMessage;
        public static event Action<int, int> OnScoreChanged;
        public MatchStage CurrentStage { get; private set; } = MatchStage.Prepare;
        public int Team0Score { get; private set; }
        public int Team1Score { get; private set; }

        public void RunTransition(MatchOutcome outcome, Action performReset)
        {
            if (_isTransitioning) return;
            StartCoroutine(TransitionCoroutine(outcome, performReset));
        }

        private IEnumerator TransitionCoroutine(MatchOutcome outcome, Action performReset)
        {
            _isTransitioning = true;
            InputBlocked = true;

            if (outcome != MatchOutcome.None)
            {
                CurrentStage = MatchStage.RoundEnd;
                ApplyScore(outcome);
                LogStage(BuildOutcomeText(outcome), _roundEndSeconds);
                yield return new WaitForSeconds(_roundEndSeconds);

                MatchOutcome matchWinner = GetMatchWinner();
                if (matchWinner != MatchOutcome.None)
                {
                    yield return EndMatch(matchWinner);
                    yield break;
                }
            }

            performReset?.Invoke();
            _roundNumber++;

            CurrentStage = MatchStage.Prepare;
            LogStage($"prepare for round {_roundNumber}", _prepareSeconds);
            yield return new WaitForSeconds(_prepareSeconds);

            InputBlocked = false;
            CurrentStage = MatchStage.Gameplay;
            LogStage($"round {_roundNumber} started", _prepareSeconds);

            _isTransitioning = false;
        }

        private IEnumerator EndMatch(MatchOutcome winner)
        {
            CurrentStage = MatchStage.Ending;
            LogStage(BuildMatchEndText(winner), _matchEndDelaySeconds);
            yield return new WaitForSeconds(_matchEndDelaySeconds);

            SceneManager.LoadScene(_mainMenuSceneName);
        }

        private MatchOutcome GetMatchWinner()
        {
            if (Team0Score >= _roundsToWin) return MatchOutcome.Team0Win;
            if (Team1Score >= _roundsToWin) return MatchOutcome.Team1Win;
            return MatchOutcome.None;
        }

        private void ApplyScore(MatchOutcome outcome)
        {
            if (outcome == MatchOutcome.Team0Win) Team0Score++;
            else if (outcome == MatchOutcome.Team1Win) Team1Score++;
            else return;

            OnScoreChanged?.Invoke(Team0Score, Team1Score);
        }

        private static void LogStage(string message, float displaySeconds)
        {
            Debug.Log(message);
            OnStageMessage?.Invoke(message, displaySeconds);
        }

        private static string TeamName(MatchOutcome outcome) => outcome == MatchOutcome.Team0Win ? "blue" : "orange";

        private static string BuildOutcomeText(MatchOutcome outcome)
        {
            return outcome switch
            {
                MatchOutcome.Draw => "round ended in a draw",
                MatchOutcome.Team0Win => $"{TeamName(outcome)} won",
                MatchOutcome.Team1Win => $"{TeamName(outcome)} won",
                _ => "round ended"
            };
        }

        private static string BuildMatchEndText(MatchOutcome winner) => $"{TeamName(winner)} won the match!";
    }
}
