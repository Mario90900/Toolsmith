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

namespace Toolsmith.ToolTinkering {
    public class CollectibleBehaviorTinkeredTools : CollectibleBehaviorSmithedTool {

        public CollectibleBehaviorTinkeredTools(CollectibleObject collObj) : base(collObj) {
            
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) { //This only seems to get called on the clientside, which makes sense. Whoops, it can't be a catch-all to fix null tools like I thought, but it's still important to display the durabilities before the item's actually used.
            if (inSlot.Itemstack == null || inSlot.Inventory.GetType() == typeof(DummyInventory) || inSlot.Inventory.GetType() == typeof(CreativeInventoryTab) || (ToolsmithModSystem.IgnoreCodes.Count > 0 && ToolsmithModSystem.IgnoreCodes.Contains(inSlot.Itemstack.Collectible.Code.ToString()))) { //I don't think it's possible for the itemstack to be null at this point, but JUST IN CASE I'll confirm it.
                return; //If this item is in a DummyInventory or CreativeInventoryTab, it's likely not an actual item - but something rendering in a Handbook slot or creative inventory slot I believe. Lets just not mess with those, they won't have data anyway.
            }
            
            var curHeadDur = inSlot.Itemstack.GetToolheadCurrentDurability();
            var maxHeadDur = inSlot.Itemstack.GetToolheadMaxDurability();
            var curHandleDur = inSlot.Itemstack.GetToolhandleCurrentDurability();
            var maxHandleDur = inSlot.Itemstack.GetToolhandleMaxDurability();
            var curBindingDur = inSlot.Itemstack.GetToolbindingCurrentDurability();
            var maxBindingDur = inSlot.Itemstack.GetToolbindingMaxDurability();

            if (maxHeadDur < 0) { //If this is 0 then assume something went wrong and reset things, it's a new item spawned in, or a player added the mod to their save.
                inSlot.Itemstack.ResetNullHead(world); //Moved the client-half of resetting the tool head into this call. Can be safely called on both sides, and handle it over there. Make sure to mark the itemslot as dirty on the client though after using this.
                curHeadDur = inSlot.Itemstack.GetToolheadCurrentDurability();
                maxHeadDur = inSlot.Itemstack.GetToolheadMaxDurability();
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
            bool debugFlag = false; //True after the Attribute line has been found
            bool foundLine = false;
            while (endIndex < workingDsc.Length && foundLine == false) { //Find and trim off the original 'Durability' information, and then...
                if (workingDsc[endIndex] == '\n') {
                    startIndex = endIndex + 1;
                }
                if (!withDebugInfo && workingDsc[endIndex] == 'D') { //I don't know if this will work for any languages other then english? And I'm worried to find out, haha.
                    foundLine = true;
                }
                if (withDebugInfo && debugFlag && workingDsc[endIndex] == 'D') { //This whole bit is specifically searching for the English translated code... So this might cause issues in other languages. Oof.
                    if (startIndex == endIndex) {
                        foundLine = true;
                    }
                }
                if (withDebugInfo && !debugFlag && (((world.Api.Side == EnumAppSide.Client && (world.Api as ICoreClientAPI).Input.KeyboardKeyStateRaw[1]) && workingDsc[endIndex] == 'A') || (workingDsc[endIndex] == 'C'))) {
                    startIndex = endIndex;
                    debugFlag = true;
                }
                endIndex++;
            }
            while (endIndex < workingDsc.Length && workingDsc[endIndex] != '\n') {
                endIndex++;
            }
            if (endIndex < workingDsc.Length) {
                workingDsc.Remove(startIndex, (endIndex - startIndex) + 1); //Remove the durability line
            }

            workingDsc.Insert(startIndex, Lang.Get("toolbindingdurability", curBindingDur, maxBindingDur) + '\n'); //Insert in the part durabilities in the place of it
            workingDsc.Insert(startIndex, Lang.Get("toolhandledurability", curHandleDur, maxHandleDur) + '\n');
            workingDsc.Insert(startIndex, Lang.Get("toolheaddurability", curHeadDur, maxHeadDur) + '\n');
            
            dsc.Clear();
            dsc.Append(workingDsc);
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
            } else if (headStack == null && foundToolInput != null) { //Probably a safety check here, since I realized some recipes IE the whetstone from Working Classes craft a knife with the stone to produce a knife.
                //Actually found an input that is a tool, so probably copy over the stats of that tool into the new one? Oh god I hope no one tries to use this with another mod that makes tool crafting need more tools. That just... will break everything.
                //Though I can't help but ask, what if it's a recipe converting one tool to another type? I hope not. Not going to dwell on that until it actually might come up though.
                outputSlot.Itemstack.SetToolhead(foundToolInput.GetToolhead());
                outputSlot.Itemstack.SetToolheadCurrentDurability(foundToolInput.GetToolheadCurrentDurability());
                outputSlot.Itemstack.SetToolheadMaxDurability(foundToolInput.GetToolheadMaxDurability());
                outputSlot.Itemstack.SetToolCurrentSharpness(foundToolInput.GetToolCurrentSharpness());
                outputSlot.Itemstack.SetToolMaxSharpness(foundToolInput.GetToolMaxSharpness());
                outputSlot.Itemstack.SetToolhandle(foundToolInput.GetToolhandle());
                outputSlot.Itemstack.SetToolhandleCurrentDurability(foundToolInput.GetToolhandleCurrentDurability());
                outputSlot.Itemstack.SetToolhandleMaxDurability(foundToolInput.GetToolhandleMaxDurability());
                outputSlot.Itemstack.SetToolbinding(foundToolInput.GetToolbinding());
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
            HandleWithStats handle;
            if (handleStack != null) {
                handle = ToolsmithModSystem.Config.ToolHandlesWithStats.Get(handleStack.Collectible.Code.Path);
            } else {
                handle = ToolsmithModSystem.Config.ToolHandlesWithStats.Get(ToolsmithConstants.DefaultHandlePartKey); //Probably shouldn't ever run into this, but just incase something does go wrong, this might prevent a crash - and default to a stick used. Maybe if configs are not configured right this could happen!
            }
            BindingWithStats binding;
            if (bindingStack != null) { //If there is a binding used, then get that one.
                if (bindingStack.Attributes != null) {
                    if (bindingStack.Attributes.HasAttribute("temperature")) {
                        bindingStack.Attributes.RemoveAttribute("temperature");
                    }
                }
                binding = ToolsmithModSystem.Config.BindingsWithStats.Get(bindingStack.Collectible.Code.Path);
            } else { 
                binding = ToolsmithModSystem.Config.BindingsWithStats.Get(ToolsmithConstants.DefaultBindingPartKey);
            }
            var handleStats = ToolsmithModSystem.Stats.handles.Get(handle.handleStats);
            var gripStats = ToolsmithModSystem.Stats.grips.Get(handle.gripStats);
            var treatmentStats = ToolsmithModSystem.Stats.treatments.Get(handle.treatmentStats);
            BindingStats bindingStats;
            if (binding == null) {
                bindingStats = ToolsmithModSystem.Stats.bindings.Get(ToolsmithConstants.DefaultBindingStatKey);//If binding is still null, none was used! Get those fallback stats.
            } else {
                bindingStats = ToolsmithModSystem.Stats.bindings.Get(binding.bindingStats);
            }

            //Various math and calculating the end effect of each part here.
            HandleExtraModCompat(allInputslots, outputSlot); //Handle some mod compatability here! Anything that needs a little bit of extra handling before getting the first BaseMaxDurability.

            var baseDur = outputSlot.Itemstack.Collectible.GetBaseMaxDurability(outputSlot.Itemstack);
            int headDur = (int)(baseDur * ToolsmithModSystem.Config.HeadDurabilityMult);//Start with the tool head, find out the base durability of the tool, multiply that by 5.
            int sharpness = (int)(baseDur * ToolsmithModSystem.Config.SharpnessMult);//Calculate the sharpness next similarly to the durability.

            var handleDur = baseDur * handleStats.baseHPfactor; //Starting with the handle: Account for baseHPfactor first in the handle...
            handleDur = handleDur + (handleDur * handleStats.selfHPBonus); //plus the selfDurabilityBonus
            handleDur = handleDur + (handleDur * treatmentStats.handleHPbonus); //Then any treatment bonus
            handleDur = handleDur + (handleDur * bindingStats.handleHPBonus); //Finally the Binding bonus, and all this should be multiplicitive, cause why not haha

            var bindingDur = baseDur * bindingStats.baseHPfactor; //Now for the binding, but this has fewer parts.
            bindingDur = bindingDur + (bindingDur * bindingStats.selfHPBonus);
            bindingDur = bindingDur + (bindingDur * handleStats.bindingHPBonus);

            //Apply the end results of that to the tool/parts. Could the parts themselves actually hold the stats...? Eh. Might be faster to just directly apply them to the tool and then update the current HP when it breaks.
            var currentHeadPer = headStack.GetPartRemainingHPPercent(); //If this returns 0, then assume it's full durability since something is unset. Keep this assumption in mind!!!
            headStack.SetMaxPartDurability(headDur);
            if (currentHeadPer <= 0) {
                currentHeadPer = 1.0f;
            }
            headStack.SetCurrentPartDurability((int)(headDur * currentHeadPer));
            var currentHeadSharpPer = headStack.GetPartRemainingSharpnessPercent();
            headStack.SetToolMaxSharpness(sharpness);
            if (currentHeadSharpPer <= 0) {
                if (isHeadMetal) {
                    currentHeadSharpPer = ToolsmithConstants.StartingSharpnessMult;
                } else {
                    currentHeadSharpPer = ToolsmithConstants.NonMetalStartingSharpnessMult;
                }
            }
            headStack.SetToolCurrentSharpness((int)(sharpness * currentHeadSharpPer));
            
            outputSlot.Itemstack.SetToolheadMaxDurability(headDur);
            outputSlot.Itemstack.SetToolheadCurrentDurability((int)(headDur * currentHeadPer));
            outputSlot.Itemstack.SetToolMaxSharpness(sharpness);
            outputSlot.Itemstack.SetToolCurrentSharpness((int)(sharpness * currentHeadSharpPer));

            var currentHandlePer = handleStack.GetPartRemainingHPPercent();
            handleStack.SetMaxPartDurability((int)handleDur);
            if (currentHandlePer <= 0) {
                currentHandlePer = 1.0f;
            }
            handleStack.SetCurrentPartDurability((int)(handleDur * currentHandlePer));
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

            outputSlot.Itemstack.Attributes.SetInt(ToolsmithAttributes.Durability, outputSlot.Itemstack.Collectible.GetMaxDurability(outputSlot.Itemstack));
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
    }
}
