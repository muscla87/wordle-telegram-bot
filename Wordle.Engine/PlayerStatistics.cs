using Microsoft.Azure.CosmosRepository;
using Wordle.Engine.Dictionaries;

namespace Wordle.Engine
{
    public class PlayerStatistics : Item
    {
        public string ChatId { get; set; } = string.Empty;
        public string DictionaryName { get; set; } = EnglishWordleOriginal.Instance.Name;
        public string GameRoomId { get; set; } = string.Empty;
        public string GameMode { get; set; } = "Practice";
        public int WordLength { get; set; }
        public int MaxAttemptsCount { get; set; }


        public int PlayedGamesCount { get; set; }
        public int WonGamesCount { get; set; }
        public int[] GamesWonPerAttempt { get; set; } = new int[0];
        public int BestStreak { get; set; }
        public int CurrentStreak { get; set; }
        public int TotalPoints { get { return CalculateTotalPoints(); } }

        private int CalculateTotalPoints()
        {
            int totalPoints = 0;

            for (int i = 0; i < GamesWonPerAttempt.Length; i++)
            {
                totalPoints += GamesWonPerAttempt[i] * (GamesWonPerAttempt.Length - i);
            }

            return totalPoints;
        }
    }
}