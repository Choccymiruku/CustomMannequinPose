using System.Reflection;
using System.Text.Json.Serialization;
using SPTarkov.Common.Extensions;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Routers.Static;
using SPTarkov.Server.Core.Services.Locales;
using SPTarkov.Server.Core.Utils;
using Path = System.IO.Path;

namespace CustomMannequinPoseServer
{
    public record ModMetadata : IModMetadata
    {
        public string Name { get; init; } = "Mod Name";
        public string Author { get; init; } = "Choccy Milk";

        public List<string>? Contributors { get; init; }
        public SemanticVersioning.Version Version { get; init; } = new("1.0.0");
        public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1");
        public bool HasPrepatcher { get; init; } = false;


        public List<string>? Incompatibilities { get; init; }
        public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
        public string? Url { get; init; }
        public string License { get; init; } = "CC-BY-NC 4.0";
        public string ModGuid { get; init; } = "com.choccy.CustomMannequinPoseServer";
    }

    [Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
    public class Main_Load(
        ISptLogger<Main_Load> logger,
        TemplateTable templateTable, 
        LocaleTable localeTable, 
        LocaleService localeService,
        ServerLocalisationService serverLocalisationService,
        ProfileHelper profileHelper,
        ModHelper modHelper) : IOnLoad
    {
        
        public Task OnLoadAsync(CancellationToken cancellationToken)
        {
            AddPoses();
            return Task.CompletedTask;
        }

        private void AddPoses()
        {
            var customization = templateTable.Customization;
            logger.Info("Pass Customization");
            var localization = localeTable.Global;
            logger.Info("Pass Localization");
            var locale = localeService;
            var path = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
            logger.Info("Pass get assembly path");
            //var jsonPath = Path.Combine(path, "PoseList.json");
            
            var poseData = modHelper.GetJsonDataFromFile<PoseList>(path, "PoseList.json");
            logger.Info($"Loading poses from {poseData.Name.Values}");
            Dictionary<MongoId, CustomizationItem> poses = poseData.Poses;
            //Add poses to customization
            foreach (var gesture in poses)
            {
                customization[gesture.Key] = gesture.Value;
            }

            /*logger.Info($"Check Pose: {customization["680139cbbd3f904e3052d991"].Properties.MannequinPoseName}");*/

            Dictionary<string, string> poseName = poseData.Name;
            //add localization
            foreach (var lcl in localization)
            {
                if (localization.TryGetValue(lcl.Key, out var lang))
                {
                    foreach (var name in poseName)
                    {
                        lang.AddTransformer(localizationName =>
                        {
                            localizationName.Add(name.Key, name.Value);
                            return localizationName;
                        });
                    }
                }
            }

            var poseLocales = localeService.GetLocaleDb("en");
            /*logger.Info(poseLocales["Hideout/Mannequin/Pose/rude_gesture"]);
            logger.Info(serverLocalisationService.GetText("Hideout/Mannequin/Pose/rude_gesture"));*/
            
            /*var profiles = profileHelper.GetProfiles();
            List<CustomisationStorage> unlocks = poseData.Unlocks;
            foreach (var (profile, value) in profiles)
            {
                foreach (var poseUnlock in unlocks)
                {
                    profiles.TryGetValue(profile, out var profileData);
                    if (profileData.CustomisationUnlocks != null)
                    {
                        var dupe = profileData.CustomisationUnlocks.Where(x => x.Id != poseUnlock.Id);
                        if (!dupe.Any())
                        {
                            profileData.CustomisationUnlocks.AddRange(dupe);
                        }
                    }
                }
            }*/
        }
    }

    public record PoseList
    {
        [JsonPropertyName("Poses")]
        public Dictionary<MongoId, CustomizationItem> Poses { get; set; }
        [JsonPropertyName("Name")]
        public Dictionary<string, string> Name { get; set; }
        [JsonPropertyName("PoseUnlock")]
        public List<CustomisationStorage> Unlocks { get; set; }
    }

    [Injectable(TypePriority = OnLoadOrder.Routers + 29)]
    public class Router(
        ISptLogger<Router> logger,
        TemplateTable templateTable, 
        LocaleTable localeTable, 
        JsonUtil jsonUtil,
        ProfileHelper profileHelper, 
        LocaleService localeService,
        ServerLocalisationService serverLocalisationService,
        ModHelper modHelper) : StaticRouter (jsonUtil, 
        [new RouteAction("/client/game/start",
                async (url, info, sessionId, output, token) =>
                {
                    var profiles = profileHelper.GetProfiles();
                    var path = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
                    var poseData = modHelper.GetJsonDataFromFile<PoseList>(path, "PoseList.json");
                    List<CustomisationStorage> unlocks = poseData.Unlocks;
                    foreach (var (profile, value) in profiles)
                    {
                        foreach (var poseUnlock in unlocks)
                        {
                            profiles.TryGetValue(profile, out var profileData);
                            if (profileData.CustomisationUnlocks != null)
                            {
                                var dupe = profileData.CustomisationUnlocks;
                                if (!dupe.All(x => x.Id == poseUnlock.Id))
                                {
                                    profileData.CustomisationUnlocks.Add(poseUnlock);
                                }
                            }
                        }
                    }
                    return output;
                }, typeof(GameStaticRouter))
        ]
    )
    {
        
    }

    /*[Injectable]
    public class AddPoseToExistingProfile(ProfileHelper profileHelper, JsonUtil jsonUtil, ModHelper modHelper)
    {
        
        public ValueTask<string> AddToExisting(string url, PoseUnlocks poseUnlocks, MongoId sessionId)
        {
            var profileHelper = ProfileHelper
        }
    }

    public record PoseUnlocks : IRequestData
    {
        
    }*/
}