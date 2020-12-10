using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FantasyCritic.Lib.Domain;
using FantasyCritic.Lib.Domain.ScoringSystems;
using FantasyCritic.Lib.Enums;

namespace FantasyCritic.Web.Models.Responses
{
    public class LeagueOptionsViewModel
    {
        public LeagueOptionsViewModel(IEnumerable<int> openYears, IEnumerable<DraftSystem> draftSystems,
            IEnumerable<PickupSystem> pickupSystems, IEnumerable<ScoringSystem> scoringSystems)
        {
            OpenYears = openYears.ToList();
            DraftSystems = draftSystems.Select(x => x.Value).ToList();
            PickupSystems = pickupSystems.Select(x => x.Value).ToList();
            ScoringSystems = scoringSystems.Select(x => x.Name).ToList();
        }

        public IReadOnlyList<int> OpenYears { get; }
        public IReadOnlyList<string> DraftSystems { get; }
        public IReadOnlyList<string> PickupSystems { get; }
        public IReadOnlyList<string> ScoringSystems { get; }
    }
}
