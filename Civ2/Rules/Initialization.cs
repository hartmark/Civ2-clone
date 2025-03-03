using Civ2.ImageLoader;
using Civ2engine;
using Civ2engine.Advances;
using Civ2engine.Enums;
using Civ2engine.IO;

namespace Civ2.Rules;

public static class Initialization
{
    private static GameInitializationConfig? _config;
    public static GameInitializationConfig ConfigObject => _config ??= GetConfig() ;

    private static IList<Ruleset>? _ruleSets;
    public static IList<Ruleset> RuleSets => _ruleSets ??= Locator.LocateRules(Settings.SearchPaths);

    private static GameInitializationConfig GetConfig()
    {

        //TODO: Load config
        var config = new GameInitializationConfig();

        return config;
    }

    internal static Game GameInstance = null;

    public static void Start(Game game)
    {
        GameInstance = game;
    }
    
    public static void LoadGraphicsAssets(Civ2Interface civ2Interface)
    {
        ConfigObject.RuleSet ??= RuleSets.First();
        
        ConfigObject.Rules = RulesParser.ParseRules(ConfigObject.RuleSet);
        
        //TODO: Check is interface already hase initialized images and unload them
    
        TerrainLoader.LoadTerrain(ConfigObject.RuleSet, civ2Interface);
        UnitLoader.LoadUnits(ConfigObject.RuleSet, civ2Interface);
        CityLoader.LoadCities(ConfigObject.RuleSet, civ2Interface.CityImages, civ2Interface);
        IconLoader.LoadIcons(ConfigObject.RuleSet, civ2Interface);
        //LoadPeopleIcons(ruleset);
        //LoadCityWallpaper(ruleset);
    }

    public static void CompleteConfig()
    {
        
        ConfigObject.GroupedTribes = ConfigObject.Rules.Leaders
            .ToLookup(g => g.Color);


        var civilizations = new List<Civilization>
        {
            new()
            {
                Adjective = Labels.Items[17], LeaderName = Labels.Items[18], Alive = true, Id = 0,
                PlayerType = PlayerType.Barbarians, Advances = new bool[ConfigObject.Rules.Advances.Length]
            },
            ConfigObject.PlayerCiv
        };

        if (!ConfigObject.SelectComputerOpponents)
        {
            for (var i = 1; civilizations.Count <= ConfigObject.NumberOfCivs; i++)
            {
                if (i == ConfigObject.PlayerCiv.Id) continue;

                var tribes = ConfigObject.GroupedTribes.Contains(i)
                    ? ConfigObject.GroupedTribes[i].ToList()
                    : ConfigObject.Rules.Leaders
                        .Where(leader => civilizations.All(civ => civ.Adjective != leader.Adjective)).ToList();

                civilizations.Add(MakeCivilization(ConfigObject, ConfigObject.Random.ChooseFrom(tribes), false,
                    i));
            }
        }

        ConfigObject.Civilizations = civilizations;
    }

    public static Civilization MakeCivilization(GameInitializationConfig config, LeaderDefaults tribe, bool human, int id)
    {
        var titles = config.Rules.Governments.Select((g, i) => GetLeaderTitle(config, tribe, g, i)).ToArray();
        var gender = human ? config.Gender : tribe.Female ? 1 : 0;
        return new Civilization
        {
            Adjective = tribe.Adjective,
            Alive = true,
            Government = GovernmentType.Despotism,
            Id = id,
            Money = 0,
            Advances = new bool[config.Rules.Advances.Length],
            CityStyle = tribe.CityStyle,
            LeaderGender =gender ,
            LeaderName = gender == 0 ? tribe.NameMale : tribe.NameFemale,
            LeaderTitle = titles[(int)GovernmentType.Despotism],
            LuxRate = 0,
            ScienceRate = 60,
            TaxRate = 40,
            TribeName = tribe.Plural,
            Titles = titles,
            PlayerType = human ? PlayerType.Local : PlayerType.Ai,
            NormalColour = tribe.Color,
            AllowedAdvanceGroups = tribe.AdvanceGroups ?? new [] { AdvanceGroupAccess.CanResearch }
        };
    }
    private static string GetLeaderTitle(GameInitializationConfig config, LeaderDefaults tribe, Government gov, int governmentType)
    {
        var govt = tribe.Titles.FirstOrDefault(t=>t.Gov == governmentType) ?? (IGovernmentTitles)gov;
        return config.Gender == 0 ? govt.TitleMale : govt.TitleFemale;
    }
}