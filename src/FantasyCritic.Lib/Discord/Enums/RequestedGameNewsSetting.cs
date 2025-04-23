
namespace FantasyCritic.Lib.Discord.Enums;
/// <summary>
/// These Settings are used for Simplification of Game News Settings for Users while Interfacing with the Discord Bot.
/// These settings do not get persisted into the database, and instead will be converted to the normal Game News Settings when needed.
/// </summary>
public class RequestedGameNewsSetting : TypeSafeEnum<RequestedGameNewsSetting>
{
    private static readonly GameNewsSettings _all = new GameNewsSettings()
    {
        AllGameUpdatesEnabled = true,
    };

    private static readonly GameNewsSettings _willReleaseInYear = new GameNewsSettings()
    {
        ShowWillReleaseInYearNews = true,
    };

    private static readonly GameNewsSettings _mightReleaseInYear = new GameNewsSettings()
    {
        ShowWillReleaseInYearNews = true,
        ShowMightReleaseInYearNews = true,
    };

    private static readonly GameNewsSettings _recommended = new GameNewsSettings()
    {
        ShowWillReleaseInYearNews = true,
        ShowMightReleaseInYearNews = true,
        ShowScoreGameNews = true,
        ShowReleasedGameNews = true,
        ShowNewGameNews = true,
        ShowEditedGameNews = true,
    };

    private static readonly GameNewsSettings _leagueGamesOnly = new GameNewsSettings()
    {
        ShowWillReleaseInYearNews = true,
        ShowMightReleaseInYearNews = true,
        ShowScoreGameNews = true,
        ShowReleasedGameNews = true,
        ShowNewGameNews = true,
        ShowEditedGameNews = true,
    };

    private static readonly GameNewsSettings _off = new GameNewsSettings()
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
    private RequestedGameNewsSetting(string value, GameNewsSettings normalSetting)
        : base(value)
    {
        NormalSetting = normalSetting;
    }

    public GameNewsSettings NormalSetting { get; }

    public override string ToString() => Value;

    public GameNewsSettings ToNormalSetting()
    {
        if (NormalSetting == null)
        {
            throw new Exception($"{Value} cannot be converted to a normal setting");
        }

        return NormalSetting;
    }
}
