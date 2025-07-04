using SmithingOverhaul.Behaviour;
using SmithingOverhaul.Property;
using System;
using System.Linq;
using System.Text;
using Toolsmith.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using static HarmonyLib.Code;

namespace SmithingOverhaul.Item
{

    public class SmithingWorkItem : ItemWorkItem
    {
        
        public SmithingBehavior[] SmithingBehaviors = new SmithingBehavior[0];
        private SmithingPropertyVariant smithProps = null;

        public bool isOverstrained = false;
        public override void OnLoaded(ICoreAPI api)
        {
            SmithingPropertyVariant var;
            if (api.ModLoader.GetModSystem<SmithingOverhaulModSystem>().metalPropsByCode.TryGetValue(Variant["metal"], out var))
            {
                smithProps = var;
            }

            base.OnLoaded(api);
        }
        public override float GetTemperature(IWorldAccessor world, ItemStack itemstack)
        {
            bool preventDefault = false;
            float temperature = 20f;

            foreach (SmithingBehavior behavior in SmithingBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;
                float temperatureBh = behavior.GetTemperature(world, itemstack, ref handled);
                if (handled != EnumHandling.PassThrough)
                {
                    temperature = temperatureBh;
                    preventDefault = true;
                }

                if (handled == EnumHandling.PreventSubsequent) return temperature;
            }

            if (preventDefault) return temperature;

            //Default Behaviour 

            if (itemstack?.Attributes?["temperature"] is not ITreeAttribute)
            {
                return 20;
            }

            ITreeAttribute attr = (ITreeAttribute)itemstack.Attributes["temperature"];

            double nowHours = world.Calendar.TotalHours;
            double lastUpdateHours = attr.GetDecimal("temperatureLastUpdate");

            double hourDiff = nowHours - lastUpdateHours;
            float temp = (float)attr.GetDecimal("temperature", 20);

            if (itemstack.Attributes.GetBool("timeFrozen")) return temp;

            // 1.5 deg per irl second
            // 1 game hour = irl 60 seconds
            if (hourDiff > 1 / 85f && temp > 0f)
            {
                RecoverStrain(temp, hourDiff, itemstack);

                temp = Math.Max(0, temp - Math.Max(0, (float)(nowHours - lastUpdateHours) * attr.GetFloat("cooldownSpeed", 90)));
                
                attr.SetFloat("temperature", temp);
                attr.SetDouble("temperatureLastUpdate", nowHours);
            }

            return temp;

        }
        public override float GetTemperature(IWorldAccessor world, ItemStack itemstack, double didReceiveHeat)
        {
            bool preventDefault = false;
            float temperature = 20f;

            foreach (SmithingBehavior behavior in SmithingBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;
                float temperatureBh = behavior.GetTemperature(world, itemstack, didReceiveHeat, ref handled);
                if (handled != EnumHandling.PassThrough)
                {
                    temperature = temperatureBh;
                    preventDefault = true;
                }

                if (handled == EnumHandling.PreventSubsequent) return temperature;
            }

            if (preventDefault) return temperature;

            //Default Behaviour

            if (itemstack?.Attributes?["temperature"] is not ITreeAttribute)
            {
                return 20;
            }

            var attr = (ITreeAttribute)itemstack.Attributes["temperature"];

            var nowHours = world.Calendar.TotalHours;
            var lastUpdateHours = attr.GetDouble("temperatureLastUpdate");

            var hourDiff = nowHours - (lastUpdateHours + didReceiveHeat);

            var temp = attr.GetFloat("temperature", 20);

            // 1.5 deg per irl second
            // 1 game hour = irl 60 seconds
            if (hourDiff > 1 / 85f && temp > 0f)
            {
                RecoverStrain(temp, hourDiff, itemstack);

                temp = Math.Max(0, temp - Math.Max(0, (float)(nowHours - lastUpdateHours) * attr.GetFloat("cooldownSpeed", 90)));
                attr.SetFloat("temperature", temp);
            }
            attr.SetDouble("temperatureLastUpdate", nowHours);

            return temp;
        }
        public override void SetTemperature(IWorldAccessor world, ItemStack itemstack, float temperature, bool delayCooldown = true)
        {
            bool preventDefault = false;

            foreach (SmithingBehavior behavior in SmithingBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;

                behavior.SetTemperature(world, itemstack, temperature, delayCooldown, ref handled);

                if (handled != EnumHandling.PassThrough) preventDefault = true;

                if (handled == EnumHandling.PreventSubsequent) return;
            }

            if (preventDefault) return;

            //Default Behaviour 

            base.SetTemperature(world, itemstack, temperature, delayCooldown); return;
        }
        public new bool CanWork(ItemStack stack)
        {
            bool preventDefault = false;
            bool canWork = false;

            foreach (SmithingBehavior behavior in SmithingBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;
                bool canWorkBh = behavior.CanWork(api.World, stack, ref handled);
                if (handled != EnumHandling.PassThrough)
                {
                    canWork = canWorkBh;
                    preventDefault = true;
                }

                if (handled == EnumHandling.PreventSubsequent) return canWork;
            }

            if (preventDefault) return canWork;

            //Default Behaviour

            float temperature = stack.Collectible.GetTemperature(api.World, stack);
            float workTemp = 0;

            if (smithProps != null)
            {
                workTemp = smithProps.WarmForgingTemp;
            }

            if (stack.Collectible.Attributes?["workableTemperature"].Exists == true)
            {
                return stack.Collectible.Attributes["workableTemperature"].AsFloat(workTemp) <= temperature;
            }

            return temperature >= workTemp;
        }
        public virtual float AddStrain(float changeInStrain, ItemStack stack)
        {
            bool preventDefault = false;
            float strain = -1f;

            foreach (SmithingBehavior behavior in SmithingBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;
                float strainBh = behavior.AddStrain(changeInStrain, stack, ref handled);
                if (handled != EnumHandling.PassThrough)
                {
                    strain = strainBh;
                    preventDefault = true;
                }

                if (handled == EnumHandling.PreventSubsequent) return strain;
            }

            if (preventDefault) return strain;

            //Default Behaviour

            strain = stack.Attributes.GetFloat("plasticStrain");

            strain += changeInStrain;

            SmithingUtils.SetStrain(smithProps, stack, strain);

            return strain;
        }
        public virtual void RecoverStrain(float temperature, double hourDiff, ItemStack stack)
        {
            bool preventDefault = false;

            foreach (SmithingBehavior behavior in SmithingBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;

                behavior.RecoverStrain(temperature, stack, ref handled);

                if (handled != EnumHandling.PassThrough) preventDefault = true;

                if (handled == EnumHandling.PreventSubsequent) return;
            }

            if (preventDefault) return;

            //Default Behaviour

            float strain = 0f;

            strain = stack.Attributes.GetFloat("plasticStrain");

            float strain_recovered = SmithingUtils.GetRecrystalization(smithProps, stack, temperature, strain, hourDiff);

            strain -= strain_recovered;

            SmithingUtils.SetStrain(smithProps, stack, strain);
        }
        public virtual void AfterOnHit(int voxelsChanged, ItemStack stack)
        {
            bool preventDefault = false;

            foreach (SmithingBehavior behavior in SmithingBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;

                behavior.AfterOnHit(voxelsChanged, stack, ref handled);

                if (handled != EnumHandling.PassThrough) preventDefault = true;

                if (handled == EnumHandling.PreventSubsequent) return;
            }

            if (preventDefault) return;

            //Default Behaviour

            float strainChange = (voxelsChanged == 0 ? 1 : voxelsChanged) * SmithingUtils.STRAINMULT;
            AddStrain(strainChange, stack);
            return;
        }
        public virtual void AfterOnUpset(ItemStack stack)
        {
            bool preventDefault = false;

            foreach (SmithingBehavior behavior in SmithingBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;

                behavior.AfterOnUpset(stack, ref handled);

                if (handled != EnumHandling.PassThrough) preventDefault = true;

                if (handled == EnumHandling.PreventSubsequent) return;
            }

            if (preventDefault) return;

            //Default Behaviour

            float strainChange = 1 * SmithingUtils.STRAINMULT;
            AddStrain(strainChange, stack);
            return;
        }
        public virtual void AfterOnSplit(ItemStack stack)
        {
            bool preventDefault = false;

            foreach (SmithingBehavior behavior in SmithingBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;

                behavior.AfterOnSplit(stack, ref handled);

                if (handled != EnumHandling.PassThrough) preventDefault = true;

                if (handled == EnumHandling.PreventSubsequent) return;
            }

            if (preventDefault) return;

            //Default Behaviour

            float strainChange = 1 * SmithingUtils.STRAINMULT;
            AddStrain(strainChange, stack);
        }

        public virtual bool IsOverstrained(ItemStack stack)
        {
            bool preventDefault = false;
            bool overtsrained = false;

            foreach (SmithingBehavior behavior in SmithingBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;

                bool overstrainedBh = behavior.IsOverstrained(stack, ref handled);

                if (handled != EnumHandling.PassThrough)
                {
                    overtsrained = overstrainedBh;
                    preventDefault = true;
                }
                if (handled == EnumHandling.PreventSubsequent) return overtsrained;
            }

            if (preventDefault) return overtsrained;

            //Default Behaviour

            bool? result = stack.Attributes.TryGetBool("isOverstrained");
            if (result == null)
            {
                stack.Attributes.SetBool("isOverstrained", false);
                return false;
            }
            else return (bool)result;
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            float strain = inSlot.Itemstack.Attributes.GetFloat("plasticStrain");
            float strainprcnt = 0;

            if (smithProps != null)
            {
                strainprcnt = strain / smithProps.Elongation * 100;
            }

            if (inSlot.Itemstack.ItemAttributes?["smithingProperties"].Exists == true)
            {
                JsonObject props = inSlot.Itemstack.ItemAttributes["smithingProperties"];
                if (props.KeyExists("elongation"))
                {
                    strainprcnt = strain / props["elongation"].AsFloat() * 100;
                }
            }

            dsc.AppendLine(Lang.Get("Metal Strain: {0} %", strainprcnt));
            return;
        }
    }
}
