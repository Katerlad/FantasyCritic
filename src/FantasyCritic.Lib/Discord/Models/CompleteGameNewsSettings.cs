using FantasyCritic.Lib.Discord.Enums;
using System.Text;


namespace FantasyCritic.Lib.Discord.Models;
public class CompleteGameNewsSettings
{
    public bool EnableGameNews { get; set; }
    public bool? ShowPickedGameNews { get; set; } = null;
    public bool? ShowEligibleGameNews { get; set; } = null;
    public bool? ShowCurrentYearGameNewsOnly { get; set; } = null;
    public NotableMissSetting? NotableMissSetting { get; set; } = null;
    public bool ShowMightReleaseInYearNews { get; set; }
    public bool ShowWillReleaseInYearNews { get; set; }
    public bool ShowScoreGameNews { get; set; }
    public bool ShowAlreadyReleasedGameNews { get; set; }
    public bool ShowNewGameNews { get; set; }
    public bool ShowEditedGameNews { get; set; }
    public List<MasterGameTag> SkippedTags { get; set; } = new List<MasterGameTag>();

    public bool Recommended
    {
        get
        {
            return (ShowPickedGameNews == true || ShowPickedGameNews == null) &&
                   (ShowEligibleGameNews == true || ShowEligibleGameNews == null) &&
                   (ShowCurrentYearGameNewsOnly == false || ShowCurrentYearGameNewsOnly == null) &&
                   (NotableMissSetting == NotableMissSetting.ScoreUpdates || NotableMissSetting == null) &&
                   ShowMightReleaseInYearNews &&
                   ShowWillReleaseInYearNews &&
                   ShowScoreGameNews &&
                   ShowAlreadyReleasedGameNews &&
                   ShowNewGameNews &&
                   ShowEditedGameNews &&
                   SkippedTags.Count == 0;
        }
        set
        {
            if (value)
            {
                ShowPickedGameNews = ShowPickedGameNews == null ? null : true;
                ShowEligibleGameNews = ShowEligibleGameNews == null ? null : true;
                ShowCurrentYearGameNewsOnly = ShowCurrentYearGameNewsOnly == null ? null : false;
                NotableMissSetting = NotableMissSetting == null ? null : NotableMissSetting.ScoreUpdates;
                EnableGameNews = true;
                ShowMightReleaseInYearNews = true;
                ShowWillReleaseInYearNews = true;
                ShowScoreGameNews = true;
                ShowAlreadyReleasedGameNews = true;
                ShowNewGameNews = true;
                ShowEditedGameNews = true;
                SkippedTags = new();
            }
        }
    }

    public void SetLeagueRecommendedSettings()
    {
        EnableGameNews = true;
        ShowPickedGameNews = true;
        ShowEligibleGameNews = true;
        ShowCurrentYearGameNewsOnly = false;
        NotableMissSetting = NotableMissSetting.ScoreUpdates;
        ShowMightReleaseInYearNews = true;
        ShowWillReleaseInYearNews = true;
        ShowScoreGameNews = true;
        ShowAlreadyReleasedGameNews = true;
        ShowNewGameNews = true;
        ShowEditedGameNews = true;
    }

    public GameNewsSettingsRecord ToGameNewsSettings()
    {
        return new GameNewsSettingsRecord()
        {
            EnableGameNews = EnableGameNews,
            ShowMightReleaseInYearNews = ShowMightReleaseInYearNews,
            ShowWillReleaseInYearNews = ShowWillReleaseInYearNews,
            ShowScoreGameNews = ShowScoreGameNews,
            ShowReleasedGameNews = ShowAlreadyReleasedGameNews,
            ShowEditedGameNews = ShowEditedGameNews,
            ShowNewGameNews = ShowNewGameNews,
            SkippedTags = SkippedTags
        };
    }
    public string ToDiscordMessage()
    {
        string GetEmoji(bool? setting) => setting == true ? "✅" : setting == false ? "❌" : string.Empty;

        var embedMessage = new StringBuilder();
        embedMessage.AppendLine("\n**Current Game News Settings**");
        embedMessage.AppendLine("------------------------------");

        embedMessage.AppendLine("\n**General News Settings:**");
        embedMessage.AppendLine($"  -- Game News Enabled: {(EnableGameNews == false ? "**False**" : "**True**")}");
        embedMessage.AppendLine($"  -- Is League Channel: {(ShowEligibleGameNews != null ? "**True**" : "**False**")}");
        embedMessage.AppendLine($"  -- Setting State: {(Recommended == true ? "**Recommended**" : "**Custom**")}");


        //If eligiblegame news is null that should be an idicator that this is not a league channel
        if (ShowEligibleGameNews != null)
        {
            embedMessage.AppendLine("\n**LeagueChannel Settings:**");
            embedMessage.AppendLine($"  -- {GetEmoji(ShowPickedGameNews)} Show Picked Game News");
            embedMessage.AppendLine($"  -- {GetEmoji(ShowEligibleGameNews)} Show Eligible Game News");
            embedMessage.AppendLine($"  -- {GetEmoji(ShowCurrentYearGameNewsOnly)} Show Current Year Game News Only");
        }

        if (NotableMissSetting != null)
        {
            embedMessage.AppendLine($"    --- Notable Miss Setting: **{NotableMissSetting.ReadableName}**");
        }

        embedMessage.AppendLine("\n** Game Release Settings:**");
        embedMessage.AppendLine($"  -- {GetEmoji(ShowNewGameNews)} Show New Game News");
        embedMessage.AppendLine($"  -- {GetEmoji(ShowMightReleaseInYearNews)} Show Might Release In Year News");
        embedMessage.AppendLine($"  -- {GetEmoji(ShowWillReleaseInYearNews)} Show Will Release In Year News");
        embedMessage.AppendLine($"  -- {GetEmoji(ShowAlreadyReleasedGameNews)} Show Released Game News");

        embedMessage.AppendLine("\n**Game Update Settings:**");
        embedMessage.AppendLine($"  -- {GetEmoji(ShowScoreGameNews)} Show Score Game News");
        embedMessage.AppendLine($"  -- {GetEmoji(ShowEditedGameNews)} Show Edited Game News");

        embedMessage.AppendLine("\n**Skipped Tags:**");
        if (SkippedTags != null && SkippedTags.Any())
        {
            
            foreach (var tag in SkippedTags)
            {
                embedMessage.AppendLine($"- {tag.ReadableName}");
            }
        }
        else
        {
            embedMessage.AppendLine("  -- None");
        }

            embedMessage.AppendLine("------------------------");

        return embedMessage.ToString();
    }


}
