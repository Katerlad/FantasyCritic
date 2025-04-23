
using FantasyCritic.Lib.Discord.Enums;

namespace FantasyCritic.Lib.Discord.Models;
public class AdvancedGameNewsSettings
{
    //nullable for when league updates are turned off
    public bool LeagueGameNewsEnabled { get; set; }
    public bool MightReleaseInYearEnabled { get; set; }
    public bool WillReleaseInYearEnabled { get; set; }
    public bool UnannouncedGameNewsEnabled { get; set; }
    public bool AllGameUpdatesEnabled { get => GetAllGameUpdatesEnabled(); set => SetAllGameUpdatesEnabled(value); }
    public bool AllNonLeagueGameUpdatesEnabled{get => GetAllNonLeagueGameUpdatesEnabled(); set => SetAllNonLeagueGameUpdatesEnabled(value); }
    public NotableMissesSetting NotableMissSetting { get; set; }

    public AdvancedGameNewsSettings(MultiYearLeagueChannel? leagueChannel, GameNewsChannel? gameChannel )
    {
        LeagueGameNewsEnabled = leagueChannel?.SendLeagueMasterGameUpdates ?? false;
        NotableMissSetting = leagueChannel?.NotableMissesSetting ?? NotableMissesSetting.None;
        MightReleaseInYearEnabled = gameChannel?.AdvancedGameNewsSettings.MightReleaseInYearEnabled ?? false;
        WillReleaseInYearEnabled = gameChannel?.AdvancedGameNewsSettings.WillReleaseInYearEnabled ?? false;
        UnannouncedGameNewsEnabled = gameChannel?.AdvancedGameNewsSettings.UnannouncedGameNewsEnabled ?? false;
    }
    private bool GetAllNonLeagueGameUpdatesEnabled()
    {
        return WillReleaseInYearEnabled && MightReleaseInYearEnabled && UnannouncedGameNewsEnabled;
    }

    private void SetAllNonLeagueGameUpdatesEnabled(bool value)
    {
        WillReleaseInYearEnabled = value;
        MightReleaseInYearEnabled = value;
        UnannouncedGameNewsEnabled = value;
    }


    private void SetAllGameUpdatesEnabled(bool value)
    {
        WillReleaseInYearEnabled = value;
        MightReleaseInYearEnabled = value;
        UnannouncedGameNewsEnabled = value;
        NotableMissSetting = value ? NotableMissesSetting.All : NotableMissesSetting.None;
    }

    private bool GetAllGameUpdatesEnabled()
    {
        return WillReleaseInYearEnabled
        && MightReleaseInYearEnabled
        && UnannouncedGameNewsEnabled
        && NotableMissSetting == NotableMissesSetting.All;
    }

    


}
