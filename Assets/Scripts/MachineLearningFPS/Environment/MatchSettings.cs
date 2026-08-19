namespace MachineLearningFPS.Environment
{
    public static class MatchSettings
    {
        public static GameMode SelectedGameMode { get; private set; }
        public static bool HasSelection { get; private set; }

        public static void Select(GameMode mode)
        {
            SelectedGameMode = mode;
            HasSelection = true;
        }
    }
}
