
namespace FantasyCritic.Lib.Discord.Enums;
/// <summary>
/// These Settings are used for Simplification of Game News Settings for Users while Interfacing with the Discord Bot.
/// These settings do not get persisted into the database, and instead will be converted to the normal Game News Settings when needed.
/// </summary>
public class RequestedGameNewsSetting : TypeSafeEnum<RequestedGameNewsSetting>
{
    private static readonly GameNewsSettingsRecord _all = new GameNewsSettingsRecord()
    {
        AllGameUpdatesEnabled = true,
    };

    private static readonly GameNewsSettingsRecord _willReleaseInYear = new GameNewsSettingsRecord()
    {
        ShowWillReleaseInYearNews = true,
    };

    private static readonly GameNewsSettingsRecord _mightReleaseInYear = new GameNewsSettingsRecord()
    {
        ShowWillReleaseInYearNews = true,
        ShowMightReleaseInYearNews = true,
    };

    private static readonly GameNewsSettingsRecord _recommended = new GameNewsSettingsRecord()
    {
        ShowWillReleaseInYearNews = true,
        ShowMightReleaseInYearNews = true,
        ShowScoreGameNews = true,
        ShowReleasedGameNews = true,
        ShowNewGameNews = true,
        ShowEditedGameNews = true,
    };

    private static readonly GameNewsSettingsRecord _leagueGamesOnly = new GameNewsSettingsRecord()
    {
        ShowWillReleaseInYearNews = true,
        ShowMightReleaseInYearNews = true,
        ShowScoreGameNews = true,
        ShowReleasedGameNews = true,
        ShowNewGameNews = true,
        ShowEditedGameNews = true,
    };

    private static readonly GameNewsSettingsRecord _off = new GameNewsSettingsRecord()
    {
        AllGameUpdatesEnabled = false,
    };


    // Define Enum values here.
    public static readonly RequestedGameNewsSetting All = new RequestedGameNewsSetting("All", _all);
    public static readonly RequestedGameNewsSetting WillReleaseInYear = new RequestedGameNewsSetting("WillReleaseInYear", _willReleaseInYear);
    public static readonly RequestedGameNewsSetting MightReleaseInYear = new RequestedGameNewsSetting("MightReleaseInYear", _mightReleaseInYear);
    public static readonly RequestedGameNewsSetting Recommended = new RequestedGameNewsSetting("Recommended",_recommended);
    public static readonly RequestedGameNewsSetting LeagueGamesOnly = new RequestedGameNewsSetting("LeagueGamesOnly", _leagueGamesOnly);
    public static readonly RequestedGameNewsSetting Off = new RequestedGameNewsSetting("Off", _off);

    // Constructor is private: values are defined within this class only!
    private RequestedGameNewsSetting(string value, GameNewsSettingsRecord normalSetting)
        : base(value)
    {
        NormalSetting = normalSetting;
    }

    public GameNewsSettingsRecord NormalSetting { get; }

    public override string ToString() => Value;

    public GameNewsSettingsRecord ToNormalSetting()
    {
        if (NormalSetting == null)
        {
            throw new Exception($"{Value} cannot be converted to a normal setting");
        }

        return NormalSetting;
    }
}
