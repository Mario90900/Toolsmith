using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.Server;

namespace Toolsmith.ToolTinkering.Behaviors {
    public class CollectibleBehaviorToolHead : CollectibleBehaviorToolPartWithHealth {

        private bool crafting = false;
        private bool sharpening = false;
        protected float deltaLastTick = 0;
        protected float lastInterval = 0;

        public CollectibleBehaviorToolHead(CollectibleObject collObj) : base(collObj) {

        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling) { //Handle the grinding code here as well as the tool itself! Probably can offload the core interaction to a helper utility function?
            var entPlayer = (byEntity as EntityPlayer);
            
            if (TinkeringUtility.ValidHandleInOffhand(byEntity)) { //Check for Handle in Offhand
                handHandling = EnumHandHandling.PreventDefault;
                handling = EnumHandling.PreventSubsequent;
                if (byEntity.World.Side == EnumAppSide.Server) {
                    byEntity.World.PlaySoundAt(new AssetLocation("sounds/player/messycraft.ogg"), byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z, null, true, 32f, 1f);
                }
                crafting = true;
                return;
            } else if (TinkeringUtility.WhetstoneInOffhand(byEntity) != null && TinkeringUtility.ToolOrHeadNeedsSharpening(slot.Itemstack, byEntity.World)) {
                handHandling = EnumHandHandling.PreventDefault;
                handling = EnumHandling.PreventSubsequent;
                sharpening = true;
                return;
            } else if (entPlayer != null && entPlayer.Controls.ShiftKey && blockSel.Block.Code.FirstCodePart() == "toolsmith:grindstone") {
                ToolsmithModSystem.Logger.Warning("Found a grindstone!"); //TODO - This doesn't exactly work, maybe the grindstone intercepts it.
            }

            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling) {
            if (crafting) {
                handling = EnumHandling.PreventSubsequent;
                return crafting && secondsUsed < ToolsmithConstants.TimeToCraftTinkerTool; //Time for crafting is now a constant variable!
            } else if (sharpening) {
                handling = EnumHandling.PreventSubsequent;
                return TinkeringUtility.TryWhetstoneSharpening(ref deltaLastTick, ref lastInterval, secondsUsed, slot, byEntity, ref handling);
            }

            return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling) {
            if (crafting && secondsUsed >= ToolsmithConstants.TimeToCraftTinkerTool - 0.1) { //If they were crafting, verify that the countdown is up, and if so, craft it (if there still is a valid offhand handle!)
                handling = EnumHandling.PreventDefault;
                if (byEntity.World.Side.IsServer() && TinkeringUtility.ValidHandleInOffhand(byEntity)) {
                    TinkeringUtility.AssemblePartBundle(slot, byEntity, blockSel);
                }
                crafting = false;
                return;
            } else if (sharpening) {
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

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
            dsc.AppendLine(Lang.Get("toolheaddirections"));
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }

        public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, ref EnumHandling bhHandling) { //TODO - This isn't called when a tool head is smithed. Buh! Have to move it elsewhere, or otherwise get this called... Where does XSkills patch in the Quality for Smithed tools?
            //I do hope this also gets called when Smithing/Knapping completes. I think it should?

            bool isToolMetal = outputSlot.Itemstack.Collectible.IsCraftableMetal();
            int baseDur = 1000; //Since we don't know the actual base durability YET for the tool, until it is crafted. So this is a placeholder.
            int partDur = (int)(baseDur * ToolsmithModSystem.Config.HeadDurabilityMult);
            int sharpness = (int)(baseDur * ToolsmithModSystem.Config.SharpnessMult);
            int startingSharpness;
            if (isToolMetal) {
                startingSharpness = (int)(sharpness * ToolsmithConstants.StartingSharpnessMult);
            } else {
                startingSharpness = (int)(sharpness * ToolsmithConstants.NonMetalStartingSharpnessMult);
            }

            outputSlot.Itemstack.SetPartCurrentDurability(partDur);
            outputSlot.Itemstack.SetPartMaxDurability(partDur);
            outputSlot.Itemstack.SetToolCurrentSharpness(startingSharpness);
            outputSlot.Itemstack.SetToolMaxSharpness(sharpness);

            if (ToolsmithModSystem.Config.DebugMessages) {
                ToolsmithModSystem.Logger.Debug("The starting Sharpness is: " + startingSharpness);
                ToolsmithModSystem.Logger.Debug("Finally the max Sharpness is: " + sharpness);
            }
        }
    }
}
