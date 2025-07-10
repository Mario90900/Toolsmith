using MathNet.Numerics.Statistics.Mcmc;
using SmithingOverhaul.Behaviour;
using SmithingOverhaul.Property;
using System;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using Toolsmith.SmithingOverhaul.Utils;
using Toolsmith.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using static HarmonyLib.Code;

namespace SmithingOverhaul.Item
{
    public class SmithingWorkItem : ItemWorkItem
    {
        public static int nextHandlerRefId = 0;
        public SmithingBehavior[] SmithingBehaviors = new SmithingBehavior[0];
        public SmithingPropertyVariant smithProps = null;
        public override void OnLoaded(ICoreAPI api)
        {
            SmithingPropertyVariant var;
            if (api.ModLoader.GetModSystem<SmithingOverhaulModSystem>().metalPropsByCode.TryGetValue(Variant["metal"], out var))
            {
                smithProps = var;
            }

            foreach (SmithingBehavior behavior in SmithingBehaviors)
            {
                behavior.OnLoaded(api);
            }

            base.OnLoaded(api);
        }
        public override void OnUnloaded(ICoreAPI api)
        {
            foreach (SmithingBehavior behavior in SmithingBehaviors)
            {
                behavior.OnUnloaded(api);
            }

            base.OnUnloaded(api);
        }
        public override float GetTemperature(IWorldAccessor world, ItemStack itemstack)
        {
            //Default Behaviour 

            if (itemstack?.Attributes?["temperature"] is not ITreeAttribute)
            {
                return 20;
            }

            ITreeAttribute attr = itemstack.Attributes.GetOrAddTreeAttribute("temperature");

            double nowHours = world.Calendar.TotalHours;
            double lastUpdateHours = attr.GetDecimal("temperatureLastUpdate", nowHours);

            double hourDiff = nowHours - lastUpdateHours;
            float temp = (float)attr.GetFloat("temperature", 20);
            if (itemstack.Attributes.GetBool("timeFrozen", false)) return temp;

            // 1.5 deg per irl second
            // 1 game hour = irl 60 seconds
            if (hourDiff > 1 / 85f && temp > 0f)
            {
                TemperatureEffect(itemstack, temp, hourDiff);

                float cooledTemp = (float)(nowHours - lastUpdateHours) * attr.GetFloat("cooldownSpeed", 90);
                temp = Math.Max(0, temp - Math.Max(0, cooledTemp));
            }
            SetTemperature(world, itemstack, temp, false);
            return temp;

        }
        public override float GetTemperature(IWorldAccessor world, ItemStack itemstack, double didReceiveHeat)
        {
            //Default Behaviour

            if (itemstack?.Attributes?["temperature"] is not ITreeAttribute)
            {
                return 20;
            }

            var attr = itemstack.Attributes.GetOrAddTreeAttribute("temperature");

            var nowHours = world.Calendar.TotalHours;
            var lastUpdateHours = attr.GetDouble("temperatureLastUpdate");

            var hourDiff = nowHours - (lastUpdateHours + didReceiveHeat);

            var temp = attr.GetFloat("temperature", 20);
            // 1.5 deg per irl second
            // 1 game hour = irl 60 seconds
            if (hourDiff > 1 / 85f && temp > 0f)
            {
                TemperatureEffect(itemstack, temp, hourDiff);

                float cooledTemp = (float)(nowHours - lastUpdateHours) * attr.GetFloat("cooldownSpeed", 90);
                temp = Math.Max(0, temp - Math.Max(0, cooledTemp));

                
            }
            SetTemperature(world, itemstack, temp, false);
            return temp;
        }
        public override void SetTemperature(IWorldAccessor world, ItemStack itemstack, float temperature, bool delayCooldown = true)
        {
            //Default Behaviour 

            if (itemstack == null) return;

            ITreeAttribute attr = itemstack.Attributes.GetOrAddTreeAttribute("temperature");

            double nowHours = world.Calendar.TotalHours;
            double initialHours = attr.GetDouble("temperatureLastUpdate", nowHours);
            float initialTemp = attr.GetFloat("temperature", temperature);
            // If the colletible gets heated, retain the heat for 1 ingame hour
            if (initialTemp < temperature)
            {
                if (delayCooldown) nowHours += 0.5f;
                HeatingEffects(itemstack, temperature - initialTemp, nowHours - initialHours);
            }
            else if (initialTemp > temperature) CoolingEffects(itemstack, initialTemp - temperature, nowHours - initialHours);

            attr.SetDouble("temperatureLastUpdate", nowHours);
            attr.SetFloat("temperature", temperature);
        }

        //Handles effects related to being a certain temperature
        public virtual void TemperatureEffect(ItemStack stack, float temperature, double hourDiff)
        {
            bool preventDefault = false;
            StressStrainHandler ssh = stack.GetStressStrainHandler(api);
            if (ssh == null) return;

            foreach (SmithingBehavior behavior in SmithingBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;

                behavior.OnTemperatureEffect(ssh, stack, api.World, temperature, hourDiff, ref handled);

                if (handled != EnumHandling.PassThrough) preventDefault = true;

                if (handled == EnumHandling.PreventSubsequent) return;
            }

            if (preventDefault) return;

            //Default Behaviour

            ssh.RecoverStrain(stack, temperature, hourDiff);
        }

        //Handles effects resulting from cooling a piece
        public virtual void CoolingEffects(ItemStack stack, float tempDiff, double hourDiff)
        {
            bool preventDefault = false;
            StressStrainHandler ssh = stack.GetStressStrainHandler(api);
            if (ssh == null) return;

            foreach (SmithingBehavior behavior in SmithingBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;

                behavior.OnCoolingEffect(ssh, stack, api.World, tempDiff, hourDiff, ref handled);

                if (handled != EnumHandling.PassThrough) preventDefault = true;

                if (handled == EnumHandling.PreventSubsequent) return;
            }

            if (preventDefault) return;
        }

        //Handles effects resulting from heating a piece
        public virtual void HeatingEffects(ItemStack stack, float tempDiff, double hourDiff)
        {
            bool preventDefault = false;
            StressStrainHandler ssh = stack.GetStressStrainHandler(api);
            if (ssh == null) return;

            foreach (SmithingBehavior behavior in SmithingBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;

                behavior.OnHeatingEffect(ssh, stack, api.World, tempDiff, hourDiff, ref handled);

                if (handled != EnumHandling.PassThrough) preventDefault = true;

                if (handled == EnumHandling.PreventSubsequent) return;
            }

            if (preventDefault) return;

        }
        public virtual void AfterOnHit(int voxelsChanged, ItemStack stack)
        {
            bool preventDefault = false;
            StressStrainHandler ssh = stack.GetStressStrainHandler(api);
            if (ssh == null) return;
            foreach (SmithingBehavior behavior in SmithingBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;

                behavior.AfterOnHit(ssh, stack, api.World, voxelsChanged, ref handled);

                if (handled != EnumHandling.PassThrough) preventDefault = true;

                if (handled == EnumHandling.PreventSubsequent) return;
            }

            if (preventDefault) return;

            //Default Behaviour

            float strainChange = (voxelsChanged == 0 ? 1 : voxelsChanged) * SmithingUtils.STRAINMULT;
            ssh.AddStrain(strainChange);
            return;
        }
        public virtual void AfterOnUpset(ItemStack stack)
        {
            bool preventDefault = false;
            StressStrainHandler ssh = stack.GetStressStrainHandler(api);
            if (ssh == null) return;

            foreach (SmithingBehavior behavior in SmithingBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;

                behavior.AfterOnUpset(ssh, stack, api.World, ref handled);

                if (handled != EnumHandling.PassThrough) preventDefault = true;

                if (handled == EnumHandling.PreventSubsequent) return;
            }

            if (preventDefault) return;

            //Default Behaviour

            float strainChange = 1 * SmithingUtils.STRAINMULT;
            ssh.AddStrain(strainChange);
            return;
        }
        public virtual void AfterOnSplit(ItemStack stack)
        {
            bool preventDefault = false;
            StressStrainHandler ssh = stack.GetStressStrainHandler(api);
            if (ssh == null) return;

            foreach (SmithingBehavior behavior in SmithingBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;

                behavior.AfterOnSplit(ssh, stack, api.World, ref handled);

                if (handled != EnumHandling.PassThrough) preventDefault = true;

                if (handled == EnumHandling.PreventSubsequent) return;
            }

            if (preventDefault) return;

            //Default Behaviour

            float strainChange = 1 * SmithingUtils.STRAINMULT;
            ssh.AddStrain(strainChange);
        }

        public virtual bool IsOverstrained(ItemStack stack)
        {
            StressStrainHandler ssh = stack.GetStressStrainHandler(api);
            if (ssh == null) return false;
            return ssh.IsOverstrained;
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            StressStrainHandler ssh = inSlot.Itemstack.GetStressStrainHandler(api);
            if (ssh == null) return;

            dsc.AppendLine(Lang.Get("Metal Strain: {0} %", ssh.PlasticStrainPrct));
            return;
        }
    }
}
