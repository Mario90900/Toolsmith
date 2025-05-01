using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Toolsmith.ToolTinkering.Blocks {
    public class BlockEntityGrindstone : BlockEntity {

        public bool grinding { get; set; }
        protected ILoadedSound grindingWheel;
        protected ILoadedSound honingScrape;
        string rotation;

        public virtual string Rotation {
            get { return rotation; }
            set {
                rotation = value;
                switch (value) {
                    case "north":
                        rotationVec.Y = 0;
                        break;
                    case "east":
                        rotationVec.Y = 270;
                        break;
                    case "west":
                        rotationVec.Y = 90;
                        break;
                    default:
                        rotationVec.Y = 180;
                        break;
                }
            }
        }
        public Vec3f rotationVec = new Vec3f();
        private bool isClient = false;

        BlockEntityAnimationUtil AnimUtil {
            get {
                return GetBehavior<BEBehaviorAnimatable>()?.animUtil;
            }
        }

        public override void Initialize(ICoreAPI api) {
            base.Initialize(api);

            isClient = api.Side == EnumAppSide.Client;
            Rotation = api.World.BlockAccessor.GetBlock(Pos).LastCodePart();

            if (isClient) {
                AnimUtil.InitializeAnimator("grindstone", null, null, rotationVec);
            }
            if (isClient && grinding) {
                OnBlockInteractStart();
            }
        }

        public void OnBlockInteractStart() {
            if (!isClient) {
                return;
            }

            AnimUtil?.StartAnimation(new AnimationMetaData() { Animation = "spinwheel", Code = "spinwheel", EaseInSpeed = 10, EaseOutSpeed = 2 });
            grinding = true;
            ToggleWheelSound(true);
            MarkDirty();
        }

        public void OnBlockInteractStop() {
            AnimUtil?.StopAnimation("spinwheel");
            grinding = false;
            ToggleWheelSound(false);
            MarkDirty();
        }

        //Modeled after the Rift Ward's sounds, and toggled on when starting interacting with the grindstone and off when canceled or stopped.
        private void ToggleWheelSound(bool startSound) {
            if (startSound) {
                if (grindingWheel == null || !grindingWheel.IsPlaying) {
                    grindingWheel = ((IClientWorldAccessor)Api.World).LoadSound(new SoundParams() {
                        Location = new AssetLocation("sounds/block/quern.ogg"),
                        ShouldLoop = true,
                        Position = Pos.ToVec3f().Add(0.5f, 0.5f, 0.5f),
                        DisposeOnFinish = false,
                        Volume = 0,
                        Range = 6,
                        SoundType = EnumSoundType.Ambient
                    });

                    if (grindingWheel != null) {
                        grindingWheel.Start();
                        grindingWheel.FadeTo(1.0, 1f, (s) => { });
                    }
                } else {
                    if (grindingWheel.IsPlaying) {
                        grindingWheel.FadeTo(1.0, 1f, (s) => { });
                    }
                }
            } else {
                grindingWheel?.FadeOut(1.0f, (s) => { s.Dispose(); grindingWheel = null; });
            }
        }

        public void ToggleHoningSound(bool startSound) {
            if (startSound) {
                if (!isClient) {
                    return;
                }
                if (honingScrape == null || !honingScrape.IsPlaying) {
                    honingScrape = ((IClientWorldAccessor)Api.World).LoadSound(new SoundParams() {
                        Location = new AssetLocation("toolsmith:sounds/grindstone-scraping-loop.ogg"),
                        ShouldLoop = true,
                        Position = Pos.ToVec3f().Add(0.5f, 0.5f, 0.5f),
                        DisposeOnFinish = false,
                        Volume = 0,
                        Range = 6,
                        SoundType = EnumSoundType.Ambient
                    });

                    if (honingScrape != null) {
                        honingScrape.Start();
                        honingScrape.FadeTo(0.75, 1f, (s) => { });
                    }
                } else {
                    if (honingScrape.IsPlaying) {
                        honingScrape.FadeTo(0.75, 1f, (s) => { });
                    }
                }
            } else {
                honingScrape?.FadeOut(0.2f, (s) => { s.Dispose(); honingScrape = null; });
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve) {
            base.FromTreeAttributes(tree, worldAccessForResolve);

            grinding = tree.GetBool("grinding");
            Rotation = tree.GetString("rotation", Rotation);

            if (grinding) {
                OnBlockInteractStart();
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree) {
            base.ToTreeAttributes(tree);

            tree.SetBool("grinding", grinding);
            tree.SetString("rotation", Rotation);
        }
    }
}
