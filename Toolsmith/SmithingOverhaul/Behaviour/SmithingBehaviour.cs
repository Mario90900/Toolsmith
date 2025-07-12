using Toolsmith.SmithingOverhaul.Item;
using Toolsmith.SmithingOverhaul.Property;
using Toolsmith.SmithingOverhaul.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Toolsmith.SmithingOverhaul.Behaviour
{
    public abstract class SmithingBehavior : CollectibleBehavior
    {
        public SmithingPropertyVariant metalProps;

        public SmithingBehavior(SmithingWorkItem smithObj) : base(smithObj)
        {
            this.metalProps = smithObj.smithProps;
        }
        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
        }
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }

        public virtual void OnTemperatureEffect(StressStrainHandler ssh, ItemStack stack, IWorldAccessor world, float temperature, double hourDiff, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return;
        }
        public virtual void OnCoolingEffect(StressStrainHandler ssh, ItemStack stack, IWorldAccessor world, float tempDiff, double hourDiff, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return;
        }
        public virtual void OnHeatingEffect(StressStrainHandler ssh, ItemStack stack, IWorldAccessor world, float tempDiff, double hourDiff, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return;
        }
        public virtual bool OnCanWork(StressStrainHandler ssh, ItemStack stack, IWorldAccessor world, ref EnumHandling handling) 
        {
            handling = EnumHandling.PassThrough;
            return true;
        }

        public virtual void AfterOnHit(StressStrainHandler ssh, ItemStack stack, IWorldAccessor world, int voxelsChanged, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return;
        }

        public virtual void AfterOnUpset(StressStrainHandler ssh, ItemStack stack, IWorldAccessor world, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return;
        }

        public virtual void AfterOnSplit(StressStrainHandler ssh, ItemStack stack, IWorldAccessor world, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return;
        }

        public virtual void OnAddStrain(StressStrainHandler ssh, ItemStack stack, IWorldAccessor world, float changeInStrain, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return;
        }

        public virtual void OnRecoverStrain(StressStrainHandler ssh, ItemStack stack, IWorldAccessor world, float temperature, double hourDiff, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            return;
        }

    }
}
