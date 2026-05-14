using ScientificSmithy.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Toolsmith.ToolTinkering.Behaviors {
    public class CollectibleBehaviorSmithedTools : CollectibleBehavior {



        public CollectibleBehaviorSmithedTools(CollectibleObject collObj) : base(collObj) {

        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
            if (TinkeringUtility.ShouldNotAccessStats(inSlot)) {
                return;
            }

            var curSharp = inSlot.Itemstack.GetToolCurrentSharpness(); //All that needs to be added to the stringbuilder is the Sharpness.
            var maxSharp = inSlot.Itemstack.GetToolMaxSharpness();

            StringBuilder workingDsc = new StringBuilder();
            workingDsc.Append(dsc); //Still for safety sake, lets copy dsc into a temp one for active processing.
            int startIndex = 0;
            int endIndex = 0;

            StringHelpers.FindTooltipVanillaDurabilityLine(ref startIndex, ref endIndex, workingDsc, world, withDebugInfo); //Moved this code originally from TinkerTools into it's own helper function.

            if (!inSlot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorToolBlunt>()) {
                if (inSlot.Itemstack.HasTotalHoneValue() && inSlot.Itemstack.GetTotalHoneValue() > 0 && inSlot.Itemstack.GetTotalHoneValue() < 1) {
                    workingDsc.AppendLine(Lang.Get("smithedtoolhoninginprogress"));
                } else if (!inSlot.Itemstack.HasTotalHoneValue()) {
                    workingDsc.AppendLine(Lang.Get("smithedtoolfreehone"));
                }
                workingDsc.Insert(startIndex, Lang.Get("toolsharpness", curSharp, maxSharp) + '\n');
            }

            dsc.Clear();
            dsc.Append(workingDsc);
        }

        public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, IRecipeBase byRecipe, ref EnumHandling bhHandling) {
            ItemStack foundToolInput = null; //I do hope this gets called when Smithing completes. I think it should?
            if (allInputslots.Length > 0) {
                foreach (var slot in allInputslots.Where(i => i.Itemstack != null)) {
                    if (slot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorSmithedTools>() && slot.Itemstack.Collectible.Code == outputSlot.Itemstack.Collectible.Code) {
                        foundToolInput = slot.Itemstack.Clone();
                    }
                }
            }

            bool isToolMetal = false;
            if (foundToolInput != null) { //If it's a recipe where a tool is being converted or repaired I guess?
                outputSlot.Itemstack.SetSmithedDurability(foundToolInput.GetSmithedDurability());
                outputSlot.Itemstack.SetToolCurrentSharpness(foundToolInput.GetToolCurrentSharpness());
                outputSlot.Itemstack.SetToolMaxSharpness(foundToolInput.GetToolMaxSharpness());
                return;
            } else {
                isToolMetal = outputSlot.Itemstack.Collectible.IsCraftableMetal();
            }

            var baseDur = outputSlot.Itemstack.Collectible.GetBaseMaxDurability(outputSlot.Itemstack);
            var toolDur = outputSlot.Itemstack.GetSmithedMaxDurability();
            int sharpness;

            if (outputSlot.Itemstack.Attributes.HasAttribute(ScientificSmithyAttr.StatsAttr)) {
                ITreeAttribute stats = outputSlot.Itemstack.Attributes.GetTreeAttribute(ScientificSmithyAttr.StatsAttr);
                float sharpMult = stats.GetFloat(ScientificSmithyAttr.HardnessMultAttr, (float)ToolsmithModSystem.Config.SharpnessMult);
                int halfTough = stats.GetInt(ScientificSmithyAttr.HalfToughAttr, baseDur);
                sharpness = (int)(sharpMult * halfTough);
            } else
                sharpness = (int)(baseDur * ToolsmithModSystem.Config.SharpnessMult);

            int startingSharpness;
            if (isToolMetal) {
                startingSharpness = (int)(sharpness * ToolsmithConstants.StartingSharpnessMult);
            } else {
                startingSharpness = (int)(sharpness * ToolsmithConstants.NonMetalStartingSharpnessMult);
            }

            outputSlot.Itemstack.SetSmithedDurability(toolDur);
            outputSlot.Itemstack.SetToolCurrentSharpness(startingSharpness);
            outputSlot.Itemstack.SetToolMaxSharpness(sharpness);

            if (ToolsmithModSystem.Config.DebugMessages) {
                ToolsmithModSystem.Logger.Debug("Tool's durability is: " + baseDur);
                ToolsmithModSystem.Logger.Debug("So the Smithed Durability is: " + toolDur);
                ToolsmithModSystem.Logger.Debug("And the starting Sharpness is: " + startingSharpness);
                ToolsmithModSystem.Logger.Debug("Finally the max Sharpness is: " + sharpness);
                if (allInputslots.Length > 0) {
                    ToolsmithModSystem.Logger.Debug("This tool had input slots! Was it grid crafted?");
                } else {
                    ToolsmithModSystem.Logger.Debug("This tool had no input slots! Was it smithed?");
                }
            }
        }

        //This call is handled SLIGHTLY different from Tinkered Tools. While on Tinkered Tools, we want to fully handle the damage and breaking of them...
        //Smithed Tools really only need to care about checking if it should be damaged or not, and handle Sharpness. Everything else can be handled by vanilla.
        public override void OnDamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, ref int amount, ref EnumHandling bhHandling) {
            if (world.Side.IsServer()) { //If it's a smithed tool, only need to deal with the Sharpness, and any extra "head" damage. Head in this case is just the tool as a whole.
                ItemStack itemStack = itemslot.Itemstack;
                bool isBluntTool = itemslot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorToolBlunt>();
                var currentDur = itemStack.GetSmithedDurability();

                //Time for SHARPNESS and WEAR! Lets a go!
                bool doDamageTool = false;
                bool doubleToolDamage = false;
                int currentSharpness = itemStack.GetToolCurrentSharpness();
                int maxSharpness = itemStack.GetToolMaxSharpness();
                float sharpnessPer = itemStack.GetToolSharpnessPercent();

                if (maxSharpness <= 1) { //If the Sharpness Max is 1, likely means something got marked improperly. I don't think it could be 1 otherwise?
                    sharpnessPer = 0f; //Set the percent to one as a placeholder to just avoid infinite sharpness.
                }
                if (currentSharpness > 0) {
                    currentSharpness -= amount;
                } else {
                    doubleToolDamage = true;
                }

                if (!isBluntTool) {
                    itemStack.SetToolCurrentSharpness(currentSharpness);
                    itemStack.SetTotalHoneValue(0);
                }

                if (sharpnessPer < 0.98f) {
                    if (!isBluntTool) {
                        doDamageTool = world.Rand.NextDouble() <= ToolsmithModSystem.Config.SharpWear;
                    } else {
                        doDamageTool = world.Rand.NextDouble() <= ToolsmithModSystem.Config.BluntWear;
                    }
                }

                if (doubleToolDamage && doDamageTool) {
                    currentDur -= amount;
                    itemStack.SetSmithedDurability(currentDur);
                }

                if (doDamageTool && amount >= currentDur) {
                    if (world.Api.ModLoader.IsModEnabled("canjewelry")) {
                        TinkeringUtility.HandleGemDropsForJewelry(byEntity, itemStack);
                    }
                }

                itemslot.MarkDirty();

                if(!doDamageTool) {
                    bhHandling = EnumHandling.PreventDefault; //This should only prevent the default in the case of we don't want to damage the tool. Otherwise vanilla damages it like normal, and handles the breaking.
                }
            } else if (!world.Side.IsServer()) {
                bhHandling = EnumHandling.PreventDefault; //Clientside Catch for hitting this point, wait for the server sync to update everything to hopefully prevent that desync from the client
            }
        }

        //Tyron and Co you are fucking amazing for this. Actually sending the BH the initial max durability like this is SO HELPFUL...
        public override int GetMaxDurability(ItemStack itemstack, int durability, ref EnumHandling bhHandling) {
            bhHandling = EnumHandling.PreventDefault;
            return (int)((double)durability * ToolsmithModSystem.Config.HeadDurabilityMult);
        }

        //THIS TOO! It's like- Holy crap, it's like it was made for me.
        public override float GetMiningSpeed(ItemStack itemstack, BlockSelection blockSel, Block block, IPlayer forPlayer, ref EnumHandling bhHandling) {
            bhHandling = EnumHandling.Handled;

            float speedMult = 1f;
            var sharpnessPer = itemstack.GetToolSharpnessPercent();
            if (sharpnessPer >= 0.9) {
                speedMult += speedMult * ToolsmithConstants.HighSharpnessSpeedBonusMult;
            } else if (sharpnessPer <= 0.33) {
                speedMult += speedMult * ToolsmithConstants.LowSharpnessSpeedMalusMult;
            }

            return speedMult;
        }
    }
}
