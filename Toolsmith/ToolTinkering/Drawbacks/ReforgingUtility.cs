using SmithingPlus.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Toolsmith.ToolTinkering.Behaviors;
using Toolsmith.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Toolsmith.ToolTinkering.Drawbacks {
    public static class ReforgingUtility {

        //Only call this on the server side!
        public static SmithingRecipe TryGetSmithingRecipeFromCache(ItemStack stack, ICoreAPI api) { //Pass this a tool head or any smithed tool to prepare it for a reforge!
            if (!IsBelowPercentDurability(stack, ToolsmithModSystem.Config.PercentDamageForReforge)) { //If it is above the configurable threshold for Reforge, just return nothing.
                return null;
            }

            SmithingRecipe smithingRecipe;
            var reforgeTargetCode = stack.Collectible.Code;
            if (RecipeRegisterModSystem.ToolHeadSmithingRecipes.TryGetValue(reforgeTargetCode, out smithingRecipe)) { //Otherwise, it is elegible for a Reforge, check the cache first!
                return smithingRecipe;
            }

            smithingRecipe = api.ModLoader.GetModSystem<RecipeRegistrySystem>().SmithingRecipes.FirstOrDefault(r => r.Output.ResolvedItemstack.Satisfies(stack));
            RecipeRegisterModSystem.ToolHeadSmithingRecipes.Add(reforgeTargetCode, smithingRecipe);
            return smithingRecipe;
        }

        public static bool IsBelowPercentDurability(ItemStack stack, float percent) {
            float percentDamage = 1.0f;
            if (stack.Collectible.HasBehavior<CollectibleBehaviorToolHead>()) {
                percentDamage = stack.GetPartRemainingHPPercent();
            } else if (stack.Collectible.HasBehavior<CollectibleBehaviorSmithedTools>()) {
                percentDamage = stack.GetSmithedRemainingHPPercent();
            }

            if (percentDamage == 0.0f) { //If either of the percent calls return 0, it's most likely that something went wrong with the tool head and somehow it's health is negative or 0 without breaking, or it's simply unset. So assume it's full durability.
                percentDamage = 1.0f;
            }

            return (percentDamage < percent);
        }

        public static float GetReforgablePercentDamage(ItemStack stack) {
            float percentDamage = 1.0f;
            if (stack.Collectible.HasBehavior<CollectibleBehaviorToolHead>()) {
                percentDamage = stack.GetPartRemainingHPPercent();
            } else if (stack.Collectible.HasBehavior<CollectibleBehaviorSmithedTools>()) {
                percentDamage = stack.GetSmithedRemainingHPPercent();
            }

            if (percentDamage == 0.0f) {
                percentDamage = 1.0f;
            }

            return percentDamage;
        }

        public static ItemStack GetWorkItemFromMetalType(ICoreAPI api, string metal) {
            var item = api.World.GetItem(new AssetLocation(ToolsmithConstants.WorkItemCode + "-" + metal));
            if (item == null) {
                ToolsmithModSystem.Logger.Error("Could not find this workitem! " + ToolsmithConstants.WorkItemCode + "-" + metal);
                return null;
            }
            return new ItemStack(item);
        }

        public static bool[,,] GetVoxelCopyFromRecipe(SmithingRecipe recipe) {
            var sourceVoxels = recipe.Voxels;
            bool[,,] copyVoxels = new bool[sourceVoxels.GetLength(0), sourceVoxels.GetLength(1), sourceVoxels.GetLength(2)];
            for (int x = 0; x < sourceVoxels.GetLength(0); x++) {
                for (int y = 0; y < sourceVoxels.GetLength(1); y++) {
                    for (int z = 0; z < sourceVoxels.GetLength(2); z++) {
                        copyVoxels[x, y, z] = sourceVoxels[x, y, z];
                    }
                }
            }
            return copyVoxels;
        }

        public static int TotalVoxelsInRecipe(SmithingRecipe recipe) {
            var recipeVoxels = recipe.Voxels;
            return recipeVoxels.Cast<bool>().Count(voxel => voxel);
        }

        public static bool[,,] DamageWorkpieceForReforge(bool[,,] voxels, int totalVoxels, int targetVoxels, ICoreAPI api) {
            //A 3D array of bools. True is a Voxel, false is air.
            //Incrementing X determines the horizontal direction across the recipe, while incrementing Z is the vertical one. Y is what layer UP from the surface of the Anvil the whole plane is.
            //X and Z are from 0 to 15, while Y is from 0 to 5
            //The recipes appear to automatically get centered around the middle-most point on the X and Z, and starting on the lowest y. So they try to center around 8,0,8 or so
            //Most recipes also appear to be oriented with the 'edge' facing the left, but this isn't garenteed unfortunately with mods.
            //  - Plus double-headed or blunt objects. Well... Okay blunted is marked so... maybe just find edge-voxels for those? - Could add a bypass to handle blunt tools separately.
            //
            //Perhaps keep the functions working on the leftmost bits? If it lands on a bit further in, a small chance to act on that one, but it's more likely to shift towards the left and target that bit instead.
            //  - If a bit is shifted, it will be shifted rightmost for the most part, sometimes up, sometimes down, but placed on the topmost y-layer.
            //
            // To start the process, perhaps parse and find the leftmost entries in each Z-Coord Row?
            //  - Assign chance-to-target each Z-Coord with the furthest left getting the largest weight, up to 3 steps up along the X-Coord before it hits 0 until things trim away. Fuck this is going to be complex.
            //  - 60% chance for the leftmost Z, 30% for one in, 10% for two in, then just randomly pick from any valid ones in that X
            //
            // Will have to run through the voxels until the leftmost filled Z is found, and keep track of that, and count the number of valid voxels in that X-collumn
            //  - Then can roll for chances based on this for which Z-coord is targetted based on the above chances.
            //  - Roll for which voxel along the X is chosen, from the first to the last encountered voxel with even chances for all.
            //  - Then finally act on the Voxel. For a regular tool reforge, 15% chance to not effect it, 50% to remove it, 30% chance to shift it, 5% to double down and find another to act on without decrementing the voxel count.
            //  - Once there is no need to reduce voxel numbers to hit the target count, it's done and ready for reforge!
            //

            //First, find the number of voxels to remove and then locate and store the coords of the first leftmost Voxel found
            var numToEffect = totalVoxels - targetVoxels;
            (int,int,int) firstVoxel = FindLeftmostVoxel(voxels);
            (int, int, int)[] voxelsInLeftSlice = FindNumVoxelsAlongSlice(voxels, firstVoxel.Item1); //Grab the first 3 'slices' of the workpiece to iterate on before jumping into the loop, this way they won't be grabbed each time
            (int, int, int)[] voxelsInMidSlice = null;
            (int, int, int)[] voxelsInRightSlice = null;
            if (firstVoxel.Item1 + 1 < voxels.GetLength(0)) {
                voxelsInMidSlice = FindNumVoxelsAlongSlice(voxels, firstVoxel.Item1 + 1);
            }
            if (firstVoxel.Item1 + 2 < voxels.GetLength(0)) { //For the second and third slices, ensure they are valid first before attempting to grab them. In all likelyhood they _should_ be valid always? But better to be safe. I've crashed things enough already :P
                voxelsInRightSlice = FindNumVoxelsAlongSlice(voxels, firstVoxel.Item1 + 2);
            }
            if (ToolsmithModSystem.Config.DebugMessages) {
                ToolsmithModSystem.Logger.Warning("Number of Voxels to mess with: " + numToEffect);
                ToolsmithModSystem.Logger.Warning("FirstVoxel is " + firstVoxel.Item1 + ", " + firstVoxel.Item2 + ", " + firstVoxel.Item3);
                ToolsmithModSystem.Logger.Warning("There are " + voxelsInLeftSlice.Length + " voxels along the slice");
            }

            int whichSlot;
            int action;
            while (numToEffect > 0) { //Now that the staging is set up, time to start actually iterating through and acting on the voxels!
                var depth = RandomDamageDepth(api); //Choose a 'depth' to act on based on the described chances, 60% chance to act on a voxel in the leftmost slice, 30% on the middle slice, and 10% on the 'right' slice

                switch (depth) { //Depending on the return values, 0 for left, 1 for mid, 2 for right, will have to target the indidivual arrays separately.
                    case 0:
                        whichSlot = RandomPickAlongVoxelArray(api, voxelsInLeftSlice.Length);
                        if (whichSlot < 0) {
                            ToolsmithModSystem.Logger.Error("Something happened during Generation of a Workpiece. Attempting to act on the Left Slice, but it is empty. Refusing action to prevent a crash hopefully.");
                            break;
                        }

                        action = ChooseActionOnVoxel(api);
                        switch (action) {
                            case 0:
                                //Nothing is done! Woo! Good to go!
                                break;
                            case 1: //It's removing the Voxel!
                                var slot = voxelsInLeftSlice[whichSlot];
                                voxelsInLeftSlice = voxelsInLeftSlice.RemoveAt(whichSlot);
                                voxels[slot.Item1, slot.Item2, slot.Item3] = false;
                                break;
                            case 2: //It's shifting the voxel! Whoa!
                                slot = voxelsInLeftSlice[whichSlot];
                                var newSlot = slot;
                                voxelsInLeftSlice = voxelsInLeftSlice.RemoveAt(whichSlot);
                                voxels[slot.Item1, slot.Item2, slot.Item3] = false;

                                var upRightOrDown = api.World.Rand.Next(3); //0 is up and right, 1 is just right, and 2 is down and right. This voxel can still be later removed if it's picked after!
                                if (upRightOrDown == 0) {
                                    if (slot.Item3 + 1 < voxels.GetLength(2)) {
                                        newSlot.Item3++;
                                    }
                                } else if (upRightOrDown == 2) {
                                    if (slot.Item3 - 1 > 0) {
                                        newSlot.Item3--;
                                    }
                                }
                                if (newSlot.Item1 + 1 < voxels.GetLength(0)) {
                                    newSlot.Item1++;
                                }
                                newSlot.Item2 = GetFirstFreeYAt(voxels, slot.Item1 + 1, slot.Item3 + 1); //For simplicity sake, lets just toss it on the lowest y-coord that is free.

                                voxelsInMidSlice = voxelsInMidSlice.Append(newSlot);
                                voxels[newSlot.Item1, newSlot.Item2, newSlot.Item3] = true;
                                break;
                            default: //The danger zone... Remove and roll again, but it will remove or shift it.
                                slot = voxelsInLeftSlice[whichSlot];
                                voxelsInLeftSlice = voxelsInLeftSlice.RemoveAt(whichSlot);
                                voxels[slot.Item1, slot.Item2, slot.Item3] = false;

                                var doWhatElse = api.World.Rand.Next(2);
                                var adjacentSlot = PickRandomAdjacentVoxel(api, voxels, slot);
                                if (voxels[adjacentSlot.Item1, adjacentSlot.Item2, adjacentSlot.Item3] == true) {
                                    voxels[adjacentSlot.Item1, adjacentSlot.Item2, adjacentSlot.Item3] = false;
                                    TryRemoveVoxelFromArrays(adjacentSlot, ref voxelsInLeftSlice, ref voxelsInMidSlice, ref voxelsInRightSlice);
                                    if (doWhatElse == 1) { //Flip a coin, either remove a second which is handled above, or move it! If 1, we are moving the voxel.
                                        upRightOrDown = api.World.Rand.Next(3); //0 is up and right, 1 is just right, and 2 is down and right. This voxel can still be later removed if it's picked after!
                                        if (upRightOrDown == 0) {
                                            if (adjacentSlot.Item3 + 1 < voxels.GetLength(2)) {
                                                adjacentSlot.Item3++;
                                            }
                                        } else if (upRightOrDown == 2) {
                                            if (adjacentSlot.Item3 - 1 > 0) {
                                                adjacentSlot.Item3--;
                                            }
                                        }
                                        if (adjacentSlot.Item1 < voxels.GetLength(0)) {
                                            adjacentSlot.Item1++;
                                        }
                                        adjacentSlot.Item2 = GetFirstFreeYAt(voxels, slot.Item1 + 1, slot.Item3 + 1);
                                        FindAndAppendMovedVoxel(adjacentSlot, ref voxelsInLeftSlice, ref voxelsInMidSlice, ref voxelsInRightSlice);
                                        voxels[adjacentSlot.Item1, adjacentSlot.Item2, adjacentSlot.Item3] = true;
                                    }
                                }
                                break;
                        }

                        if (voxelsInLeftSlice.Length <= 0) {
                            voxelsInLeftSlice = voxelsInLeftSlice.Append(voxelsInMidSlice);
                            voxelsInMidSlice = Array.Empty<(int, int, int)>();
                            voxelsInMidSlice = voxelsInMidSlice.Append(voxelsInRightSlice);
                            var newSliceCoord = voxelsInRightSlice[0].Item1 + 1;
                            voxelsInRightSlice = Array.Empty<(int, int, int)>();
                            if (newSliceCoord < voxels.GetLength(0)) {
                                voxelsInRightSlice = FindNumVoxelsAlongSlice(voxels, newSliceCoord);
                            }
                        }

                        break;
                    case 1:
                        whichSlot = RandomPickAlongVoxelArray(api, voxelsInMidSlice.Length);
                        if (whichSlot < 0) {
                            ToolsmithModSystem.Logger.Error("Something happened during Generation of a Workpiece. Attempting to act on the Middle Slice, but it is empty. Refusing action to prevent a crash hopefully.");
                            break;
                        }

                        action = ChooseActionOnVoxel(api);
                        switch (action) {
                            case 0:
                                //Nothing is done! Woo! Good to go!
                                break;
                            case 1: //It's removing the Voxel!
                                var slot = voxelsInMidSlice[whichSlot];
                                voxelsInMidSlice = voxelsInMidSlice.RemoveAt(whichSlot);
                                voxels[slot.Item1, slot.Item2, slot.Item3] = false;
                                break;
                            case 2: //It's shifting the voxel! Whoa!
                                slot = voxelsInMidSlice[whichSlot];
                                var newSlot = slot;
                                voxelsInMidSlice = voxelsInMidSlice.RemoveAt(whichSlot);
                                voxels[slot.Item1, slot.Item2, slot.Item3] = false;

                                var upRightOrDown = api.World.Rand.Next(3); //0 is up and right, 1 is just right, and 2 is down and right. This voxel can still be later removed if it's picked after!
                                if (upRightOrDown == 0) {
                                    if (slot.Item3 + 1 < voxels.GetLength(2)) {
                                        newSlot.Item3++;
                                    }
                                } else if (upRightOrDown == 2) {
                                    if (slot.Item3 - 1 > 0) {
                                        newSlot.Item3--;
                                    }
                                }
                                if (newSlot.Item1 + 1 < voxels.GetLength(0)) {
                                    newSlot.Item1++;
                                }
                                newSlot.Item2 = GetFirstFreeYAt(voxels, slot.Item1 + 1, slot.Item3 + 1); //For simplicity sake, lets just toss it on the lowest y-coord that is free.

                                voxelsInRightSlice = voxelsInRightSlice.Append(newSlot);
                                voxels[newSlot.Item1, newSlot.Item2, newSlot.Item3] = true;
                                break;
                            default: //The danger zone... Remove and roll again, but it will remove or shift it.
                                slot = voxelsInMidSlice[whichSlot];
                                voxelsInMidSlice = voxelsInMidSlice.RemoveAt(whichSlot);
                                voxels[slot.Item1, slot.Item2, slot.Item3] = false;

                                var doWhatElse = api.World.Rand.Next(2);
                                var adjacentSlot = PickRandomAdjacentVoxel(api, voxels, slot);
                                if (voxels[adjacentSlot.Item1, adjacentSlot.Item2, adjacentSlot.Item3] == true) {
                                    voxels[adjacentSlot.Item1, adjacentSlot.Item2, adjacentSlot.Item3] = false;
                                    TryRemoveVoxelFromArrays(adjacentSlot, ref voxelsInLeftSlice, ref voxelsInMidSlice, ref voxelsInRightSlice);
                                    if (doWhatElse == 1) { //Flip a coin, either remove a second which is handled above, or move it! If 1, we are moving the voxel.
                                        upRightOrDown = api.World.Rand.Next(3); //0 is up and right, 1 is just right, and 2 is down and right. This voxel can still be later removed if it's picked after!
                                        if (upRightOrDown == 0) {
                                            if (adjacentSlot.Item3 + 1 < voxels.GetLength(2)) {
                                                adjacentSlot.Item3++;
                                            }
                                        } else if (upRightOrDown == 2) {
                                            if (adjacentSlot.Item3 - 1 > 0) {
                                                adjacentSlot.Item3--;
                                            }
                                        }
                                        if (adjacentSlot.Item1 < voxels.GetLength(0)) {
                                            adjacentSlot.Item1++;
                                        }
                                        adjacentSlot.Item2 = GetFirstFreeYAt(voxels, slot.Item1 + 1, slot.Item3 + 1);
                                        FindAndAppendMovedVoxel(adjacentSlot, ref voxelsInLeftSlice, ref voxelsInMidSlice, ref voxelsInRightSlice);
                                        voxels[adjacentSlot.Item1, adjacentSlot.Item2, adjacentSlot.Item3] = true;
                                    }
                                }
                                break;
                        }

                        if (voxelsInMidSlice.Length <= 0) {
                            voxelsInMidSlice = voxelsInMidSlice.Append(voxelsInRightSlice);
                            var newSliceCoord = voxelsInRightSlice[0].Item1 + 1;
                            voxelsInRightSlice = Array.Empty<(int, int, int)>();
                            if (newSliceCoord < voxels.GetLength(0)) {
                                voxelsInRightSlice = FindNumVoxelsAlongSlice(voxels, newSliceCoord);
                            }
                        }

                        break;
                    default:
                        whichSlot = RandomPickAlongVoxelArray(api, voxelsInRightSlice.Length);
                        if (whichSlot < 0) {
                            ToolsmithModSystem.Logger.Error("Something happened during Generation of a Workpiece. Attempting to act on the Right Slice, but it is empty. Could this have actually removed all bits from the rightmost slice? Refusing action to prevent a crash hopefully.");
                            break;
                        }

                        var copyOfX = voxelsInRightSlice[whichSlot].Item1; //Entirely to keep a backup of what the X-coord of this slice in case it is emptied from this action.
                        action = ChooseActionOnVoxel(api);
                        switch (action) {
                            case 0:
                                //Nothing is done! Woo! Good to go!
                                break;
                            case 1: //It's removing the Voxel!
                                var slot = voxelsInRightSlice[whichSlot];
                                voxelsInRightSlice = voxelsInRightSlice.RemoveAt(whichSlot);
                                voxels[slot.Item1, slot.Item2, slot.Item3] = false;
                                break;
                            case 2: //It's shifting the voxel! Whoa!
                                slot = voxelsInRightSlice[whichSlot];
                                var newSlot = slot;
                                voxelsInRightSlice = voxelsInRightSlice.RemoveAt(whichSlot);
                                voxels[slot.Item1, slot.Item2, slot.Item3] = false;

                                var upRightOrDown = api.World.Rand.Next(3); //0 is up and right, 1 is just right, and 2 is down and right. This voxel can still be later removed if it's picked after!
                                if (upRightOrDown == 0) {
                                    if (slot.Item3 + 1 < voxels.GetLength(2)) {
                                        newSlot.Item3++;
                                    }
                                } else if (upRightOrDown == 2) {
                                    if (slot.Item3 - 1 > 0) {
                                        newSlot.Item3--;
                                    }
                                }
                                if (newSlot.Item1 + 1 < voxels.GetLength(0)) {
                                    newSlot.Item1++;
                                }
                                newSlot.Item2 = GetFirstFreeYAt(voxels, slot.Item1 + 1, slot.Item3 + 1); //For simplicity sake, lets just toss it on the lowest y-coord that is free.

                                voxels[newSlot.Item1, newSlot.Item2, newSlot.Item3] = true;
                                break;
                            default: //The danger zone... Remove and roll again, but it will remove or shift it.
                                slot = voxelsInRightSlice[whichSlot];
                                voxelsInRightSlice = voxelsInRightSlice.RemoveAt(whichSlot);
                                voxels[slot.Item1, slot.Item2, slot.Item3] = false;

                                var doWhatElse = api.World.Rand.Next(2);
                                var adjacentSlot = PickRandomAdjacentVoxel(api, voxels, slot);
                                if (voxels[adjacentSlot.Item1, adjacentSlot.Item2, adjacentSlot.Item3] == true) {
                                    voxels[adjacentSlot.Item1, adjacentSlot.Item2, adjacentSlot.Item3] = false;
                                    TryRemoveVoxelFromArrays(adjacentSlot, ref voxelsInLeftSlice, ref voxelsInMidSlice, ref voxelsInRightSlice);
                                    if (doWhatElse == 1) { //Flip a coin, either remove a second which is handled above, or move it! If 1, we are moving the voxel.
                                        upRightOrDown = api.World.Rand.Next(3); //0 is up and right, 1 is just right, and 2 is down and right. This voxel can still be later removed if it's picked after!
                                        if (upRightOrDown == 0) {
                                            if (adjacentSlot.Item3 + 1 < voxels.GetLength(2)) {
                                                adjacentSlot.Item3++;
                                            }
                                        } else if (upRightOrDown == 2) {
                                            if (adjacentSlot.Item3 - 1 > 0) {
                                                adjacentSlot.Item3--;
                                            }
                                        }
                                        if (adjacentSlot.Item1 < voxels.GetLength(0)) {
                                            adjacentSlot.Item1++;
                                        }
                                        adjacentSlot.Item2 = GetFirstFreeYAt(voxels, slot.Item1 + 1, slot.Item3 + 1);
                                        FindAndAppendMovedVoxel(adjacentSlot, ref voxelsInLeftSlice, ref voxelsInMidSlice, ref voxelsInRightSlice);
                                        voxels[adjacentSlot.Item1, adjacentSlot.Item2, adjacentSlot.Item3] = true;
                                    }
                                }

                                break;
                        }

                        if (voxelsInRightSlice.Length <= 0) {
                            var newSliceCoord = copyOfX + 1;
                            if (newSliceCoord < voxels.GetLength(0)) {
                                voxelsInRightSlice = FindNumVoxelsAlongSlice(voxels, newSliceCoord);
                            }
                        }

                        break;
                }

                numToEffect--;
            }

            return voxels;
        }

        public static (int,int,int) FindLeftmostVoxel(bool[,,] voxels) {
            (int, int, int) firstVoxel = (0, 0, 0); //First Voxel from the left! Upper layers first, then just from 0 upwards on both x and z
            bool foundFirst = false;
            for (int x = 0; x < voxels.GetLength(0); x++) {
                for (int y = voxels.GetLength(1) - 1; y >= 0; y--) { //Start from the top and work down, so that we hit the upper voxels first
                    for (int z = 0; z < voxels.GetLength(2); z++) {
                        if (voxels[x, y, z] == true) {
                            firstVoxel = (x, y, z);
                            foundFirst = true;
                            break;
                        }
                    }
                    if (foundFirst) {
                        break;
                    }
                }
                if (foundFirst) {
                    break;
                }
            }

            return firstVoxel;
        }

        public static (int,int,int)[] FindNumVoxelsAlongSlice(bool[,,] voxels, int xSlice) { //Going with the multi-dim array defaults of 0 is x, 1 is y, and 2 is z. This counts for all valid voxels in the Z-Y slice of the workpiece, based on the X sent.
            IEnumerable<(int, int, int)> buildCoords = Array.Empty<(int, int, int)>();

            for (int y = 0; y < voxels.GetLength(1); y++) {
                for (int z = 0; z < voxels.GetLength(2); z++) {
                    if (voxels[xSlice, y, z] == true) {
                        buildCoords = buildCoords.Append((xSlice, y, z));
                    }
                }
            }

            return buildCoords.ToArray();
        }

        public static int RandomPickAlongVoxelArray(ICoreAPI api, int arrayLength) {
            if (arrayLength == 0) {
                return -1;
            }
            return api.World.Rand.Next(arrayLength);
        }

        public static int RandomDamageDepth(ICoreAPI api) {
            var randNum = api.World.Rand.Next(10);

            if (randNum < 6) { //This should come out to 60% of it being 0-5
                return 0;
            } else if (randNum < 9) { //30% of it being 6-8
                return 1;
            } else { //10% of it actually being 9
                return 2;
            }
        }

        public static int ChooseActionOnVoxel(ICoreAPI api) { //This will return 0 for no action, 1 for delete the voxel, 2 for move the voxel, and 3 for delete and double down.
            var randNum = api.World.Rand.Next(20);

            if (randNum < 3) { //If it is 0-2, that should be 15% likely
                return 0;
            } else if (randNum < 13) { //If it is 3-12, that is 50% likely
                return 1;
            } else if (randNum < 19) { //If it is 13-18, that is 30% likely
                return 2;
            } else {
                return 3;
            }
        }

        public static int GetFirstFreeYAt(bool[,,] voxels, int x, int z) {
            var y = 0;

            while (y < 6 && voxels[x, y, z] == true) {
                y++;
            }

            return y;
        }

        public static (int,int,int) PickRandomAdjacentVoxel(ICoreAPI api, bool[,,] voxels, (int,int,int) originVoxel) {
            var adjacent = originVoxel;
            var randDir = api.World.Rand.Next(6); //0 means test the voxel to the left, 1 is the one "north" from it, 2 is "south", 3 is the y above, 4 is the y below, and 5 is to the right. Any of these could easily not actually be a valid entry in the array.

            switch (randDir) {
                case 0:
                    adjacent.Item1--;
                    if (adjacent.Item1 < 0) {
                        adjacent.Item1 = 0;
                    }
                    break;
                case 1:
                    adjacent.Item3++;
                    if (adjacent.Item3 >= voxels.GetLength(2)) {
                        adjacent.Item3 = voxels.GetLength(2) - 1;
                    }
                    break;
                case 2:
                    adjacent.Item3--;
                    if (adjacent.Item3 < 0) {
                        adjacent.Item3 = 0;
                        
                    }
                    break;
                case 3:
                    adjacent.Item2++;
                    if (adjacent.Item2 >= voxels.GetLength(1)) {
                        adjacent.Item2 = voxels.GetLength(1) - 1;
                    }
                    break;
                case 4:
                    adjacent.Item2--;
                    if (adjacent.Item2 < 0) {
                        adjacent.Item2 = 0;
                    }
                    break;
                default:
                    adjacent.Item1++;
                    if (adjacent.Item1 >= voxels.GetLength(0)) {
                        adjacent.Item1 = voxels.GetLength(0) - 1;
                    }
                    break;
            } //Again for simplicity sake, if it just happens to roll one that's not valid, lets just not bother. It's such a small chance anyway.

            return adjacent;
        }

        public static void TryRemoveVoxelFromArrays((int, int, int) voxel, ref (int, int, int)[] leftSlice, ref (int, int, int)[] midSlice, ref (int, int, int)[] rightSlice) {
            if (leftSlice.Length > 0) {
                if (leftSlice.Contains(voxel)) {
                    leftSlice = leftSlice.Remove(voxel);
                    return;
                }
            }
            if (midSlice.Length > 0) {
                if (midSlice.Contains(voxel)) {
                    midSlice = midSlice.Remove(voxel);
                    return;
                }
            }
            if (rightSlice.Length > 0) {
                if (rightSlice.Contains(voxel)) {
                    rightSlice = rightSlice.Remove(voxel);
                    return;
                }
            }
        }

        public static void FindAndAppendMovedVoxel((int, int, int) voxel, ref (int, int, int)[] leftSlice, ref (int, int, int)[] midSlice, ref (int, int, int)[] rightSlice) {
            var voxX = voxel.Item1;
            if (leftSlice.Length > 0 && voxX == leftSlice[0].Item1) {
                leftSlice = leftSlice.Append(voxel);
            } else if (midSlice.Length > 0 && voxX == midSlice[0].Item1) {
                midSlice = midSlice.Append(voxel);
            } else if (rightSlice.Length > 0 && voxX == rightSlice[0].Item1) {
                rightSlice = rightSlice.Append(voxel);
            }
        }

        public static void SerializeVoxelsToWorkPiece(ItemStack workpiece, bool[,,] voxels) {
            workpiece.Attributes.SetBytes(ToolsmithAttributes.WorkPieceVoxels, BlockEntityAnvil.serializeVoxels(ConvertBoolToByteArray(voxels)));
        }

        public static void SetRecipeIDToWorkPiece(ItemStack workpiece, SmithingRecipe recipe) {
            workpiece.Attributes.SetInt(ToolsmithAttributes.WorkPieceSelectedRecipeID, recipe.RecipeId);
        }

        public static byte[,,] ConvertBoolToByteArray(bool[,,] source) {
            byte[,,] destination = new byte[source.GetLength(0), source.GetLength(1), source.GetLength(2)];
            for (int x = 0; x < source.GetLength(0); x++) {
                for (int y = 0; y < source.GetLength(1); y++) {
                    for (int z = 0; z < source.GetLength(2); z++) {
                        if (source[x,y,z] == true) {
                            destination[x, y, z] = 1;
                        } else {
                            destination[x, y, z] = 0;
                        }
                    }
                }
            }

            return destination;
        }
    }
}
