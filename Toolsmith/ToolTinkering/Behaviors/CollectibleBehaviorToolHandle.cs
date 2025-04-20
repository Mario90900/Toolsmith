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

        private Dictionary<int, PartData> PartRenderData => ObjectCacheUtil.GetOrCreate(Api, ToolsmithConstants.HandleRenderingDataRef, () => new Dictionary<int, PartData>());
        private ICoreClientAPI Capi;
        private ICoreAPI Api;

        public CollectibleBehaviorToolHandle(CollectibleObject collObj) : base(collObj) {

        }

        public override void OnLoaded(ICoreAPI api) {
            base.OnLoaded(api);

            Api = api;
            Capi = api as ICoreClientAPI;
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
                    var woodtypeTextPath = ToolsmithConstants.DebarkedWoodPathMinusType + woodtype;
                    outputSlot.Itemstack.SetHandleShaftTextureString(woodtypeTextPath);
                    outputSlot.Itemstack.SetPartCurrentDurability(1000);
                    outputSlot.Itemstack.SetPartMaxDurability(1000);
                }
            } else if (handleSlot != null && gripOrTreatmentSlot != null) {
                outputSlot.Itemstack.Attributes = handleSlot.Itemstack.Attributes.Clone();

                if (ToolsmithModSystem.Config.GripRegistry.ContainsKey(gripOrTreatmentSlot.Itemstack.Collectible.Code.Path)) {
                    if (handleSlot.Itemstack.HasHandleGripTag()) {
                        outputSlot.Itemstack = null;
                    } else {
                        var grip = gripOrTreatmentSlot.Itemstack;
                        var gripStats = ToolsmithModSystem.Stats.grips[ToolsmithModSystem.Config.GripRegistry[grip.Collectible.Code.Path].gripStatTag];
                        outputSlot.Itemstack.SetHandleGripTag(gripStats.id);
                        outputSlot.Itemstack.SetGripTextPath(gripStats.texturePath);
                        var handleStatPair = ToolsmithModSystem.Config.BaseHandleRegistry[handleSlot.Itemstack.Collectible.Code.Path];
                        outputSlot.Itemstack.SetModularPartShape(handleStatPair.gripShapePath);
                    }
                } else {
                    if (handleSlot.Itemstack.HasHandleGripTag() || handleSlot.Itemstack.HasHandleTreatmentTag()) {
                        outputSlot.Itemstack = null;
                    } else {
                        var treatment = gripOrTreatmentSlot.Itemstack;
                        var treatmentStatPair = ToolsmithModSystem.Config.TreatmentRegistry[treatment.Collectible.Code.Path];
                        var treatmentStats = ToolsmithModSystem.Stats.treatments[treatmentStatPair.treatmentStatTag];
                        var handleStatPair = ToolsmithModSystem.Config.BaseHandleRegistry[handleSlot.Itemstack.Collectible.Code.Path];

                        if (ToolsmithModSystem.Api.Side.IsServer()) {
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
                        }

                        if (treatmentStatPair.isLiquid && (treatment as ILiquidInterface) != null) {
                            treatment = (treatment as ILiquidInterface).GetContent(treatment);
                            outputSlot.Itemstack.SetTreatmentTextPath(ToolsmithConstants.DarkTreatementOverlayPath);
                        } else if (!treatmentStatPair.isLiquid) {
                            outputSlot.Itemstack.SetTreatmentTextPath(ToolsmithConstants.LightTreatementOverlayPath);
                        } else {
                            ToolsmithModSystem.Logger.Error("A treatment config is improperly set! This treatment - " + treatment.Collectible.Code + " - was marked as a liquid, but could not find a liquid container in this recipe. Will treat it as a non-liquid, but is worth fixing that!");
                            outputSlot.Itemstack.SetTreatmentTextPath(ToolsmithConstants.LightTreatementOverlayPath);
                        }
                        
                        outputSlot.Itemstack.SetHandleTreatmentTag(treatmentStats.id);
                    }
                }
            } else if (handleSlot != null) {
                outputSlot.Itemstack.Attributes = handleSlot.Itemstack.Attributes.Clone();
            }

            outputSlot.MarkDirty();
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo) {
            
            if (itemstack.Collectible.HasBehavior<ModularPartRenderingFromAttributes>()) {
                var modularRender = itemstack.Collectible.GetBehavior<ModularPartRenderingFromAttributes>();
                int meshrefID = itemstack.TempAttributes.GetInt(ToolsmithAttributes.ToolsmithMeshID);
                if (meshrefID == 0) { //If there is no meshrefID in temp attributes, it has likely been updated since the last render tick. Instead of just letting it run it's course, lets just regenerate an override PartData to send.
                    PartData partData = new PartData();
                    partData.partAttribute = ToolsmithAttributes.ToolHandle;

                    if (itemstack.HasHandleShaftTexture()) {
                        var handleTextureData = new TextureData {
                            code = "shaft",
                            attribute = ToolsmithAttributes.ShaftWoodTypeAttribute,
                            values = Array.Empty<string>().Append(itemstack.GetHandleShaftTextureString())
                        };
                        partData.textures.Append(handleTextureData);
                    }

                    if (itemstack.HasHandleGripTag()) {
                        var handleStatPair = ToolsmithModSystem.Config.BaseHandleRegistry[itemstack.Collectible.Code.Path];
                        partData.shapePath = handleStatPair.gripShapePath;
                        var gripTextureData = new TextureData {
                            code = "grip",
                            attribute = ToolsmithAttributes.GripTexture,
                            values = Array.Empty<string>().Append(itemstack.GetGripTextPath())
                        };
                        partData.textures.Append(gripTextureData);
                    } else {
                        partData.shapePath = itemstack.Item.Shape.Base;
                        if (itemstack.HasWetTreatment()) {
                            var treatmentStats = ToolsmithModSystem.Config.TreatmentRegistry.First(t => t.Value.treatmentStatTag == itemstack.GetHandleTreatmentTag());
                            var treatmentTextureData = new TextureData {
                                code = "shaft",
                                attribute = ToolsmithAttributes.TreatmentOverlay,
                                overlay = true,
                                values = Array.Empty<string>()
                            };
                            if (treatmentStats.Value.isLiquid) {
                                treatmentTextureData.values.Append(ToolsmithConstants.DarkTreatementOverlayPath);
                            } else {
                                treatmentTextureData.values.Append(ToolsmithConstants.LightTreatementOverlayPath);
                            }
                            partData.textures.Append(treatmentTextureData);
                        }
                    }
                    renderinfo.ModelRef = modularRender.GetMeshRef(capi, itemstack, target, ref renderinfo, partData);
                }
            }

            return;
        }
    }
}
