using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using Toolsmith.Client;
using Toolsmith.Client.Behaviors;
using Toolsmith.Config;
using Toolsmith.Server;
using Toolsmith.ToolTinkering;
using Toolsmith.ToolTinkering.Behaviors;
using Toolsmith.ToolTinkering.Blocks;
using Toolsmith.ToolTinkering.Items;
using Toolsmith.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Vintagestory.Server;
using Vintagestory.ServerMods;

namespace Toolsmith {
    public class ToolsmithModSystem : ModSystem {

        public static ILogger Logger;
        public static string ModId;
        public static string ModVersion;
        public static ICoreAPI Api => Sapi != null ? Sapi : Capi;
        public static ICoreServerAPI Sapi;
        public static ICoreClientAPI Capi;
        public static Harmony HarmonyInstance;
        public static ToolsmithConfigs Config;
        public static ToolsmithClientConfigs ClientConfig; //Any setting in here is NOT synced from the server, and intended to control client-side changes only.
        public static ToolsmithPartStats Stats;
        public static int GradientSelection = 0;
        public static bool DoesConfigNeedRegen = false;
        public static bool IgnoreAttributesAdded = false;

        public const string ToolTinkeringDamagePatchCategory = "toolTinkeringDamage";
        public const string ToolTinkeringToolUseStatsPatchCategory = "toolTinkeringToolUseStats";
        public const string ToolTinkeringTransitionalPropsPatchCategory = "toolTinkeringTransitionalProps";
        public const string ToolTinkeringCraftingPatchCategory = "toolTinkeringCrafting";
        public const string ToolTinkeringRenderPatchCategory = "toolTinkeringRender";
        public const string ToolTinkeringGuiElementPatchCategory = "toolTinkeringGuiElement";
        public const string ToolTinkeringItemAxePatchCategory = "itemAxeOnBrokenWith";

        public const string ToolRenderingPatchCategory = "toolPartRendering";

        public const string OffhandDominantInteractionUsePatchCategory = "offhandDominantInteractionUse";

        public static List<string> IgnoreCodes;
        public static List<string> ToolsWithWoodInBindingShapes; //Used on both sides - since Grid Crafting is clientside.
        public static Dictionary<string, int> BindingTiers; //This is only initialized on the Client side! Used for just generating and storing the various bindings tier levels to display on their tooltips.

        public override void StartPre(ICoreAPI api) {
            Logger = Mod.Logger;
            ModId = Mod.Info.ModID;
            ModVersion = Mod.Info.Version;
            if (api as ICoreServerAPI != null) {
                Sapi = api as ICoreServerAPI;
            } else {
                Capi = api as ICoreClientAPI;
            }
            TryToLoadConfig(api);
            TryToLoadClientConfig(api);
            TryToLoadStats(api);
            IgnoreCodes = new List<string>();
            ToolsWithWoodInBindingShapes = new List<string>();

            ConfigUtility.PrepareAndSplitConfigStrings(); //After this point, mods and anyone can add to the config strings!
        }

        public override void Start(ICoreAPI api) {
            if (api.ModLoader.IsModEnabled("smithingplus")) {
                Logger.VerboseDebug("Smithing Plus found, trying to patch in attributes to the forgettable config!");
                HandleSmithingPlusStartCompat(api);
                api.World.Config.SetBool(ToolsmithConstants.SmithWithBitsEnabled, false); //If Smithing Plus is enabled, just always defer to it for Smithing With Bits.
            } else {
                api.World.Config.SetBool(ToolsmithConstants.SmithWithBitsEnabled, Config.UseBitsForSmithing); //If it's not, then check the config.
            }
            
            if (!ClientConfig.DisableMultiPartRendering) {
                api.World.Config.SetBool(ToolsmithConstants.DisabledMultiPartRenders, true);
            } else {
                api.World.Config.SetBool(ToolsmithConstants.DisabledMultiPartRenders, false);
            }

            //This is important to let the Treasure Hunter Trader accept a Toolsmith Pick to get the map for Story Content! Thank you Item Rarity for also having the issue and both leaving a comment in their code and pushing the commit not too long before I had the same problem :P
            if (!IgnoreAttributesAdded) { //This bool is entirely for checking if it's singleplayer or not, since we don't want to run it twice if it is. That'll just double up the attributes in the list.
                var globalLen = GlobalConstants.IgnoredStackAttributes.Length;
                string[] ignoreTheseAttributes = new string[globalLen + ToolsmithAttributes.ToolsmithIgnoreAttributesArray.Length];
                for (int i = 0; i < globalLen; i++) {
                    ignoreTheseAttributes[i] = GlobalConstants.IgnoredStackAttributes[i];
                }
                for (int i = globalLen; i < ToolsmithAttributes.ToolsmithIgnoreAttributesArray.Length + globalLen; i++) {
                    ignoreTheseAttributes[i] = ToolsmithAttributes.ToolsmithIgnoreAttributesArray[i - globalLen];
                }
                GlobalConstants.IgnoredStackAttributes = ignoreTheseAttributes;
                IgnoreAttributesAdded = true;
            }

            //Tool Tinkering general Behaviors
            api.RegisterCollectibleBehaviorClass($"{ModId}:TinkeredTools", typeof(CollectibleBehaviorTinkeredTools));
            api.RegisterCollectibleBehaviorClass($"{ModId}:ToolPartWithHealth", typeof(CollectibleBehaviorToolPartWithHealth));
            api.RegisterCollectibleBehaviorClass($"{ModId}:ToolLowDamageWithUse", typeof(CollectibleBehaviorToolBlunt));
            api.RegisterCollectibleBehaviorClass($"{ModId}:SmithedTool", typeof(CollectibleBehaviorSmithedTools));
            api.RegisterCollectibleBehaviorClass($"{ModId}:ToolHead", typeof(CollectibleBehaviorToolHead));
            api.RegisterCollectibleBehaviorClass($"{ModId}:ToolHandle", typeof(CollectibleBehaviorToolHandle));
            api.RegisterCollectibleBehaviorClass($"{ModId}:ToolBinding", typeof(CollectibleBehaviorToolBinding));

            //Utility Behaviors
            api.RegisterCollectibleBehaviorClass($"{ModId}:OffhandDominantInteraction", typeof(CollectibleBehaviorOffhandDominantInteraction));

            //Rendering-Based Behaviors
            api.RegisterCollectibleBehaviorClass($"{ModId}:ModularPartRenderingFromAttributes", typeof(ModularPartRenderingFromAttributes));

            //Blocks and Items registry
            api.RegisterBlockEntityClass($"{ModId}:EntityGrindstone", typeof(BlockEntityGrindstone));
            api.RegisterBlockEntityClass($"{ModId}:EntityWorkbench", typeof(BlockEntityWorkbench));
            api.RegisterBlockClass($"{ModId}:BlockGrindstone", typeof(BlockGrindstone));
            api.RegisterBlockClass($"{ModId}:BlockWorkbench", typeof(BlockWorkbench));
            api.RegisterItemClass($"{ModId}:ItemWhetstone", typeof(ItemWhetstone));
            api.RegisterItemClass($"{ModId}:ItemTinkerToolParts", typeof(ItemTinkerToolParts));
            api.RegisterItemClass($"{ModId}:WorkableBits", typeof(ItemWorkableNugget));

            HarmonyPatch();
        }

        public override void StartServerSide(ICoreServerAPI api) {
            ConfigUtility.MergeAndSetConfigStrings();
            ServerCommands.RegisterServerCommands(api);
        }

        public override void StartClientSide(ICoreClientAPI api) {
            GradientSelection = 0;
            if (ClientConfig.UseGradientForSharpnessInstead) {
                TinkeringUtility.InitializeSharpnessColorGradient();
            }

            string configBase64String = api.World.Config.GetString(ToolsmithConstants.ToolsmithConfigKey, "");
            if (configBase64String != "") {
                try {
                    byte[] configBytes = Convert.FromBase64String(configBase64String);
                    string configJson = System.Text.Encoding.UTF8.GetString(configBytes);
                    Config = JsonConvert.DeserializeObject<ToolsmithConfigs>(configJson);
                    Logger.Notification("Recieved configs successfully from Server!");
                } catch (Exception ex) {
                    Logger.Error("Failed to deserialize config from server: " + ex + "\nThis may cause problems for a proper server-client separation.");
                    Config = new ToolsmithConfigs();
                }
            } else {
                Logger.Error("Failed to retrieve config from server, running with default settings. This may cause problems for a proper server-client separation.");
                Config = new ToolsmithConfigs();
            }

            string statsBase64String = api.World.Config.GetString(ToolsmithConstants.ToolsmithStatsKey, "");
            if (statsBase64String != "") {
                try {
                    byte[] statsBytes = Convert.FromBase64String(statsBase64String);
                    string statsJson = System.Text.Encoding.UTF8.GetString(statsBytes);
                    Stats = JsonConvert.DeserializeObject<ToolsmithPartStats>(statsJson);
                    Logger.Notification("Recieved part stats successfully from Server!");
                } catch (Exception ex) {
                    Logger.Error("Failed to deserialize part stats from server: " + ex + "\nThis may cause problems for a proper server-client separation.");
                    Stats = new ToolsmithPartStats();
                }
            } else {
                Logger.Error("Failed to retrieve part stats from server, running with default settings. This may cause problems for a proper server-client separation.");
                Stats = new ToolsmithPartStats();
            }

            string woodInBindingBase64String = api.World.Config.GetString(ToolsmithConstants.ToolsmithWoodInToolBindingsData, "");
            if (woodInBindingBase64String != "") {
                try {
                    byte[] woodInBindingBytes = Convert.FromBase64String(woodInBindingBase64String);
                    string woodInBindingJson = System.Text.Encoding.UTF8.GetString(woodInBindingBytes);
                    ToolsWithWoodInBindingShapes = JsonConvert.DeserializeObject<List<string>>(woodInBindingJson);
                    Logger.Notification("Recieved the List of tools with wood in their bindings from the server!");
                } catch (Exception ex) {
                    Logger.Error("Failed to deserialize List of tools with Wood in their bindings from the server: " + ex + "\nAttempting clientside fallback method instead.");
                    ClientAttemptBasicWoodInBindingsInit(api);
                }
            } else {
                Logger.Error("Failed to retrieve the List of tools with wood in their bindings from the server. Attempting fallback method.");
                ClientAttemptBasicWoodInBindingsInit(api);
            }
        }

        public override void AssetsFinalize(ICoreAPI api) {
            base.AssetsFinalize(api);

            if (api.Side.IsClient()) {
                //Init these Clientside as well, in the case of Multiplayer
                ToolsmithConstants.ToolsmithHeadItemTag = api.TagRegistry.ItemTagsToTagArray(["toolsmith-head"]);
                ToolsmithConstants.ToolsmithHandleItemTag = api.TagRegistry.ItemTagsToTagArray(["toolsmith-handle"]);
                ToolsmithConstants.ToolsmithBindingItemTag = api.TagRegistry.ItemTagsToTagArray(["toolsmith-binding"]);
                ToolsmithConstants.ToolsmithPartItemTag = api.TagRegistry.ItemTagsToTagArray(["toolsmith-part"]);
                ToolsmithConstants.ToolsmithMaintenanceItemTag = api.TagRegistry.ItemTagsToTagArray(["toolsmith-maintenance"]);

                ToolsmithConstants.ToolsmithBindingBlockTag = api.TagRegistry.BlockTagsToTagArray(["toolsmith-binding"]);
                ToolsmithConstants.ToolsmithPartBlockTag = api.TagRegistry.BlockTagsToTagArray(["toolsmith-part"]);

                if (BindingTiers == null) {
                    BindingTiers = new Dictionary<string, int>();
                }
                CalculateBindingTiers();
                CacheAlternateWorkbenchSlots(api as ICoreClientAPI);

                return;
            }

            ProcessJsonPartsAndStats(api);

            var handleDict = Stats.BaseHandleParts;
            var bindingDict = Stats.BindingParts;
            var gripDict = Stats.GripParts;
            var treatmentDict = Stats.TreatmentParts;
            RecipeRegisterModSystem.HandleList = new List<CollectibleObject>();
            RecipeRegisterModSystem.BindingList = new List<CollectibleObject>();
            RecipeRegisterModSystem.GripList = new List<CollectibleObject>();
            RecipeRegisterModSystem.TreatmentList = new List<CollectibleObject>();
            RecipeRegisterModSystem.TinkerableToolsList = new List<CollectibleObject>();
            RecipeRegisterModSystem.LiquidContainers = new List<CollectibleObject>();

            List<CollectibleObject> SinglePartToolsList = null;
            if (Config.PrintAllParsedToolsAndParts) {
                SinglePartToolsList = new List<CollectibleObject>();
            }

            //Prep Item and Block Tags, registering them all here once.
            string[] itemTags = ["toolsmith-part", "toolsmith-maintenance", "toolsmith-binding", "toolsmith-handle", "toolsmith-head"];
            string[] blockTags = ["toolsmith-part", "toolsmith-binding"];
            api.TagRegistry.RegisterItemTags(itemTags);
            api.TagRegistry.RegisterBlockTags(blockTags);

            var bindingItemTag = api.TagRegistry.ItemTagsToTagArray(["toolsmith-part", "toolsmith-binding"]);
            var bindingBlockTag = api.TagRegistry.BlockTagsToTagArray(["toolsmith-part", "toolsmith-binding"]);
            var handleItemTag = api.TagRegistry.ItemTagsToTagArray(["toolsmith-part", "toolsmith-handle"]);

            ToolsmithConstants.ToolsmithHeadItemTag = api.TagRegistry.ItemTagsToTagArray(["toolsmith-head"]);
            ToolsmithConstants.ToolsmithHandleItemTag = api.TagRegistry.ItemTagsToTagArray(["toolsmith-handle"]);
            ToolsmithConstants.ToolsmithBindingItemTag = api.TagRegistry.ItemTagsToTagArray(["toolsmith-binding"]);
            ToolsmithConstants.ToolsmithPartItemTag = api.TagRegistry.ItemTagsToTagArray(["toolsmith-part"]);
            ToolsmithConstants.ToolsmithMaintenanceItemTag = api.TagRegistry.ItemTagsToTagArray(["toolsmith-maintenance"]);

            ToolsmithConstants.ToolsmithBindingBlockTag = api.TagRegistry.BlockTagsToTagArray(["toolsmith-binding"]);
            ToolsmithConstants.ToolsmithPartBlockTag = api.TagRegistry.BlockTagsToTagArray(["toolsmith-part"]);

            foreach (var t in api.World.Collectibles.Where(t => t?.Code != null)) { //A tool/part should likely be only one of these!
                if (ConfigUtility.IsTinkerableTool(t.Code.ToString()) && !(ConfigUtility.IsToolHead(t.Code.ToString())) && !(ConfigUtility.IsOnBlacklist(t.Code.ToString()))) { //Any tool that you actually craft from a Tool Head to create!
                    if (Config.DebugMessages) {
                        Logger.Debug("Attempting to register " + t.Code.ToString() + " as a Tinkerable Tool.");
                    }
                    if (t.ItemClass == EnumItemClass.Item && !t.HasBehavior<ModularPartRenderingFromAttributes>()) {
                        t.AddBehavior<ModularPartRenderingFromAttributes>();
                    }
                    if (!t.HasBehavior<CollectibleBehaviorTinkeredTools>()) {
                        t.AddBehavior<CollectibleBehaviorTinkeredTools>();
                    }
                    if (ConfigUtility.IsBluntTool(t.Code.ToString())) { //Any tinkered tool can still be one that's 'blunt', IE a Hammer in this case
                        if (!t.HasBehavior<CollectibleBehaviorToolBlunt>()) {
                            t.AddBehavior<CollectibleBehaviorToolBlunt>();
                        }
                    }
                    if (ConfigUtility.IsToolWithWoodInBindingShapes(t.Code.ToString())) {
                        if (!ToolsWithWoodInBindingShapes.Contains(t.FirstCodePart())) {
                            ToolsWithWoodInBindingShapes.Add(t.FirstCodePart());
                        }
                    }

                    RecipeRegisterModSystem.TinkerableToolsList.Add(t);
                    continue;
                } else if (ConfigUtility.IsSinglePartTool(t.Code.ToString()) && !(ConfigUtility.IsOnBlacklist(t.Code.ToString()))) { //A 'Smithed' tool is one that once you finish the anvil smithing recipe, the tool is done. Shears, Wrench, or Chisel in vanilla! Add the 'Smithed' Tool Behavior so they can gain the Grinding interaction to maintain them.
                    if (Config.DebugMessages) {
                        Logger.Debug("Attempting to register " + t.Code.ToString() + " as a Smithed Tool.");
                    }
                    if (!t.HasBehavior<CollectibleBehaviorSmithedTools>()) {
                        t.AddBehavior<CollectibleBehaviorSmithedTools>();
                    }
                    if (ConfigUtility.IsBluntTool(t.Code.ToString())) { //Any smithed tool can still be one that's 'blunt', IE a Wrench in this case
                        if (!t.HasBehavior<CollectibleBehaviorToolBlunt>()) {
                            t.AddBehavior<CollectibleBehaviorToolBlunt>();
                        }
                    }
                    if (SinglePartToolsList != null) {
                        SinglePartToolsList.Add(t);
                    }
                    continue;
                } else if (ConfigUtility.IsToolHandle(t.Code.Path, handleDict)) { //Probably don't need the blacklist anymore, since can assume the configs have the exact Path
                    if (Config.DebugMessages) {
                        Logger.Debug("Attempting to register " + t.Code.ToString() + " as a Tool Handle.");
                    }
                    if (t.ItemClass == EnumItemClass.Item && !t.HasBehavior<ModularPartRenderingFromAttributes>()) {
                        t.AddBehavior<ModularPartRenderingFromAttributes>();
                    }
                    if (!t.HasBehavior<CollectibleBehaviorToolHandle>()) {
                        t.AddBehavior<CollectibleBehaviorToolHandle>();
                    }
                    if (!t.StorageFlags.HasFlag(EnumItemStorageFlags.Offhand)) {
                        t.StorageFlags += 0x100;
                    }
                    if (t.ItemClass == EnumItemClass.Item) {
                        var item = ((Item)t);
                        if (!handleItemTag.isPresentIn(ref item.Tags)) {
                            var tagArray = item.Tags.ToArray(api).ToArray();
                            tagArray = tagArray.AddRangeToArray(["toolsmith-part", "toolsmith-handle"]);
                            item.Tags = api.TagRegistry.ItemTagsToTagArray(tagArray);
                        }
                    }
                    RecipeRegisterModSystem.HandleList.Add(t);
                } else if (ConfigUtility.IsToolBinding(t.Code.Path, bindingDict)) {
                    if (Config.DebugMessages) {
                        Logger.Debug("Attempting to register " + t.Code.ToString() + " as a Tool Binding.");
                    }
                    if (!t.HasBehavior<CollectibleBehaviorToolBinding>()) {
                        t.AddBehavior<CollectibleBehaviorToolBinding>();
                    }
                    if (!t.StorageFlags.HasFlag(EnumItemStorageFlags.Offhand)) {
                        t.StorageFlags += 0x100;
                    }
                    if (t.ItemClass == EnumItemClass.Item) {
                        var item = ((Item)t);
                        if (!bindingItemTag.isPresentIn(ref item.Tags)) {
                            var tagArray = item.Tags.ToArray(api).ToArray();
                            tagArray = tagArray.AddRangeToArray(["toolsmith-part", "toolsmith-binding"]);
                            item.Tags = api.TagRegistry.ItemTagsToTagArray(tagArray);
                        }
                    } else {
                        var block = ((Block)t);
                        if (!bindingBlockTag.isPresentIn(ref block.Tags)) {
                            var tagArray = block.Tags.ToArray(api).ToArray();
                            tagArray = tagArray.AddRangeToArray(["toolsmith-part", "toolsmith-binding"]);
                            block.Tags = api.TagRegistry.BlockTagsToTagArray(tagArray);
                        }
                    }
                    RecipeRegisterModSystem.BindingList.Add(t);
                }

                if (ConfigUtility.IsValidGripMaterial(t.Code.Path, gripDict)) {
                    RecipeRegisterModSystem.GripList.Add(t);
                }
                if (ConfigUtility.IsValidTreatmentMaterial(t.Code.Path, treatmentDict)) {
                    RecipeRegisterModSystem.TreatmentList.Add(t);
                }
                if (t.Attributes?.KeyExists("liquidContainerProps") == true) {
                    if (!t.StorageFlags.HasFlag(EnumItemStorageFlags.Offhand)) {
                        t.StorageFlags += 0x100;
                    }
                    RecipeRegisterModSystem.LiquidContainers.Add(t);
                }
            }

            SaveWoodInToolBindingToWorld(api);

            if (Config.PrintAllParsedToolsAndParts) { //Mainly left in for debugging purposes since it's kinda useful to just let it run through everything and see what might be going wrong and where... Especially when adding other mods
                Logger.Debug("Single Part Tools:");
                foreach (var t in SinglePartToolsList) {
                    Logger.Debug(t.Code.ToString());
                }
                Logger.Debug("Tinkerable Tools:");
                foreach (var t in RecipeRegisterModSystem.TinkerableToolsList) {
                    Logger.Debug(t.Code.ToString());
                }
                Logger.Debug("Tool Handles:");
                foreach (var t in RecipeRegisterModSystem.HandleList) {
                    Logger.Debug(t.Code.ToString());
                }
                Logger.Debug("Tool Bindings:");
                foreach (var t in RecipeRegisterModSystem.BindingList) {
                    Logger.Debug(t.Code.ToString());
                }
                Logger.Debug("Grip Materials:");
                foreach (var t in RecipeRegisterModSystem.GripList) {
                    Logger.Debug(t.Code.ToString());
                }
                Logger.Debug("Treatment Materials:");
                foreach (var t in RecipeRegisterModSystem.TreatmentList) {
                    Logger.Debug(t.Code.ToString());
                }
                Logger.Debug("Registered Liquid Containers:");
                foreach (var t in RecipeRegisterModSystem.LiquidContainers) {
                    Logger.Debug(t.Code.ToString());
                }
            }
        }

        private void ProcessJsonPartsAndStats(ICoreAPI api) {
            if (Config.EnableEditsForRegex) {
                Logger.Debug("Server is starting with Config Edits for the Regex Strings enabled. If something goes wrong, the changes made could be the cause. Disable the option to reset configs to the generated default, or if you want to update the defaults from the JSON files again - it will not update from any compatability files while this is active. If you report an issue with this enabled, please include your Toolsmith.json config changes as well as the logs!");
            } else {
                Dictionary<AssetLocation, List<string>> toolHeads = api.Assets.GetMany<List<string>>(api.Logger, "config/toolsmith/regex/toolheads");
                Config.ToolHeads += "@.*(";
                foreach (var toolHead in toolHeads) {
                    ToolsmithConfigsHelpers.AddToRegexString(toolHead.Value, ref Config.ToolHeads);
                }
                Config.ToolHeads = Config.ToolHeads.Remove(Config.ToolHeads.Length - 1); //Trim away the very last | that gets added on at the end.
                Config.ToolHeads += ").*";

                Dictionary<AssetLocation, List<string>> tinkerableTools = api.Assets.GetMany<List<string>>(api.Logger, "config/toolsmith/regex/tinkerabletools");
                Config.TinkerableTools += "@.*:(";
                foreach (var tinkerableTool in tinkerableTools) {
                    ToolsmithConfigsHelpers.AddToRegexString(tinkerableTool.Value, ref Config.TinkerableTools);
                }
                Config.TinkerableTools = Config.TinkerableTools.Remove(Config.TinkerableTools.Length - 1);
                Config.TinkerableTools += ").*";

                Dictionary<AssetLocation, List<string>> singlePartTools = api.Assets.GetMany<List<string>>(api.Logger, "config/toolsmith/regex/singleparttools");
                Config.SinglePartTools += "@.*:(";
                foreach (var singlePartTool in singlePartTools) {
                    ToolsmithConfigsHelpers.AddToRegexString(singlePartTool.Value, ref Config.SinglePartTools);
                }
                Config.SinglePartTools = Config.SinglePartTools.Remove(Config.SinglePartTools.Length - 1);
                Config.SinglePartTools += ").*";

                Dictionary<AssetLocation, List<string>> bluntHeadedTools = api.Assets.GetMany<List<string>>(api.Logger, "config/toolsmith/regex/bluntheadedtools");
                Config.BluntHeadedTools += "@.*:(";
                foreach (var bluntHeadedTool in bluntHeadedTools) {
                    ToolsmithConfigsHelpers.AddToRegexString(bluntHeadedTool.Value, ref Config.BluntHeadedTools);
                }
                Config.BluntHeadedTools = Config.BluntHeadedTools.Remove(Config.BluntHeadedTools.Length - 1);
                Config.BluntHeadedTools += ").*";

                Dictionary<AssetLocation, List<string>> partBlacklists = api.Assets.GetMany<List<string>>(api.Logger, "config/toolsmith/regex/partblacklist");
                Config.PartBlacklist += "@.*(";
                foreach (var partBlacklist in partBlacklists) {
                    ToolsmithConfigsHelpers.AddToRegexString(partBlacklist.Value, ref Config.PartBlacklist);
                }
                Config.PartBlacklist = Config.PartBlacklist.Remove(Config.PartBlacklist.Length - 1);
                Config.PartBlacklist += ").*";

                Dictionary<AssetLocation, List<string>> toolsWithWoodInBindingShapes = api.Assets.GetMany<List<string>>(api.Logger, "config/toolsmith/regex/woodinbindingshapes");
                Config.ToolsWithWoodInBindingShape += "@.*(";
                foreach (var tool in toolsWithWoodInBindingShapes) {
                    ToolsmithConfigsHelpers.AddToRegexString(tool.Value, ref Config.ToolsWithWoodInBindingShape);
                }
                Config.ToolsWithWoodInBindingShape = Config.ToolsWithWoodInBindingShape.Remove(Config.ToolsWithWoodInBindingShape.Length - 1);
                Config.ToolsWithWoodInBindingShape += ").*";
            }

            if (Stats.EnableEdits) {
                Logger.Debug("Server is starting with Config Edits for the Parts and Stats enabled. If something goes wrong, the changes made could be the cause. Disable the option to reset configs to the generated default. If you report an issue with this enabled, please include your ToolsmithPartsStats.json config changes as well as the logs!");
            }

            if (Config.RunFullJsonVerifying) {
                Logger.Debug("Running full Json verification for all found Toolsmith Configs for parts and stats.");
            }

            Dictionary<AssetLocation, List<HandlePartDefines>> handleParts = api.Assets.GetMany<List<HandlePartDefines>>(api.Logger, "config/toolsmith/parts/handles");
            foreach (var handlePart in handleParts) {
                ToolsmithPartStatsHelpers.VerifyAndStoreDefinesInDict(handlePart.Value, Config.RunFullJsonVerifying, ref Stats.BaseHandleParts);
            }

            Dictionary<AssetLocation, List<GripPartDefines>> gripParts = api.Assets.GetMany<List<GripPartDefines>>(api.Logger, "config/toolsmith/parts/grips");
            foreach (var gripPart in gripParts) {
                ToolsmithPartStatsHelpers.VerifyAndStoreDefinesInDict(gripPart.Value, Config.RunFullJsonVerifying, ref Stats.GripParts);
            }

            Dictionary<AssetLocation, List<TreatmentPartDefines>> treatmentParts = api.Assets.GetMany<List<TreatmentPartDefines>>(api.Logger, "config/toolsmith/parts/treatments");
            foreach (var treatmentPart in treatmentParts) {
                ToolsmithPartStatsHelpers.VerifyAndStoreDefinesInDict(treatmentPart.Value, Config.RunFullJsonVerifying, ref Stats.TreatmentParts);
            }

            Dictionary<AssetLocation, List<BindingPartDefines>> bindingParts = api.Assets.GetMany<List<BindingPartDefines>>(api.Logger, "config/toolsmith/parts/bindings");
            foreach (var bindingPart in bindingParts) {
                ToolsmithPartStatsHelpers.VerifyAndStoreDefinesInDict(bindingPart.Value, Config.RunFullJsonVerifying, ref Stats.BindingParts);
            }

            Dictionary<AssetLocation, List<HandleStatDefines>> handleStats = api.Assets.GetMany<List<HandleStatDefines>>(api.Logger, "config/toolsmith/stats/handles");
            foreach (var handleStat in handleStats) {
                ToolsmithPartStatsHelpers.VerifyAndStoreDefinesInDict(handleStat.Value, Config.RunFullJsonVerifying, ref Stats.BaseHandleStats);
            }

            Dictionary<AssetLocation, List<GripStatDefines>> gripStats = api.Assets.GetMany<List<GripStatDefines>>(api.Logger, "config/toolsmith/stats/grips");
            foreach (var gripStat in gripStats) {
                ToolsmithPartStatsHelpers.VerifyAndStoreDefinesInDict(gripStat.Value, Config.RunFullJsonVerifying, ref Stats.GripStats);
            }

            Dictionary<AssetLocation, List<TreatmentStatDefines>> treatmentStats = api.Assets.GetMany<List<TreatmentStatDefines>>(api.Logger, "config/toolsmith/stats/treatments");
            foreach (var treatmentStat in treatmentStats) {
                ToolsmithPartStatsHelpers.VerifyAndStoreDefinesInDict(treatmentStat.Value, Config.RunFullJsonVerifying, ref Stats.TreatmentStats);
            }

            Dictionary<AssetLocation, List<BindingStatDefines>> bindingStats = api.Assets.GetMany<List<BindingStatDefines>>(api.Logger, "config/toolsmith/stats/bindings");
            foreach (var bindingStat in bindingStats) {
                ToolsmithPartStatsHelpers.VerifyAndStoreDefinesInDict(bindingStat.Value, Config.RunFullJsonVerifying, ref Stats.BindingStats);
            }

            if (Config.RunFullJsonVerifying) {
                Logger.Debug("Full Json Verification complete! All found errors will have been printed above.");
            }

            SaveConfigToWorldData(api);
            SaveStatsToWorldData(api);
            SaveConfigAfterJsonAdditions(api);
            SaveStatsAfterJsonAdditions(api);
        }

        private void TryToLoadConfig(ICoreAPI api) { //Created following the tutorial on the Wiki!
            try {
                Config = api.LoadModConfig<ToolsmithConfigs>(ConfigUtility.ConfigFilename);
                if (Config == null) {
                    Config = new ToolsmithConfigs();
                } else {
                    if (Config.AutoUpdateConfigsOnVersionChange) {
                        DoesConfigNeedRegen = (Config.ModVersionNumber != ModVersion);
                        if (DoesConfigNeedRegen) {
                            Config = new ToolsmithConfigs();
                        }
                    }
                    if (!DoesConfigNeedRegen && !Config.EnableEditsForRegex) {
                        ToolsmithConfigsHelpers.ResetRegexStrings(ref Config);
                    }
                }
                Config.ModVersionNumber = ModVersion;
            } catch (Exception e) {
                Mod.Logger.Error("Could not load config, using default settings instead!");
                Mod.Logger.Error(e);
                Config = new ToolsmithConfigs();
            }
        }

        private void TryToLoadClientConfig(ICoreAPI api) {
            try {
                ClientConfig = api.LoadModConfig<ToolsmithClientConfigs>(ConfigUtility.ClientConfigFilename);
                if (ClientConfig == null || DoesConfigNeedRegen) {
                    ClientConfig = new ToolsmithClientConfigs();
                }
                api.StoreModConfig(ClientConfig, ConfigUtility.ClientConfigFilename);
            } catch (Exception e) {
                Mod.Logger.Error("Could not load stats, using default settings instead!");
                Mod.Logger.Error(e);
                ClientConfig = new ToolsmithClientConfigs();
            }
        }

        private void TryToLoadStats(ICoreAPI api) {
            try {
                Stats = api.LoadModConfig<ToolsmithPartStats>(ConfigUtility.StatsFilename);
                if (Stats == null) {
                    Stats = new ToolsmithPartStats();
                } else {
                    if (!Stats.EnableEdits) {
                        Stats = new ToolsmithPartStats();
                    }
                }
            } catch (Exception e) {
                Mod.Logger.Error("Could not load stats, using default settings instead!");
                Mod.Logger.Error(e);
                Stats = new ToolsmithPartStats();
            }
        }

        private void SaveConfigToWorldData(ICoreAPI api) {
            string configJson = JsonConvert.SerializeObject(Config);
            byte[] configBytes = System.Text.Encoding.UTF8.GetBytes(configJson);
            string configBase64String = Convert.ToBase64String(configBytes);
            api.World.Config.SetString(ToolsmithConstants.ToolsmithConfigKey, configBase64String);
        }

        private void SaveStatsToWorldData(ICoreAPI api) {
            string statsJson = JsonConvert.SerializeObject(Stats);
            byte[] statsBytes = System.Text.Encoding.UTF8.GetBytes(statsJson);
            string statsBase64String = Convert.ToBase64String(statsBytes);
            api.World.Config.SetString(ToolsmithConstants.ToolsmithStatsKey, statsBase64String);
        }

        private void SaveWoodInToolBindingToWorld(ICoreAPI api) {
            string woodInBindingJson = JsonConvert.SerializeObject(ToolsWithWoodInBindingShapes);
            byte[] woodInBindingBytes = System.Text.Encoding.UTF8.GetBytes(woodInBindingJson);
            string woodInBindingBase64String = Convert.ToBase64String(woodInBindingBytes);
            api.World.Config.SetString(ToolsmithConstants.ToolsmithWoodInToolBindingsData, woodInBindingBase64String);
        }

        private void SaveConfigAfterJsonAdditions(ICoreAPI api) {
            try {
                api.StoreModConfig(Config, ConfigUtility.ConfigFilename);
            } catch (Exception e) {
                Mod.Logger.Error("Could not save config after processing the Json additions.");
                Mod.Logger.Error(e);
            }
        }

        private void SaveStatsAfterJsonAdditions(ICoreAPI api) {
            try {
                api.StoreModConfig(Stats, ConfigUtility.StatsFilename);
            } catch (Exception e) {
                Mod.Logger.Error("Could not save stats after processing the Json additions.");
                Mod.Logger.Error(e);
            }
        }

        private void HandleSmithingPlusStartCompat(ICoreAPI api) {
            SmithingPlus.Core SPCore = api.ModLoader.GetModSystem<SmithingPlus.Core>();
            if (SPCore != null) {
                if (!SmithingPlus.Core.Config.GetToolRepairForgettableAttributes.Contains<string>("tinkeredToolHead")) {
                    SmithingPlus.Core.Config.ToolRepairForgettableAttributes = SmithingPlus.Core.Config.ToolRepairForgettableAttributes + ToolsmithAttributes.ToolsmithForgettableAttributes;
                    Logger.VerboseDebug("Added Toolsmith Attributes to Smithing Plus's Forgettable Attributes config!");
                } else {
                    Logger.VerboseDebug("Found possible presence of existing configs already in Smithing Plus for Toolsmith, forgoing the addition! If you have issues, please reset the ToolRepairForgettableAttributes line in the Smithing Plus config.");
                }
            } else {
                Logger.Error("Found Smithing Plus loaded, but could not retrieve the Core ModLoader for it! Auto compatability will not work.");
            }
        }

        //A bit of a fallback method so it at least gets populated with SOMETHING. While it originally is making a Regex method, it's easiest to just put the tool's code in here. This will likely let it work for most cases even if it has to use this method?
        private void ClientAttemptBasicWoodInBindingsInit(ICoreAPI api) {
            Dictionary<AssetLocation, List<string>> toolsWithWoodInBindingShapes = api.Assets.GetMany<List<string>>(api.Logger, "config/toolsmith/regex/woodinbindingshapes");
            foreach ((AssetLocation loc, List<string> tools) in toolsWithWoodInBindingShapes) {
                ToolsWithWoodInBindingShapes.AddRange(tools);
            }
        }

        private void CalculateBindingTiers() {
            foreach (var binding in Stats.BindingParts) {
                var bindingStats = Stats.BindingStats.Get(binding.Value.bindingStatTag);
                if (bindingStats != null) {
                    var bindingTotalFactor = bindingStats.baseHPfactor * (1 + bindingStats.selfHPBonus);
                    switch(bindingTotalFactor) {
                        case <= 1.0f:
                            if (Config.DebugMessages) {
                                Logger.Warning("Binding: " + binding.Key + " |Tier: " + 0);
                            }
                            if (!BindingTiers.ContainsKey(binding.Key)) {
                                BindingTiers.Add(binding.Key, 0);
                            }
                            break;
                        case < 1.75f:
                            if (Config.DebugMessages) {
                                Logger.Warning("Binding: " + binding.Key + " |Tier: " + 1);
                            }
                            if (!BindingTiers.ContainsKey(binding.Key)) {
                                BindingTiers.Add(binding.Key, 1);
                            }
                            break;
                        case < 2.5f:
                            if (Config.DebugMessages) {
                                Logger.Warning("Binding: " + binding.Key + " |Tier: " + 2);
                            }
                            if (!BindingTiers.ContainsKey(binding.Key)) {
                                BindingTiers.Add(binding.Key, 2);
                            }
                            break;
                        case < 3f:
                            if (Config.DebugMessages) {
                                Logger.Warning("Binding: " + binding.Key + " |Tier: " + 3);
                            }
                            if (!BindingTiers.ContainsKey(binding.Key)) {
                                BindingTiers.Add(binding.Key, 3);
                            }
                            break;
                        case >= 3f:
                            if (Config.DebugMessages) {
                                Logger.Warning("Binding: " + binding.Key + " |Tier: " + 4);
                            }
                            if (!BindingTiers.ContainsKey(binding.Key)) {
                                BindingTiers.Add(binding.Key, 4);
                            }
                            break;
                    }
                }
            }
        }

        private void CacheAlternateWorkbenchSlots(ICoreClientAPI capi) {
            Dictionary<string, MeshData> slotMarkerTextures = ObjectCacheUtil.GetOrCreate(capi, ToolsmithConstants.WorkbenchSlotShapesCache, () => {
                Shape emptySlot = capi.Assets.TryGet(new AssetLocation(ToolsmithConstants.WorkbenchSlotMarkerShapePath + ".json"))?.ToObject<Shape>();
                ShapeTextureSource markerTexSource = new(capi, emptySlot, "For rendering a slot marker for any Workbench Slot");
                
                markerTexSource.textures.Clear();
                markerTexSource.textures["slot"] = new CompositeTexture(new AssetLocation(ToolsmithConstants.WorkbenchSlotMarkerEmptyPath + ".png"));
                capi.Tesselator.TesselateShape("EmptySlot for Workbench Crafting Slot", emptySlot, out MeshData emptyMarkerData, markerTexSource);

                markerTexSource.textures.Clear();
                markerTexSource.textures["slot"] = new CompositeTexture(new AssetLocation(ToolsmithConstants.WorkbenchSlotMarkerHeadPath + ".png"));
                capi.Tesselator.TesselateShape("HeadSlot for Workbench Crafting Slot", emptySlot, out MeshData headMarkerData, markerTexSource);

                markerTexSource.textures.Clear();
                markerTexSource.textures["slot"] = new CompositeTexture(new AssetLocation(ToolsmithConstants.WorkbenchSlotMarkerHandlePath + ".png"));
                capi.Tesselator.TesselateShape("HandleSlot for Workbench Crafting Slot", emptySlot, out MeshData handleMarkerData, markerTexSource);

                markerTexSource.textures.Clear();
                markerTexSource.textures["slot"] = new CompositeTexture(new AssetLocation(ToolsmithConstants.WorkbenchSlotMarkerBindingPath + ".png"));
                capi.Tesselator.TesselateShape("BindingSlot for Workbench Crafting Slot", emptySlot, out MeshData bindingMarkerData, markerTexSource);
                
                return new Dictionary<string, MeshData>() {
                    [ToolsmithConstants.WorkbenchSlotMarkerEmptyPath] = emptyMarkerData,
                    [ToolsmithConstants.WorkbenchSlotMarkerHeadPath] = headMarkerData,
                    [ToolsmithConstants.WorkbenchSlotMarkerHandlePath] = handleMarkerData,
                    [ToolsmithConstants.WorkbenchSlotMarkerBindingPath] = bindingMarkerData
                };
            });
        }

        private static void HarmonyPatch() {
            if (HarmonyInstance != null) {
                return;
            }
            HarmonyInstance = new Harmony(ModId);
            Logger.VerboseDebug("Harmony is starting Patches!");
            HarmonyInstance.PatchCategory(ToolTinkeringDamagePatchCategory);
            HarmonyInstance.PatchCategory(ToolTinkeringToolUseStatsPatchCategory);
            HarmonyInstance.PatchCategory(ToolTinkeringTransitionalPropsPatchCategory);
            HarmonyInstance.PatchCategory(ToolTinkeringCraftingPatchCategory);
            HarmonyInstance.PatchCategory(ToolTinkeringRenderPatchCategory);
            HarmonyInstance.PatchCategory(ToolTinkeringGuiElementPatchCategory);
            HarmonyInstance.PatchCategory(ToolTinkeringItemAxePatchCategory);
            Logger.VerboseDebug("Patched functions for Tool Tinkering purposes.");
            HarmonyInstance.PatchCategory(OffhandDominantInteractionUsePatchCategory);
            Logger.VerboseDebug("Patched functions for Offhand Dominant Interaction purposes.");
            HarmonyInstance.PatchCategory(ToolRenderingPatchCategory);
            Logger.VerboseDebug("Patched functions for Tool Multi-Part Rendering purposes.");
        }

        private static void HarmonyUnpatch() {
            Logger?.VerboseDebug("Unpatching Harmony Patches.");
            HarmonyInstance?.UnpatchAll(ModId);
            HarmonyInstance = null;
        }

        public override void Dispose() {
            HarmonyUnpatch();
            Logger = null;
            ModId = null;
            Sapi = null;
            Capi = null;
            Config = null;
            ClientConfig = null;
            Stats = null;
            IgnoreCodes = null;
            ToolsWithWoodInBindingShapes = null;
            base.Dispose();
        }
    }
}
