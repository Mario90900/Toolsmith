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
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }

        public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, ref EnumHandling bhHandling) {
            ItemSlot toolSlot = null;
            ItemSlot blankSlot = null;
            ItemSlot handleSlot = null;
            ItemSlot gripOrTreatmentSlot = null;

            bhHandling = EnumHandling.Handled;
            foreach (var slot in allInputslots) {
                if (!slot.Empty && (slot.Itemstack.Collectible.Code != ToolsmithConstants.SandpaperCode || slot.Itemstack.Collectible.Code != ToolsmithConstants.FirewoodCode)) {
                    if (slot.Itemstack.Collectible.Tool != null) {
                        toolSlot = slot;
                    } else if (slot.Itemstack.Collectible.Code.FirstCodePart() == ToolsmithConstants.HandleBlankCode) {
                        blankSlot = slot;
                    } else if (slot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorToolHandle>()) {
                        handleSlot = slot;
                    } else {
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
                    ITreeAttribute renderTree = outputSlot.Itemstack.GetPartRenderTree();
                    ITreeAttribute textureTree = renderTree.GetPartTextureTree();
                    var woodtypeTextPath = ToolsmithConstants.DebarkedWoodPathMinusType + woodtype;
                    textureTree.SetString("shaft", woodtypeTextPath);
                    outputSlot.Itemstack.SetPartCurrentDurability(1000);
                    outputSlot.Itemstack.SetPartMaxDurability(1000);
                }
            } else if (handleSlot != null && gripOrTreatmentSlot != null) {
                outputSlot.Itemstack.Attributes = handleSlot.Itemstack.Attributes.Clone();
                ITreeAttribute renderTree = outputSlot.Itemstack.GetPartRenderTree();
                ITreeAttribute textureTree = renderTree.GetPartTextureTree();

                if (ToolsmithModSystem.Config.GripRegistry.ContainsKey(gripOrTreatmentSlot.Itemstack.Collectible.Code.Path)) {
                    if (handleSlot.Itemstack.HasHandleGripTag()) {
                        outputSlot.Itemstack = null;
                    } else {
                        var grip = gripOrTreatmentSlot.Itemstack;
                        renderTree.SetPartShapeIndex(handleSlot.Itemstack.Collectible.Code.Path);
                        var gripStats = ToolsmithModSystem.Stats.grips[ToolsmithModSystem.Config.GripRegistry[grip.Collectible.Code.Path].gripStatTag];
                        outputSlot.Itemstack.SetHandleGripTag(gripStats.id);
                        textureTree.SetString("grip", gripStats.texturePath);
                    }
                } else {
                    if (handleSlot.Itemstack.HasHandleGripTag() || handleSlot.Itemstack.HasHandleTreatmentTag()) {
                        outputSlot.Itemstack = null;
                    } else {
                        var treatment = gripOrTreatmentSlot.Itemstack;
                        var treatmentStatPair = ToolsmithModSystem.Config.TreatmentRegistry[treatment.Collectible.Code.Path];
                        var treatmentStats = ToolsmithModSystem.Stats.treatments[treatmentStatPair.treatmentStatTag];
                        var handleStatPair = ToolsmithModSystem.Config.BaseHandleRegistry[handleSlot.Itemstack.Collectible.Code.Path];
                        outputSlot.Itemstack.SetHandleTreatmentTag(treatmentStats.id);

                        /*if (ToolsmithModSystem.Api.Side.IsServer()) {
                        if (outputSlot.Itemstack.Collectible.TransitionableProps == null) {
                                outputSlot.Itemstack.Collectible.TransitionableProps = Array.Empty<TransitionableProperties>();
                            }

                            var transProp = new TransitionableProperties {
                                Type = EnumTransitionType.Dry,
                                FreshHours = NatFloat.createUniform(0, 0),
                                TransitionHours = NatFloat.createUniform((treatmentStatPair.dryingHours * handleStatPair.dryingTimeMult), 0),
                                TransitionedStack = new JsonItemStack { Type = handleSlot.Itemstack.Collectible.ItemClass, Code = handleSlot.Itemstack.Collectible.Code, Attributes = JsonObject.FromJson(handleSlot.Itemstack.Attributes.ToJsonToken()) },
                                TransitionRatio = 1
                            };
                            outputSlot.Itemstack.Collectible.TransitionableProps.Append(transProp);
                            outputSlot.Itemstack.Collectible.SetTransitionState(outputSlot.Itemstack, EnumTransitionType.Dry, 0);
                        }*/

                        if (treatmentStatPair.isLiquid && (treatment as ILiquidInterface) != null) {
                            treatment = (treatment as ILiquidInterface).GetContent(treatment);
                            textureTree.SetOverlay("shaft", ToolsmithConstants.DarkTreatementOverlayPath);
                        } else if (!treatmentStatPair.isLiquid) {
                            textureTree.SetOverlay("shaft", ToolsmithConstants.LightTreatementOverlayPath);
                        } else {
                            ToolsmithModSystem.Logger.Error("A treatment config is improperly set! This treatment - " + treatment.Collectible.Code + " - was marked as a liquid, but could not find a liquid container in this recipe. Will treat it as a non-liquid, but is worth fixing that!");
                            textureTree.SetOverlay("shaft", ToolsmithConstants.LightTreatementOverlayPath);
                        }
                    }
                }
            } else if (handleSlot != null) {
                outputSlot.Itemstack.Attributes = handleSlot.Itemstack.Attributes.Clone();
            }

            outputSlot.MarkDirty();
        }
    }
}
