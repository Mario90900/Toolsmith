using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.Client;
using Toolsmith.Client.Behaviors;
using Toolsmith.Config;
using Toolsmith.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Toolsmith.ToolTinkering.Behaviors {
    public class CollectibleBehaviorToolHandle : CollectibleBehaviorToolPartWithHealth { //Mostly here just to allow for easy detection if something is a tool handle!

        public CollectibleBehaviorToolHandle(CollectibleObject collObj) : base(collObj) {

        }

        public override void OnLoaded(ICoreAPI api) {
            base.OnLoaded(api);
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo) {
            dsc.AppendLine(Lang.Get("toolhandledirections"));
            if (inSlot.Itemstack.HasHandleGripTag() && inSlot.Itemstack.HasHandleTreatmentTag()) {
                dsc.AppendLine(Lang.Get("toolhandlefullyprepared", inSlot.Itemstack.GetHandleTreatmentTag(), inSlot.Itemstack.GetHandleGripTag()));
            } else if (inSlot.Itemstack.HasHandleTreatmentTag()) {
                dsc.AppendLine(Lang.Get("toolhandletreated", inSlot.Itemstack.GetHandleTreatmentTag()));
            } else if (inSlot.Itemstack.HasHandleGripTag()) {
                dsc.AppendLine(Lang.Get("toolhandlegripped", inSlot.Itemstack.GetHandleGripTag()));
            }
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }

        public override void GetHeldItemName(StringBuilder sb, ItemStack itemStack) {
            if (itemStack.HasWetTreatment()) {
                sb.Append(Lang.Get("handleiswet"));
            }
        }

        public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, ref EnumHandling bhHandling) {
            ItemSlot toolSlot = null;
            ItemSlot blankSlot = null;
            ItemSlot handleSlot = null;
            ItemSlot gripOrTreatmentSlot = null;

            foreach (var slot in allInputslots) {
                if (!slot.Empty && (slot.Itemstack.Collectible.Code != ToolsmithConstants.SandpaperCode || slot.Itemstack.Collectible.Code != ToolsmithConstants.FirewoodCode)) {
                    if (slot.Itemstack.Collectible.Tool != null) {
                        toolSlot = slot;
                    } else if (slot.Itemstack.Collectible.Code.FirstCodePart() == ToolsmithConstants.HandleBlankCode) {
                        blankSlot = slot;
                    } else if (slot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorToolHandle>()) {
                        handleSlot = slot;
                    } else if (slot.Itemstack != null) {
                        if (slot.Itemstack.Collectible.Code.Path.StartsWith(ToolsmithAttributes.OldHandlePrefix)) { //If we find an old handle it's time to convert it to the new ones. Remove this bit later on after some time.
                            outputSlot.Itemstack = ItemStackExtensions.CheckForOldHandleAndConvert(slot.Itemstack);
                            return;
                        }
                        gripOrTreatmentSlot = slot;
                    }
                }
            }

            if (toolSlot != null && blankSlot != null) {
                var woodtype = blankSlot.Itemstack.Collectible.LastCodePart();
                if (woodtype != null) {
                    if (woodtype == "veryaged" || woodtype == "veryagedrotten") {
                        woodtype = "aged";
                    }
                    ITreeAttribute multiPartTree = outputSlot.Itemstack.GetMultiPartRenderTree();
                    ITreeAttribute handlePartAndTransformTree = multiPartTree.GetPartAndTransformRenderTree(ToolsmithAttributes.ModularPartHandleName);
                    ITreeAttribute handleRenderTree = handlePartAndTransformTree.GetPartRenderTree();
                    ITreeAttribute handleTextureTree = handleRenderTree.GetPartTextureTree();
                    var woodtypeTextPath = ToolsmithConstants.DebarkedWoodPathMinusType + woodtype;
                    handleTextureTree.SetString("wood", woodtypeTextPath);
                    HandleStatPair handleStats = ToolsmithModSystem.Config.BaseHandleRegistry[outputSlot.Itemstack.Collectible.Code.Path];
                    handleRenderTree.SetPartShapePath(handleStats.handleShapePath);
                    outputSlot.Itemstack.SetHandleStatTag(handleStats.handleStatTag);
                    outputSlot.Itemstack.SetPartCurrentDurability(1000);
                    outputSlot.Itemstack.SetPartMaxDurability(1000);
                    bhHandling = EnumHandling.Handled;
                }
            } else if (handleSlot != null && gripOrTreatmentSlot != null) {
                outputSlot.Itemstack.Attributes = handleSlot.Itemstack.Attributes.Clone();
                if (handleSlot.Itemstack.HasPartRenderTree()) { //If this is a spawned-in handle from creative, this will catch it and convert from a PartRenderTree to a MultiPartRenderTree.
                    ITreeAttribute handlePartTree = handleSlot.Itemstack.GetPartRenderTree().Clone();
                    outputSlot.Itemstack.RemovePartRenderTree();

                    ITreeAttribute multiPartTreeTemp = outputSlot.Itemstack.GetMultiPartRenderTree();
                    ITreeAttribute handlePartAndTransformTree = multiPartTreeTemp.GetPartAndTransformRenderTree(ToolsmithAttributes.ModularPartHandleName);
                    handlePartAndTransformTree.SetPartRenderTree(handlePartTree);
                }
                ITreeAttribute multiPartTree = outputSlot.Itemstack.GetMultiPartRenderTree();

                if (ToolsmithModSystem.Config.GripRegistry.ContainsKey(gripOrTreatmentSlot.Itemstack.Collectible.Code.Path)) {
                    if (handleSlot.Itemstack.HasHandleGripTag() || handleSlot.Itemstack.HasWetTreatment()) {
                        outputSlot.Itemstack = null;
                        outputSlot.Itemstack = new ItemStack(ToolsmithModSystem.Api.World.GetBlock(new AssetLocation("game:air")));
                        outputSlot.Itemstack.SetDisposeMeNowPlease();
                        bhHandling = EnumHandling.PreventDefault;
                    } else {
                        var grip = gripOrTreatmentSlot.Itemstack;
                        var gripWithStats = ToolsmithModSystem.Config.GripRegistry[grip.Collectible.Code.Path];
                        var gripStats = ToolsmithModSystem.Stats.grips[gripWithStats.gripStatTag];
                        ITreeAttribute gripPartTree = multiPartTree.GetPartAndTransformRenderTree(ToolsmithAttributes.ModularPartGripName);
                        ITreeAttribute gripRenderTree = gripPartTree.GetPartRenderTree();
                        ITreeAttribute gripTextureTree = gripRenderTree.GetPartTextureTree();
                        if (handleSlot.Itemstack.Collectible.Code.Path == "crudehandle") {
                            gripRenderTree.SetShapeOverrideTag("-crude");
                        }
                        gripRenderTree.SetPartShapePath(gripWithStats.gripShapePath);
                        outputSlot.Itemstack.SetHandleGripTag(gripStats.id);
                        if (gripWithStats.gripTextureOverride != "") {
                            gripTextureTree.SetString("grip", gripWithStats.gripTextureOverride);
                        } else {
                            gripTextureTree.SetString("grip", gripStats.texturePath);
                        }
                        bhHandling = EnumHandling.Handled;
                    }
                } else {
                    if (handleSlot.Itemstack.HasHandleGripTag() || handleSlot.Itemstack.HasHandleTreatmentTag()) {
                        outputSlot.Itemstack = null;
                        outputSlot.Itemstack = new ItemStack(ToolsmithModSystem.Api.World.GetBlock(new AssetLocation("game:air")));
                        outputSlot.Itemstack.SetDisposeMeNowPlease();
                        bhHandling = EnumHandling.PreventDefault;
                    } else {
                        ITreeAttribute handlePartTree = multiPartTree.GetPartAndTransformRenderTree(ToolsmithAttributes.ModularPartHandleName);
                        ITreeAttribute handleRenderTree = handlePartTree.GetPartRenderTree();
                        ITreeAttribute handleTextureTree = handleRenderTree.GetPartTextureTree();
                        var treatment = gripOrTreatmentSlot.Itemstack;
                        var treatmentStatPair = ToolsmithModSystem.Config.TreatmentRegistry[treatment.Collectible.Code.Path];
                        var treatmentStats = ToolsmithModSystem.Stats.treatments[treatmentStatPair.treatmentStatTag];
                        var handleStatPair = ToolsmithModSystem.Config.BaseHandleRegistry[handleSlot.Itemstack.Collectible.Code.Path];
                        outputSlot.Itemstack.SetHandleTreatmentTag(treatmentStats.id);
                        outputSlot.Itemstack.SetWetTreatment((int)(treatmentStatPair.dryingHours * handleStatPair.dryingTimeMult));
                        outputSlot.Itemstack.Collectible.SetTransitionState(outputSlot.Itemstack, EnumTransitionType.Dry, 0);

                        if (treatmentStatPair.isLiquid && (treatment as ILiquidInterface) != null) {
                            treatment = (treatment as ILiquidInterface).GetContent(treatment);
                            handleTextureTree.SetString("wood-overlay", ToolsmithConstants.DarkTreatementOverlayPath);
                        } else if (!treatmentStatPair.isLiquid) {
                            handleTextureTree.SetString("wood-overlay", ToolsmithConstants.LightTreatementOverlayPath);
                        } else {
                            ToolsmithModSystem.Logger.Error("A treatment config is improperly set! This treatment - " + treatment.Collectible.Code + " - was marked as a liquid, but could not find a liquid container in this recipe. Will treat it as a non-liquid, but is worth fixing that!");
                            handleTextureTree.SetString("wood-overlay", ToolsmithConstants.LightTreatementOverlayPath);
                        }
                        bhHandling = EnumHandling.Handled;
                    }
                }
            } else if (handleSlot != null) {
                outputSlot.Itemstack.Attributes = handleSlot.Itemstack.Attributes.Clone();
            } else { //If it hits this, it's likely someone crafted a more basic handle somehow, like a stick or crude handle. Lets initialize it so it can at least have the trees.
                var renderTree = outputSlot.Itemstack.GetPartRenderTree();
                renderTree.GetPartTextureTree();
            }

            outputSlot.MarkDirty();
        }
    }
}
