using System;
using System.Collections.Generic;
using System.Text;
using CSharpFunctionalExtensions;
using FantasyCritic.Lib.Domain.ScoringSystems;
using FantasyCritic.Lib.Enums;
using NodaTime;

namespace FantasyCritic.Lib.Domain
{
    public class PublisherGame
    {
        public PublisherGame(Guid publisherID, Guid publisherGameID, string gameName, Instant timestamp, bool counterPick, decimal? manualCriticScore, bool manualWillNotRelease,
            decimal? fantasyPoints, Maybe<MasterGameYear> masterGame, int? draftPosition, int? overallDraftPosition)
        {
            PublisherID = publisherID;
            PublisherGameID = publisherGameID;
            GameName = gameName;
            Timestamp = timestamp;
            CounterPick = counterPick;
            ManualCriticScore = manualCriticScore;
            ManualWillNotRelease = manualWillNotRelease;
            FantasyPoints = fantasyPoints;
            MasterGame = masterGame;
            DraftPosition = draftPosition;
            OverallDraftPosition = overallDraftPosition;
        }

        public Guid PublisherID { get; }
        public Guid PublisherGameID { get; }
        public string GameName { get; }
        public Instant Timestamp { get; }
        public bool CounterPick { get; }
        public decimal? ManualCriticScore { get; }
        public bool ManualWillNotRelease { get; }
        public decimal? FantasyPoints { get; }
        public Maybe<MasterGameYear> MasterGame { get; }
        public int? DraftPosition { get; }
        public int? OverallDraftPosition { get; }

        public bool WillRelease()
        {
            if (ManualWillNotRelease)
            {
                return false;
            }

            if (MasterGame.HasNoValue)
            {
                return false;
            }

            return MasterGame.Value.WillRelease();
        }

        public decimal GetProjectedOrRealFantasyPoints(ScoringSystem scoringSystem, SystemWideValues systemWideValues, bool simpleProjections, IClock clock)
        {
            if (MasterGame.HasNoValue)
            {
                return systemWideValues.GetAveragePoints(CounterPick);
            }

            decimal? fantasyPoints = CalculateFantasyPoints(scoringSystem, clock);
            if (fantasyPoints.HasValue)
            {
                return fantasyPoints.Value;
            }

            if (simpleProjections)
            {
                return MasterGame.Value.GetSimpleProjectedFantasyPoints(systemWideValues, CounterPick);
            }

            return MasterGame.Value.GetProjectedOrRealFantasyPoints(scoringSystem, CounterPick, clock);
        }

        public decimal? CalculateFantasyPoints(ScoringSystem scoringSystem, IClock clock)
        {
            if (ManualCriticScore.HasValue)
            {
                return scoringSystem.GetPointsForScore(ManualCriticScore.Value, CounterPick); 
            }
            if (MasterGame.HasNoValue)
            {
                return null;
            }

            return MasterGame.Value.CalculateFantasyPoints(scoringSystem, CounterPick, clock, true);
        }

        public override string ToString() => GameName;
    }
}
