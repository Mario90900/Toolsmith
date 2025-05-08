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
        public static ICoreAPI Api;
        public static Harmony HarmonyInstance;
        public static ToolsmithConfigs Config;
        public static ToolsmithPartStats Stats;
        public static int GradientSelection = 0;
        public static bool DoesConfigNeedRegen = false;

        public const string ToolTinkeringDamagePatchCategory = "toolTinkeringDamage";
        public const string ToolTinkeringToolUseStatsPatchCategory = "toolTinkeringToolUseStats";
        public const string ToolTinkeringTransitionalPropsPatchCategory = "toolTinkeringTransitionalProps";
        public const string ToolTinkeringCraftingPatchCategory = "toolTinkeringCrafting";
        public const string ToolTinkeringRenderPatchCategory = "toolTinkeringRender";
        public const string ToolTinkeringGuiElementPatchCategory = "toolTinkeringGuiElement";

        public const string OffhandDominantInteractionUsePatchCategory = "offhandDominantInteractionUse";

        public static List<string> IgnoreCodes;
        public static Dictionary<string, int> BindingTiers; //This is only initialized on the Client side! Used for just generating and storing the various bindings tier levels to display on their tooltips.

        public override void StartPre(ICoreAPI api) {
            Logger = Mod.Logger;
            ModId = Mod.Info.ModID;
            ModVersion = Mod.Info.Version;
            Api = api;
            TryToLoadConfig(api);
            TryToLoadStats(api);
            IgnoreCodes = new List<string>();
        }

        public override void Start(ICoreAPI api) {
            if (api.ModLoader.IsModEnabled("smithingplus")) {
                Logger.VerboseDebug("Smithing Plus found, trying to patch in attributes to the forgettable config!");
                HandleSmithingPlusStartCompat(api);
            }

            //This is important to let the Treasure Hunter Trader accept a Toolsmith Pick to get the map for Story Content! Thank you Item Rarity for also having the issue and both leaving a comment in their code and pushing the commit not too long before I had the same problem :P
            GlobalConstants.IgnoredStackAttributes = GlobalConstants.IgnoredStackAttributes.Append(ToolsmithAttributes.ToolsmithIgnoreAttributesArray);

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

            HarmonyPatch();
        }

        public override void StartServerSide(ICoreServerAPI api) {
            ServerCommands.RegisterServerCommands(api);
            string configJson = JsonConvert.SerializeObject(Config);
            byte[] configBytes = System.Text.Encoding.UTF8.GetBytes(configJson);
            string configBase64String = Convert.ToBase64String(configBytes);
            api.World.Config.SetString(ToolsmithConstants.ToolsmithConfigKey, configBase64String);
        }

        public override void StartClientSide(ICoreClientAPI api) {
            GradientSelection = 0;
            TinkeringUtility.InitializeSharpnessColorGradient();
            string configBase64String = api.World.Config.GetString(ToolsmithConstants.ToolsmithConfigKey, "");
            if (configBase64String != "") {
                try {
                    byte[] configBytes = Convert.FromBase64String(configBase64String);
                    string configJson = System.Text.Encoding.UTF8.GetString(configBytes);
                    Config = JsonConvert.DeserializeObject<ToolsmithConfigs>(configJson);
                } catch (Exception ex) {
                    Logger.Error("Failed to deserialize config from server: " + ex);
                    Config = new ToolsmithConfigs();
                }
            } else {
                Logger.Error("Failed to retrieve config from server, running with default settings.");
                Config = new ToolsmithConfigs();
            }
        }

        public override void AssetsFinalize(ICoreAPI api) {
            base.AssetsFinalize(api);

            if (api.Side.IsClient()) {
                if (BindingTiers == null) {
                    BindingTiers = new Dictionary<string, int>();
                }
                CalculateBindingTiers();
                return;
            }
            
            if (Config.PrintAllParsedToolsAndParts) {
                Logger.Debug("Single Part Tools:");
            }
            var handleKeys = Config.BaseHandleRegistry.Keys;
            var bindingKeys = Config.BindingRegistry.Keys;
            var gripKeys = Config.GripRegistry.Keys;
            var treatmentKeys = Config.TreatmentRegistry.Keys;
            RecipeRegisterModSystem.HandleList = new List<CollectibleObject>();
            RecipeRegisterModSystem.BindingList = new List<CollectibleObject>();
            RecipeRegisterModSystem.GripList = new List<CollectibleObject>();
            RecipeRegisterModSystem.TreatmentList = new List<CollectibleObject>();
            RecipeRegisterModSystem.TinkerableToolsList = new List<CollectibleObject>();
            foreach (var t in api.World.Collectibles.Where(t => t?.Code != null)) { //A tool/part should likely be only one of these!
                if (ConfigUtility.IsTinkerableTool(t.Code.ToString()) && !(ConfigUtility.IsToolHead(t.Code.ToString())) && !(ConfigUtility.IsOnBlacklist(t.Code.ToString()))) { //Any tool that you actually craft from a Tool Head to create!
                    if (!t.HasBehavior<CollectibleBehaviorTinkeredTools>()) {
                        t.AddBehavior<CollectibleBehaviorTinkeredTools>();
                    }
                    if (ConfigUtility.IsBluntTool(t.Code.ToString())) { //Any tinkered tool can still be one that's 'blunt', IE a Hammer in this case
                        if (!t.HasBehavior<CollectibleBehaviorToolBlunt>()) {
                            t.AddBehavior<CollectibleBehaviorToolBlunt>();
                        }
                    }
                    RecipeRegisterModSystem.TinkerableToolsList.Add(t);
                    continue;
                } else if (ConfigUtility.IsSinglePartTool(t.Code.ToString()) && !(ConfigUtility.IsOnBlacklist(t.Code.ToString()))) { //A 'Smithed' tool is one that once you finish the anvil smithing recipe, the tool is done. Shears, Wrench, or Chisel in vanilla! Add the 'Smithed' Tool Behavior so they can gain the Grinding interaction to maintain them.
                    if (!t.HasBehavior<CollectibleBehaviorSmithedTools>()) {
                        t.AddBehavior<CollectibleBehaviorSmithedTools>();
                    }
                    if (ConfigUtility.IsBluntTool(t.Code.ToString())) { //Any smithed tool can still be one that's 'blunt', IE a Wrench in this case
                        if (!t.HasBehavior<CollectibleBehaviorToolBlunt>()) {
                            t.AddBehavior<CollectibleBehaviorToolBlunt>();
                        }
                    }
                    if (Config.PrintAllParsedToolsAndParts) {
                        Logger.Debug(t.Code.ToString());
                    }
                    continue;
                } else if (ConfigUtility.IsToolHandle(t.Code.Path, handleKeys)) { //Probably don't need the blacklist anymore, since can assume the configs have the exact Path
                    if (!t.HasBehavior<ModularPartRenderingFromAttributes>()) {
                        t.AddBehavior<ModularPartRenderingFromAttributes>();
                    }
                    if (!t.HasBehavior<CollectibleBehaviorToolHandle>()) {
                        t.AddBehavior<CollectibleBehaviorToolHandle>();
                    }
                    if (!t.StorageFlags.HasFlag(EnumItemStorageFlags.Offhand)) {
                        t.StorageFlags += 0x100;
                    }
                    RecipeRegisterModSystem.HandleList.Add(t);
                } else if (ConfigUtility.IsToolBinding(t.Code.Path, bindingKeys)) {
                    if (!t.HasBehavior<CollectibleBehaviorToolBinding>()) {
                        t.AddBehavior<CollectibleBehaviorToolBinding>();
                    }
                    if (!t.StorageFlags.HasFlag(EnumItemStorageFlags.Offhand)) {
                        t.StorageFlags += 0x100;
                    }
                    RecipeRegisterModSystem.BindingList.Add(t);
                }

                if (ConfigUtility.IsValidGripMaterial(t.Code.Path, gripKeys)) {
                    RecipeRegisterModSystem.GripList.Add(t);
                }
                if (ConfigUtility.IsValidTreatmentMaterial(t.Code.Path, treatmentKeys)) {
                    RecipeRegisterModSystem.TreatmentList.Add(t);
                }
            }

            if (Config.PrintAllParsedToolsAndParts) { //Mainly left in for debugging purposes since it's kinda useful to just let it run through everything and see what might be going wrong and where... Especially when adding other mods
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
            }
        }

        private void TryToLoadConfig(ICoreAPI api) { //Created following the tutorial on the Wiki!
            try {
                Config = api.LoadModConfig<ToolsmithConfigs>(ConfigUtility.ConfigFilename);
                DoesConfigNeedRegen = Config.ModVersionNumber != ModVersion;
                if (Config == null || DoesConfigNeedRegen) {
                    Config = new ToolsmithConfigs();
                }
                api.StoreModConfig<ToolsmithConfigs>(Config, ConfigUtility.ConfigFilename);
            } catch (Exception e) {
                Mod.Logger.Error("Could not load config, using default settings instead!");
                Mod.Logger.Error(e);
                Config = new ToolsmithConfigs();
            }
        }

        private void TryToLoadStats(ICoreAPI api) {
            try {
                Stats = api.LoadModConfig<ToolsmithPartStats>(ConfigUtility.StatsFilename);
                if (Stats == null || DoesConfigNeedRegen) {
                    Stats = new ToolsmithPartStats();
                }
                api.StoreModConfig<ToolsmithPartStats>(Stats, ConfigUtility.StatsFilename);
            } catch (Exception e) {
                Mod.Logger.Error("Could not load stats, using default settings instead!");
                Mod.Logger.Error(e);
                Stats = new ToolsmithPartStats();
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

        private void CalculateBindingTiers() {
            foreach (var binding in Config.BindingRegistry) {
                var bindingStats = Stats.bindings.Get(binding.Value.bindingStatTag);
                if (bindingStats != null) {
                    var bindingTotalFactor = bindingStats.baseHPfactor * (1 + bindingStats.selfHPBonus);
                    switch(bindingTotalFactor) {
                        case <= 1.0f:
                            if (Config.DebugMessages) {
                                Logger.Warning("Binding: " + binding.Key + " |Tier: " + 0);
                            }
                            BindingTiers.Add(binding.Key, 0);
                            break;
                        case < 1.75f:
                            if (Config.DebugMessages) {
                                Logger.Warning("Binding: " + binding.Key + " |Tier: " + 1);
                            }
                            BindingTiers.Add(binding.Key, 1);
                            break;
                        case < 2.5f:
                            if (Config.DebugMessages) {
                                Logger.Warning("Binding: " + binding.Key + " |Tier: " + 2);
                            }
                            BindingTiers.Add(binding.Key, 2);
                            break;
                        case < 3f:
                            if (Config.DebugMessages) {
                                Logger.Warning("Binding: " + binding.Key + " |Tier: " + 3);
                            }
                            BindingTiers.Add(binding.Key, 3);
                            break;
                        case >= 3f:
                            if (Config.DebugMessages) {
                                Logger.Warning("Binding: " + binding.Key + " |Tier: " + 4);
                            }
                            BindingTiers.Add(binding.Key, 4);
                            break;
                    }
                }
            }
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
            HarmonyInstance.PatchCategory(ToolTinkeringGuiElementPatchCategory); // <-- This is causing turbo lag. Fix it tomorrow. Likely it's the fact I removed that ret call I'm realizing now. That was stupid, cause now it's probably going through and checking a LOT more then it needs to.
            Logger.VerboseDebug("Patched functions for Tool Tinkering purposes.");
            HarmonyInstance.PatchCategory(OffhandDominantInteractionUsePatchCategory);
            Logger.VerboseDebug("Patched functions for Offhand Dominant Interaction purposes.");
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
            Api = null;
            Config = null;
            Stats = null;
            IgnoreCodes = null;
            base.Dispose();
        }
    }
}
