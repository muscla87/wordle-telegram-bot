namespace Wordle.Engine
{
    public class PlayerStatisticsWithPositions : PlayerStatistics
    {
        public int WinRatePosition { get; set; }
        public int BestStreakPosition { get; set; } 
        public int PointsPosition { get; set; }

        public PlayerStatisticsWithPositions(PlayerStatistics playerStatistics)
        {
            ChatId = playerStatistics.ChatId;
            DictionaryName = playerStatistics.DictionaryName;
            GameRoomId = playerStatistics.GameRoomId;
            GameMode = playerStatistics.GameMode;
            WordLength = playerStatistics.WordLength;
            MaxAttemptsCount = playerStatistics.MaxAttemptsCount;

            PlayedGamesCount = playerStatistics.PlayedGamesCount;
            WonGamesCount = playerStatistics.WonGamesCount;
            GamesWonPerAttempt = playerStatistics.GamesWonPerAttempt;
            BestStreak = playerStatistics.BestStreak;
            CurrentStreak = playerStatistics.CurrentStreak;
        }
    }
}