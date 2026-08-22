using System.Reflection;
using System.Text.Json.Serialization;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Routers.Static;
using SPTarkov.Server.Core.Services.Locales;
using SPTarkov.Server.Core.Utils;

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
            //logger.Info("Pass Customization");
            var localization = localeTable.Global;
            //logger.Info("Pass Localization");
            var path = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
            //logger.Info("Pass get assembly path");
            //var jsonPath = Path.Combine(path, "PoseList.json");
            var customizationList = templateTable.CustomisationStorage;
            
            var poseData = modHelper.GetJsonDataFromFile<PoseList>(path, "PoseList.json");
            //logger.Info($"Loading poses from {poseData.Name.Values}");
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
            
            //Turns out the default customisation storage is stored in CustomisationStorage.json
            List<CustomisationStorage> customizationUnlocks = poseData.Unlocks;
            foreach (var unlocks in customizationUnlocks)
            {
                customizationList.Add(unlocks);
            }
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

    //Hooks into the client game start router and modify every profile that has
    //customisation unlock available and add our poses there cause for some reason even with AvailableAsDefault is true
    //It still needs to be added
    //above statement is no longer true, but i am keeping it here just in case someone wants to check it out on how to hook into router idk
    /*[Injectable(TypePriority = OnLoadOrder.Routers + 29)]
    public class Router(
        JsonUtil jsonUtil,
        ProfileHelper profileHelper,
        ModHelper modHelper) : StaticRouter (jsonUtil, 
        [new RouteAction("/client/game/start",
                async (url, info, sessionId, output, token) =>
                {
                    var profiles = profileHelper.GetProfiles();
                    var path = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
                    var poseData = modHelper.GetJsonDataFromFile<PoseList>(path, "PoseList.json");
                    List<CustomisationStorage> unlocks = poseData.Unlocks;
                    foreach (var profile in profiles)
                    {
                        foreach (var poseUnlock in unlocks)
                        {
                            profiles.TryGetValue(profile.Key, out var profileData);
                            if (profileData?.CustomisationUnlocks != null)
                            {
                                var dupe = profileData.CustomisationUnlocks;
                                if (dupe.All(x => x.Id != poseUnlock.Id))
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
        
    }*/
}