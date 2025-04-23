using ItemRarity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Config;
using Toolsmith.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Toolsmith.ToolTinkering.Behaviors {
    public class CollectibleBehaviorTinkeredTools : CollectibleBehavior {

        protected bool sharpening = false;
        protected float deltaLastTick = 0;
        protected float lastInterval = 0;

        public CollectibleBehaviorTinkeredTools(CollectibleObject collObj) : base(collObj) {

        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) { //This only seems to get called on the clientside, which makes sense. Whoops, it can't be a catch-all to fix null tools like I thought, but it's still important to display the durabilities before the item's actually used.
            if (inSlot.Itemstack == null || inSlot.Inventory == null || inSlot.Inventory.GetType() == typeof(DummyInventory) || inSlot.Inventory.GetType() == typeof(CreativeInventoryTab) || ToolsmithModSystem.IgnoreCodes.Count > 0 && ToolsmithModSystem.IgnoreCodes.Contains(inSlot.Itemstack.Collectible.Code.ToString())) { //I don't think it's possible for the itemstack to be null at this point, but JUST IN CASE I'll confirm it.
                return; //If this item is in a DummyInventory or CreativeInventoryTab, it's likely not an actual item - but something rendering in a Handbook slot or creative inventory slot I believe. Lets just not mess with those, they won't have data anyway.
            }

            var curHeadDur = inSlot.Itemstack.GetToolheadCurrentDurability();
            var maxHeadDur = inSlot.Itemstack.GetToolheadMaxDurability();
            var curHandleDur = inSlot.Itemstack.GetToolhandleCurrentDurability();
            var maxHandleDur = inSlot.Itemstack.GetToolhandleMaxDurability();
            var curBindingDur = inSlot.Itemstack.GetToolbindingCurrentDurability();
            var maxBindingDur = inSlot.Itemstack.GetToolbindingMaxDurability();
            var curSharp = inSlot.Itemstack.GetToolCurrentSharpness();
            var maxSharp = inSlot.Itemstack.GetToolMaxSharpness();

            //This extra reset parts bit might be redundant now after moving the resets into the Get calls themselves. It also might not ever call because it will always be > 0?
            if (curHeadDur < 0) { //If this is 0 then assume something went wrong and reset things, it's a new item spawned in, or a player added the mod to their save.
                inSlot.Itemstack.ResetNullHead(world); //Moved the client-half of resetting the tool head into this call. Can be safely called on both sides, and handle it over there. Make sure to mark the itemslot as dirty on the client though after using this.
                curHeadDur = inSlot.Itemstack.GetToolheadCurrentDurability();
            }
            if (maxHandleDur < 0 || maxBindingDur < 0) { //Same as above
                inSlot.Itemstack.ResetNullHandleOrBinding(world);
                curHandleDur = inSlot.Itemstack.GetToolhandleCurrentDurability();
                maxHandleDur = inSlot.Itemstack.GetToolhandleMaxDurability();
                curBindingDur = inSlot.Itemstack.GetToolbindingCurrentDurability();
                maxBindingDur = inSlot.Itemstack.GetToolbindingMaxDurability();
            }

            //It would be loads easier to just add what I want to a new one...
            StringBuilder workingDsc = new StringBuilder();
            workingDsc.Append(dsc);
            int startIndex = 0;
            int endIndex = 0;

            StringHelpers.FindTooltipVanillaDurabilityLine(ref startIndex, ref endIndex, workingDsc, world, withDebugInfo); //Moved this code originally from TinkerTools into it's own helper function.

            if (endIndex < workingDsc.Length) {
                workingDsc.Remove(startIndex, endIndex - startIndex + 1); //Remove the durability line
            }

            workingDsc.Insert(startIndex, Lang.Get("toolbindingdurability", curBindingDur, maxBindingDur) + '\n'); //Insert in the part durabilities in the place of it
            workingDsc.Insert(startIndex, Lang.Get("toolhandledurability", curHandleDur, maxHandleDur) + '\n');
            workingDsc.Insert(startIndex, Lang.Get("toolheaddurability", curHeadDur, maxHeadDur) + '\n');
            workingDsc.Insert(startIndex, Lang.Get("toolsharpness", curSharp, maxSharp) + '\n');

            dsc.Clear();
            dsc.Append(workingDsc);
            inSlot.MarkDirty();
        }

        //Now to break down the ingredients used in the craft... This may or may not be VERY interesting when the vanilla crafting recipes call OnCreatedByCrafting...
        //Output Slot contains the completed tool, the input slots will - at minimum - have a Toolhead and Handle (which could simply be a stick), and may or may not have a binding.
        //Should be true for even the Vanilla crafting recipes? Barring any changes to them but... probably can be accounted for with looping through the array.
        //Order of the array cannot be assumed either cause of this.
        public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, ref EnumHandling bhHandling) {
            //First, figure out what actually went into the tool. Investigate the Inputs and look for the individual behaviors. This will find the parts!
            ItemStack headStack = null;
            ItemStack handleStack = null;
            ItemStack bindingStack = null;
            ItemStack foundToolInput = null;

            bhHandling = EnumHandling.Handled;
            foreach (var itemSlot in allInputslots.Where(i => i.Itemstack != null)) { //Is it possible any slot could even be null here...? Like when a grid-craft is done. Better to be safe though?
                if (itemSlot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorToolHead>()) { //If it has this behavior, found the tool head!
                    headStack = itemSlot.Itemstack.Clone();
                    headStack.StackSize = 1;
                } else if (itemSlot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorToolHandle>()) { //If this one, then handle found!
                    handleStack = itemSlot.Itemstack.Clone();
                    handleStack.StackSize = 1;
                } else if (itemSlot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorToolBinding>()) { //And finally the (possible) binding! This isn't garenteed though remember, the others are.
                    bindingStack = itemSlot.Itemstack.Clone();
                    bindingStack.StackSize = 1;
                } else if (itemSlot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorTinkeredTools>()) {
                    foundToolInput = itemSlot.Itemstack.Clone();
                    foundToolInput.StackSize = 1;
                }
            }

            bool isHeadMetal = false;
            if (headStack == null && foundToolInput == null) { //I do hope nothing hits this. At least it'll likely get the candle backup.
                ToolsmithModSystem.Logger.Error("Somehow crafted a Tinker Tool with a recipe that could not find a head, nor tool to copy data from.\nThe tool in question is: " + outputSlot.Itemstack.Collectible.Code.ToString() + "\nAttempting to just reset the Tool Head instead. This might result in fallback data of a Candle being assigned.");
                outputSlot.Itemstack.ResetNullHead(ToolsmithModSystem.Api.World);
                headStack = outputSlot.Itemstack.GetToolhead();
            } else if (headStack == null && foundToolInput != null) { //Probably a safety check here, since I realized some recipes IE the whetstone from Working Classes craft a knife with the stone to produce a knife.
                //Actually found an input that is a tool, so probably copy over the stats of that tool into the new one? Oh god I hope no one tries to use this with another mod that makes tool crafting need more tools. That just... will break everything.
                //Though I can't help but ask, what if it's a recipe converting one tool to another type? I hope not. Not going to dwell on that until it actually might come up though.
                outputSlot.Itemstack.SetToolhead(foundToolInput.GetToolhead());
                outputSlot.Itemstack.SetToolheadCurrentDurability(foundToolInput.GetToolheadCurrentDurability());
                outputSlot.Itemstack.SetToolCurrentSharpness(foundToolInput.GetToolCurrentSharpness());
                outputSlot.Itemstack.SetToolMaxSharpness(foundToolInput.GetToolMaxSharpness());
                outputSlot.Itemstack.SetToolhandle(foundToolInput.GetToolhandle());
                outputSlot.Itemstack.SetToolhandleCurrentDurability(foundToolInput.GetToolhandleCurrentDurability());
                outputSlot.Itemstack.SetToolhandleMaxDurability(foundToolInput.GetToolhandleMaxDurability());
                var foundToolInputBinding = foundToolInput.GetToolbinding();
                if (foundToolInputBinding != null) { //Whoops. It was a mistake not to be verifying that there even was a binding first. Fixed this hole though!
                    outputSlot.Itemstack.SetToolbinding(foundToolInputBinding);
                }
                outputSlot.Itemstack.SetToolbindingCurrentDurability(foundToolInput.GetToolbindingCurrentDurability());
                outputSlot.Itemstack.SetToolbindingMaxDurability(foundToolInput.GetToolbindingMaxDurability());
                outputSlot.Itemstack.SetSpeedBonus(foundToolInput.GetSpeedBonus());
                outputSlot.Itemstack.SetGripChanceToDamage(foundToolInput.GetGripChanceToDamage());
                return; //Mama mia. Maybe make this chunk another extension? If I ever have to do this again elsewhere.
            } else {
                isHeadMetal = headStack.Collectible.IsCraftableMetal();
            }

            //Remove errant attribute data that might still be on the Tool Head like the temp. Vanilla crafting doesn't carry over the temp so it should be cleared before saving the head to the tool.
            if (headStack.Attributes != null) {
                if (headStack.Attributes.HasAttribute("temperature")) {
                    headStack.Attributes.RemoveAttribute("temperature");
                }
                if (ToolsmithModSystem.Api.ModLoader.IsModEnabled("smithingplus")) { //If Smithing Plus is found, clear it's errant data as well or else it will compound due to how it reassigns it when the head breaks.
                    if (headStack.Attributes.HasAttribute("repairedToolStack")) {
                        headStack.Attributes.RemoveAttribute("repairedToolStack");
                    }
                    if (headStack.Attributes.HasAttribute("repairSmith")) {
                        headStack.Attributes.RemoveAttribute("repairSmith");
                    }
                }
            }
            //Once all (up to) three possible parts are found, access the stats for the handle and binding! The Head doesn't need much done to it, it's the simplest to handle.
            HandleStatPair handle;
            if (handleStack != null) { //It probably shouldn't ever be the case it gets here and Handle is still null but hey.
                if (handleStack.HasHandleStatTag()) {
                    handle = ToolsmithModSystem.Config.BaseHandleRegistry.Get(handleStack.GetHandleStatTag());
                } else {
                    handle = ToolsmithModSystem.Config.BaseHandleRegistry.Get(handleStack.Collectible.Code.Path);
                }
            } else {
                handle = ToolsmithModSystem.Config.BaseHandleRegistry.Get(ToolsmithConstants.DefaultHandlePartKey); //Probably shouldn't ever run into this, but just incase something does go wrong, this might prevent a crash - and default to a stick used. Maybe if configs are not configured right this could happen!
                handleStack = new ItemStack(ToolsmithModSystem.Api.World.GetItem(new AssetLocation(ToolsmithConstants.DefaultHandleCode)), 1);
            }
            BindingStatPair binding;
            if (bindingStack != null) { //If there is a binding used, then get that one.
                if (bindingStack.Attributes != null) {
                    if (bindingStack.Attributes.HasAttribute("temperature")) {
                        bindingStack.Attributes.RemoveAttribute("temperature");
                    }
                }
                binding = ToolsmithModSystem.Config.BindingRegistry.Get(bindingStack.Collectible.Code.Path);
            } else {
                binding = ToolsmithModSystem.Config.BindingRegistry.Get(ToolsmithConstants.DefaultBindingPartKey);
            }
            var handleStats = ToolsmithModSystem.Stats.baseHandles.Get(handle.handleStatTag);

            GripStats gripStats;
            if (handleStack.HasHandleGripTag()) {
                gripStats = ToolsmithModSystem.Stats.grips.Get(handleStack.GetHandleGripTag());
            } else {
                gripStats = ToolsmithModSystem.Stats.grips.Get(ToolsmithConstants.DefaultGripTag);
            }

            TreatmentStats treatmentStats;
            if (handleStack.HasHandleTreatmentTag()) {
                treatmentStats = ToolsmithModSystem.Stats.treatments.Get(handleStack.GetHandleTreatmentTag());
            } else {
                treatmentStats = ToolsmithModSystem.Stats.treatments.Get(ToolsmithConstants.DefaultTreatmentTag);
            }

            BindingStats bindingStats;
            if (binding == null) {
                bindingStats = ToolsmithModSystem.Stats.bindings.Get(ToolsmithConstants.DefaultBindingStatKey);//If binding is still null, none was used! Get those fallback stats.
            } else {
                bindingStats = ToolsmithModSystem.Stats.bindings.Get(binding.bindingStatTag);
            }

            //Various math and calculating the end effect of each part here.
            HandleExtraModCompat(allInputslots, outputSlot); //Handle some mod compatability here! Anything that needs a little bit of extra handling before getting the first BaseMaxDurability.

            var baseDur = outputSlot.Itemstack.Collectible.GetBaseMaxDurability(outputSlot.Itemstack);
            int headDur = outputSlot.Itemstack.GetToolheadMaxDurability();//Start with the tool head.
            int sharpness = (int)(baseDur * ToolsmithModSystem.Config.SharpnessMult);//Calculate the sharpness next similarly to the durability.

            var handleDur = baseDur * handleStats.baseHPfactor; //Starting with the handle: Account for baseHPfactor first in the handle...
            handleDur = handleDur + handleDur * handleStats.selfHPBonus; //plus the selfDurabilityBonus
            handleDur = handleDur + handleDur * treatmentStats.handleHPbonus; //Then any treatment bonus
            handleDur = handleDur + handleDur * bindingStats.handleHPBonus; //Finally the Binding bonus, and all this should be multiplicitive, cause why not haha

            var bindingDur = baseDur * bindingStats.baseHPfactor; //Now for the binding, but this has fewer parts.
            bindingDur = bindingDur + bindingDur * bindingStats.selfHPBonus;
            bindingDur = bindingDur + bindingDur * handleStats.bindingHPBonus;

            //Apply the end results of that to the tool/parts. Could the parts themselves actually hold the stats...? Eh. Might be faster to just directly apply them to the tool and then update the current HP when it breaks.
            var currentHeadPer = headStack.GetPartRemainingHPPercent(); //If this returns 0, then assume it's full durability since something is unset. Keep this assumption in mind!!!
            headStack.SetPartMaxDurability(headDur);
            if (currentHeadPer <= 0) {
                currentHeadPer = 1.0f;
            }
            headStack.SetPartCurrentDurability((int)(headDur * currentHeadPer));
            var currentHeadSharpPer = headStack.GetPartRemainingSharpnessPercent();
            headStack.SetPartMaxSharpness(sharpness);
            if (currentHeadSharpPer <= 0) {
                if (isHeadMetal) {
                    currentHeadSharpPer = ToolsmithConstants.StartingSharpnessMult;
                } else {
                    currentHeadSharpPer = ToolsmithConstants.NonMetalStartingSharpnessMult;
                }
            }
            headStack.SetPartCurrentSharpness((int)(sharpness * currentHeadSharpPer));

            outputSlot.Itemstack.SetToolheadCurrentDurability((int)(headDur * currentHeadPer));
            outputSlot.Itemstack.SetToolMaxSharpness(sharpness);
            outputSlot.Itemstack.SetToolCurrentSharpness((int)(sharpness * currentHeadSharpPer));

            var currentHandlePer = handleStack.GetPartRemainingHPPercent();
            handleStack.SetPartMaxDurability((int)handleDur);
            if (currentHandlePer <= 0) {
                currentHandlePer = 1.0f;
            }
            handleStack.SetPartCurrentDurability((int)(handleDur * currentHandlePer));
            outputSlot.Itemstack.SetToolhandleMaxDurability((int)handleDur);
            outputSlot.Itemstack.SetToolhandleCurrentDurability((int)(handleDur * currentHandlePer));

            outputSlot.Itemstack.SetToolbindingMaxDurability((int)bindingDur);
            outputSlot.Itemstack.SetToolbindingCurrentDurability((int)bindingDur);

            var speedBonus = handleStats.speedBonus + gripStats.speedBonus;
            var gripChanceDamage = gripStats.chanceToDamage;
            outputSlot.Itemstack.SetSpeedBonus(speedBonus);
            outputSlot.Itemstack.SetGripChanceToDamage(gripChanceDamage);

            if (ToolsmithModSystem.Config.DebugMessages) {
                ToolsmithModSystem.Logger.Debug("Tool's durability is: " + baseDur);
                ToolsmithModSystem.Logger.Debug("Thus, the Tool Head's durability is: " + headDur);
                ToolsmithModSystem.Logger.Debug("And the current Head Durability is: " + (int)(headDur * currentHeadPer));
                ToolsmithModSystem.Logger.Debug("The tool's maximum sharpness is: " + sharpness);
                ToolsmithModSystem.Logger.Debug("This tool's current sharpness is: " + (int)(sharpness * currentHeadSharpPer));
                ToolsmithModSystem.Logger.Debug("Handle Max Durability: " + handleDur);
                ToolsmithModSystem.Logger.Debug("Handle Current Durability: " + (int)(handleDur * currentHandlePer));
                ToolsmithModSystem.Logger.Debug("Binding Durability: " + bindingDur);
                ToolsmithModSystem.Logger.Debug("Speed Bonus: " + speedBonus);
                ToolsmithModSystem.Logger.Debug("Chance To Damage: " + gripChanceDamage);
            }

            //Then don't forget to add the ItemStacks for the parts to the tool's attributes to retrieve later on damage/destruction!
            outputSlot.Itemstack.SetToolhead(headStack);
            outputSlot.Itemstack.SetToolhandle(handleStack);
            if (bindingStack != null) {
                outputSlot.Itemstack.SetToolbinding(bindingStack);
            }
            //outputSlot.MarkDirty();

            //outputSlot.Itemstack.Attributes.SetInt(ToolsmithAttributes.Durability, outputSlot.Itemstack.Collectible.GetMaxDurability(outputSlot.Itemstack));
        }

        private void HandleExtraModCompat(ItemSlot[] allInputslots, ItemSlot outputSlot) {
            if (ToolsmithModSystem.Api.ModLoader.IsModEnabled("xskills")) { //Copy over the Quality Attribute (if it exists) onto the output item so that the GetMaxDurability will account for it here!
                HandleXSkillsCompat(allInputslots, outputSlot);
            }

            if (ToolsmithModSystem.Api.ModLoader.IsModEnabled("itemrarity")) {
                HandleItemRarityCompat(allInputslots, outputSlot);
            }
        }

        private void HandleXSkillsCompat(ItemSlot[] allInputslots, ItemSlot outputSlot) {
            float quality = 0.0f;
            foreach (var input in allInputslots.Where(i => !i.Empty)) {
                if (input.Itemstack.Attributes.HasAttribute("quality")) {
                    quality = input.Itemstack.Attributes.GetFloat("quality");
                    break;
                }
            }

            if (quality > 0.0f) {
                outputSlot.Itemstack.Attributes.SetFloat("quality", quality); //Doesn't appear to double up or anything, thankfully!
            }
        }

        private void HandleItemRarityCompat(ItemSlot[] allInputslots, ItemSlot outputSlot) {
            var itemStack = outputSlot.Itemstack;
            if (itemStack == null || itemStack.Item?.Tool == null || itemStack.Attributes.HasAttribute(ModAttributes.Guid)) {
                return;
            }
            var rarity = Rarity.GetRandomRarity();
            itemStack.SetRarity(rarity.Key);
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling) { //Handle the grinding code here as well as the tool itself! Probably can offload the core interaction to a helper utility function?
            if (TinkeringUtility.WhetstoneInOffhand(byEntity) != null && TinkeringUtility.ToolOrHeadNeedsSharpening(slot.Itemstack, byEntity.World)) {
                handHandling = EnumHandHandling.PreventDefault;
                handling = EnumHandling.PreventSubsequent;
                sharpening = true;
                return;
            }

            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling) {
            if (sharpening) {
                handling = EnumHandling.PreventSubsequent;
                return TinkeringUtility.TryWhetstoneSharpening(ref deltaLastTick, ref lastInterval, secondsUsed, slot, byEntity, ref handling);
            }

            return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling) {
            if (sharpening) {
                handling = EnumHandling.PreventDefault;
                deltaLastTick = 0;
                lastInterval = 0;
                var whetstone = TinkeringUtility.WhetstoneInOffhand(byEntity);
                if (whetstone != null) {
                    whetstone.DoneSharpening();
                }
                sharpening = false;
            }

            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
        }
    }
}
