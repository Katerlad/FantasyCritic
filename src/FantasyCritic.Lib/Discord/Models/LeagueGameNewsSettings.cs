using FantasyCritic.Lib.Discord.Enums;

namespace FantasyCritic.Lib.Discord.Models;
public class LeagueGameNewsSettings
{
    public bool ShowEligibleGameNewsOnly { get;  }

    public bool ShowCurrentYearGameNewsOnly { get; }

    public NotableMissSetting NotableMissSetting { get; } = NotableMissSetting.None;
}
