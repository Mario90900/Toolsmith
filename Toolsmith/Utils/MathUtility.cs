using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace Toolsmith.Utils {
    public static class MathUtility {

        //This curve is from plotting some points that felt like a decent curve, then finding the best fit line between them. Used to find if a tool's head should be damaged from a 5% chance at 98% sharpness down to 100% at 33% sharpness.
        public static bool ShouldDamageHeadFromCurveChance(IWorldAccessor world, float howSharpPercent) {
            bool shouldDamage = false;
            //Thank you Wolfram Alpha for the formula
            var chanceToDamage = (11.0813 * (Math.Pow(howSharpPercent, 4))) - (32.4235 * (Math.Pow(howSharpPercent, 3))) + (36.4908 * (Math.Pow(howSharpPercent, 2))) - (19.6091 * howSharpPercent) + 4.52141;
            
            if (chanceToDamage >= 1.0) { //Clamp it just in case.
                return true;
            } else if (chanceToDamage <= 0.05) {
                chanceToDamage = 0.05;
            }

            var rand = world.Rand.NextDouble();
            shouldDamage = (rand <= chanceToDamage);

            return shouldDamage;
        }

        //A linear change from 33% sharpened giving 100% chance to damage, down to a 5% chance at 66% 
        public static bool ShouldDamageFromSharpening(IWorldAccessor world, float totalSharpnessHoned) {
            bool shouldDamage = false;
            //Once again thanks Wolfram Alpha! Figured I'd just make this one Linear cause it really isn't that meaningful to have a curve here.
            var chanceToDamage = 1.95 - (2.87879 * totalSharpnessHoned);

            if (chanceToDamage >= 1.0) { //Just in case clamp it!
                return true;
            } else if (chanceToDamage <= 0.05) {
                chanceToDamage = 0.05;
            }

            shouldDamage = (world.Rand.NextDouble() <= chanceToDamage);

            return shouldDamage;
        }
    }
}
