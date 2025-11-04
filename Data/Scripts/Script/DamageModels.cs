using System;
using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.Game.Entities;
using SpaceEngineers.Game.Entities.Blocks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using Sandbox.Definitions;
using EmptyKeys.UserInterface.Generated;
using VRage.Utils;

namespace AWG.DamageModels
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class DamageModels : MySessionComponentBase
    {
        
        List<string> list_damagemodels = new List<string>()
        {
            "SlatFlat",
            "Slat45",
            "Slat30",
            "SlatCentre",
            "SlatHalfVertical",
            "SlatHalfHorizontal",

            "RealERA",
            "RealERAExtended",
            "RealERAHalf",
            "MagERA",
            "MagERAPanel",
            "ERAPanel",
            "ERAPanelCentre",
            "ERAPanelHalf",
            "KontaktAngledFull",
            "KontaktAngledSquare",
            "KontaktFlatFull",
            "KontaktFlatHalf",
            "KontaktFlatFullCentred",
            "KontaktFlatHalfCentred",
            "KontaktSquareFull",
            "KontaktSquareCentred",

            "PanelSideEngineRight",
            "PanelSideEngineLeft",
            "PanelBonnetRight",
            "PanelBonnetLeft",
            "PanelBonnetCentral",
            "BumperVent",
            "SpotlightLeft",
            "SpotlightRight",
            "PanelFrontBumper",
            "PanelDoorHousingRight",
            "PanelDoorHousingLeft",
            "DoorRightWindow",
            "DoorLeftWindow",
            "PanelCabRearUpper",
            "PanelBedBack",
            "PanelBedSideExtensionRight",
            "PanelBedSideExtensionLeft",
            "PanelBedWheelCutoutRight",
            "PanelBedWheelCutoutLeft",
            "PanelBedSideRearExtendedRight",
            "PanelBedSideRearExtendedLeft",
            "PanelBedDoor",
            "PanelInnerWheelArchRight",
            "PanelInnerWheelArchLeft",

            "ChassisCrossbarBumper",
            "ChassisBarFrontLongRight",
            "ChassisBarFrontLongLeft",
            "ChassisCrossbarFront",
            "ChassisBarRight",
            "ChassisBarLeft",
            "ChassisCrossbarRear",
            "ChassisBarRearLongRight",
            "ChassisBarRearLongLeft",
            "ChassisTailbar",
        };

        private float BlockDetachThreshold = 50f;//what damage needs to be done to trigger this
        
        //the more damage you do in a single hit, the higher the chance of a detach?
        //damage threshold/resist? how can I do this when I can only have a single data point per item...

        //Your particles must be set in the files themselves!
       
        private int MinBound = 1;
        private int MaxBound = 100;
        private float DetachPercentChance = 4;

        private float selfDamage = 9999f;//don't worry about this for weld/grind
        private float belowDamage = 85f;
        private float testDamage = 100f;

        private IMySlimBlock useBlock;
        private MyCubeGrid DefinedCubeGrid;
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler (1, DHandle);


            }
        }

        private void DHandle(object target, ref MyDamageInformation info)
        {
            if (target is IMySlimBlock && target != null)
            {
                useBlock = target as IMySlimBlock;
                DefinedCubeGrid = useBlock.CubeGrid as MyCubeGrid;

                if (list_damagemodels.Contains(useBlock.BlockDefinition.Id.SubtypeName))
                {
                    foreach (var item in list_damagemodels)//for each item in the dictionary
                    {
                        if (useBlock.BlockDefinition.Id.SubtypeName.Contains("Panel") && (!useBlock.BlockDefinition.Id.SubtypeName.Contains("Wind")))
                        {
                            //MyAPIGateway.Utilities.ShowNotification($"Panel!", 5000, MyFontEnum.Green);
                            BlockDetachThreshold = 30f;
                        }

                        if (useBlock.BlockDefinition.Id.SubtypeName.Contains("Wind"))
                        {
                            //MyAPIGateway.Utilities.ShowNotification($"Window!", 5000, MyFontEnum.Blue);
                            BlockDetachThreshold = 100f;
                        }

                        if (useBlock.BlockDefinition.Id.SubtypeName.Contains("Chassis"))
                        {
                            //MyAPIGateway.Utilities.ShowNotification($"Chassis!", 5000, MyFontEnum.White);
                            BlockDetachThreshold = 480f;
                        }

                        if (info.Amount >= BlockDetachThreshold)
                        {
                            
                            bool Split = false;
                            //let's say there's a 12mm part.
                            //I'd want that to detach at... 8mm (75%,every time, if you didn't destroy it then, in one hit)
                            //I want a rate of detach that increases, the more damage it takes. So that's an integrity affected one.
                            //I need to read the integrity PERCENTAGE, I think...

                            if (useBlock.AccumulatedDamage >= useBlock.MaxIntegrity * 0.75 && useBlock.AccumulatedDamage != useBlock.Integrity)//if the damage is above 75% of the block's base health in one hit, run 1/3 chance diceroll
                            {
                                //MyAPIGateway.Utilities.ShowNotification($"Accumulated Damage: {useBlock.AccumulatedDamage}", 2000, MyFontEnum.White);
                                //MyAPIGateway.Utilities.ShowNotification($"Hit: {useBlock.FatBlock.DisplayNameText}", 5000, MyFontEnum.White);
                                //MyAPIGateway.Utilities.ShowNotification($"Damage Amount: {info.Amount:n0}", 3000, MyFontEnum.Red);

                                var GameOfChance = 1;
                                var SpinTheWheel = 2;
                                int RollTheDice = new Random().Next(GameOfChance, SpinTheWheel);  // creates a number between 0 and 2, so a 33% chance (0, 1 and 2)

                                MyAPIGateway.Utilities.ShowNotification($"RTD: {RollTheDice}", 2000, MyFontEnum.Red);

                                if (RollTheDice == 2)
                                {
                                    Split = true;
                                }

                            }
                            DetachPercentChance *= (useBlock.MaxIntegrity / useBlock.Integrity);
                            var DetachChance = MyUtils.GetRandomFloat(MinBound*DetachPercentChance,DetachPercentChance);//50% chance

                            if (DetachChance >=  10)
                            {
                                Split = true;
                                //MyAPIGateway.Utilities.ShowNotification($"detach chance is reached!", 3000, MyFontEnum.White);
                            }

                            if (Split == true)
                            {
                                //split this stuff off, turn later, into a damage report system in the menu

                                //MyAPIGateway.Utilities.ShowNotification($"Damage Type: {info.Type}", 5000, MyFontEnum.Green);

                                BlockDetachThreshold = 50f;//resetting to default

                                useBlock.RemoveNeighbours();

                                DefinedCubeGrid.UpdateDirty(null, true);
                                DefinedCubeGrid.DetectDisconnectsAfterFrame();
                                //DefinedCubeGrid.MarkAsTrash();

                                //MyAPIGateway.Utilities.ShowNotification($"DETACH!", 10000, MyFontEnum.White);
                                return;
                            };

                        }
                        else
                        {
                            //MyAPIGateway.Utilities.ShowNotification($"BlockDetachThreshold unmet", 5000, MyFontEnum.Blue);
                            return;
                        }
                    }
                }
                else
                {
                    //MyAPIGateway.Utilities.ShowNotification($"Target not counted as IMySlimBlock", 5000, MyFontEnum.Red);
                }

            }
        }


        protected override void UnloadData()
        {

        }
    }
}