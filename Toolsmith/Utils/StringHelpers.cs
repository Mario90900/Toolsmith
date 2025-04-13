using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Toolsmith.Utils {
    public static class StringHelpers {

        //Pass this the starting and ending index values you want it to set to the proper location in the tooltip where the Vanilla Durability Line is located.
        //Can technically start partway through the tooltip if needed by setting them to a value ahead of time, but general use is with them starting at 0 to start at the top of the tooltip.
        public static void FindTooltipVanillaDurabilityLine(ref int startIndex, ref int endIndex, StringBuilder tooltip, IWorldAccessor world, bool withDebugInfo) {
            bool debugFlag = false; //True after the Attribute line has been found
            bool foundLine = false;
            while (endIndex < tooltip.Length && foundLine == false) { //Find and trim off the original 'Durability' information, and then...
                if (tooltip[endIndex] == '\n') {
                    startIndex = endIndex + 1;
                }
                if (!withDebugInfo && tooltip[endIndex] == 'D') { //I don't know if this will work for any languages other then english? And I'm worried to find out, haha.
                    foundLine = true;
                }
                if (withDebugInfo && debugFlag && tooltip[endIndex] == 'D') { //This whole bit is specifically searching for the English translated code... So this might cause issues in other languages. Oof.
                    if (startIndex == endIndex) {
                        foundLine = true;
                    }
                }
                if (withDebugInfo && !debugFlag && (((world.Api.Side == EnumAppSide.Client && (world.Api as ICoreClientAPI).Input.KeyboardKeyStateRaw[1]) && tooltip[endIndex] == 'A') || (tooltip[endIndex] == 'C'))) {
                    startIndex = endIndex;
                    debugFlag = true;
                }
                endIndex++;
            }
            while (endIndex < tooltip.Length && tooltip[endIndex] != '\n') {
                endIndex++;
            }
        }
    }
}
