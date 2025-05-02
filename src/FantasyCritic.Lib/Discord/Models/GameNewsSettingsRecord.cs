

public record GameNewsSettingsRecord
{
    public bool EnableGameNews { get; set; }
    public bool ShowMightReleaseInYearNews { get; set; }
    public  bool ShowWillReleaseInYearNews { get; set; }
    public bool ShowScoreGameNews { get; set; }
    public bool ShowReleasedGameNews { get; set; }
    public bool ShowNewGameNews { get; set; }
    public bool ShowEditedGameNews { get; set; }
    public List<MasterGameTag> SkippedTags { get; set; } = new List<MasterGameTag>();
    public bool AllGameUpdatesEnabled { get => GetAllGameUpdatesEnabled(); set => SetAllGameUpdatesEnabled(value); }

    

    private void SetAllGameUpdatesEnabled(bool value)
    {
        ShowWillReleaseInYearNews = value;
        ShowMightReleaseInYearNews = value;
        ShowScoreGameNews = value;
        ShowReleasedGameNews = value;
        ShowNewGameNews = value;
        ShowEditedGameNews = value;
        if(value == true) SkippedTags.Clear();
    }

    private bool GetAllGameUpdatesEnabled()
    {
        return ShowWillReleaseInYearNews
        && ShowMightReleaseInYearNews
        && ShowScoreGameNews
        && ShowReleasedGameNews
        && ShowNewGameNews
        && ShowEditedGameNews
        && SkippedTags.Count == 0;
    }

}
