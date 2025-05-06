using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace Toolsmith.Utils {
    public static class MathUtility {

        //This curve is from plotting some points that felt like a decent curve, then finding the best fit line between them. Used to find if a tool's head should be damaged from a 5% chance at 98% sharpness down to 100% at 33% sharpness.
        public static bool ShouldChanceForDefectCurve(IWorldAccessor world, float howSharpPercent, int maxSharp) {
            bool shouldDefect = false;

            //Thanks Noland for helping work all this out! More notes left in my notepad document as well for Drawbacks in general
            //Can tweak the /10 bit to increase or decrease the total chance. Make the 10 larger to make the total chance higher.
            double chanceToDefect = 0.001;
            if (howSharpPercent >= 0.2) {
                chanceToDefect = 0.00277 * Math.Pow((howSharpPercent - 0.8), 2);// Old curve! (11.0813 * (Math.Pow(howSharpPercent, 4))) - (32.4235 * (Math.Pow(howSharpPercent, 3))) + (36.4908 * (Math.Pow(howSharpPercent, 2))) - (19.6091 * howSharpPercent) + 4.52141;

                if (chanceToDefect >= 1.0) { //Clamp it just in case, though it shouldn't ever hit this _probably_
                    return true;
                } else if (chanceToDefect <= 0) {
                    chanceToDefect = 0.0;
                }
            }


            var rand = world.Rand.NextDouble();
            shouldDefect = (rand <= chanceToDefect);

            return shouldDefect;
        }

        public static double GetLinearDamageMult(float totalSharpened) {
            return (double)((-5 * totalSharpened) + 3);
        }

        //A linear change from 40% sharpened giving 100% chance to damage, down to a 5% chance at 50% Sharp! 
        public static bool ShouldDamageFromSharpening(IWorldAccessor world, float totalSharpnessHoned) {
            bool shouldDamage = false;
            //Once again thanks Wolfram Alpha! Figured I'd just make this one Linear cause it really isn't that meaningful to have a curve here.
            var chanceToDamage = (-9.5 * totalSharpnessHoned) + 4.8;

            if (chanceToDamage >= 1.0) { //Just in case clamp it!
                return true;
            } else if (chanceToDamage <= 0.05) {
                chanceToDamage = 0.05;
            }

            shouldDamage = (world.Rand.NextDouble() <= chanceToDamage);

            return shouldDamage;
        }

        public static float FloorToNearestMult(float secondsUsed, float mult) {
            return MathF.Floor(secondsUsed / mult) * mult;
        }

        public static int NumberOfVoxelsLeftInReforge(float damagePercent, int voxelCount) {
            var percentVoxelRemain = (0.377778f * damagePercent) + 0.66f;

            if (percentVoxelRemain >= 1.0) { //Clamp just in case!
                percentVoxelRemain = 1.0f;
            } else if (percentVoxelRemain <= 0.66) {
                percentVoxelRemain = 0.66f;
            }

            return (int)(percentVoxelRemain * voxelCount);
        }
    }
}
