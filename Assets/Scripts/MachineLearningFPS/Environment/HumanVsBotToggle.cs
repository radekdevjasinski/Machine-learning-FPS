using UnityEngine;
using Unity.MLAgents.Policies;
using MachineLearningFPS.Character;

namespace MachineLearningFPS.Environment
{
    public enum GameMode
    {
        BotVsBot,
        HumanVsBot
    }

    public class HumanVsBotToggle : MonoBehaviour
    {
        [SerializeField] private GameMode _gameMode = GameMode.BotVsBot;
        [SerializeField] private BehaviorParameters _humanControlledAgent;

        public static GameMode ActiveGameMode { get; private set; }
        public static Transform HumanHeadTransform { get; private set; }

        private void Awake()
        {
            GameMode mode = MatchSettings.HasSelection ? MatchSettings.SelectedGameMode : _gameMode;
            ActiveGameMode = mode;

            if (_humanControlledAgent == null) return;

            _humanControlledAgent.BehaviorType = mode == GameMode.HumanVsBot
                ? BehaviorType.HeuristicOnly
                : BehaviorType.Default;

            if (mode == GameMode.HumanVsBot
                && _humanControlledAgent.TryGetComponent(out FPSMovement movement))
            {
                HumanHeadTransform = movement.HeadTransform;
            }
        }
    }
}
