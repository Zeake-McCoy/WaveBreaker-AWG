using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.Utils;
using VRageMath;


namespace klime.Scorch
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Scorch : MySessionComponentBase
    {
        public float damageThreshold = 4f;
        public List<MyEntity> sphereEnts = new List<MyEntity>();
        public List<IMyCubeGrid> sphereGrids = new List<IMyCubeGrid>();
        public List<IMySlimBlock> sphereBlocks = new List<IMySlimBlock>();
        public BoundingSphereD scorchSphere;

        public override void BeforeStart()
        {
            if (MyAPIGateway.Session.IsServer)
            {
                MyExplosions.OnExplosion += OnExplosion;
            }
        }

        private void OnExplosion(ref MyExplosionInfo explosionInfo)
        {
            if (explosionInfo.Damage < damageThreshold || explosionInfo.ExplosionType == MyExplosionTypeEnum.MISSILE_EXPLOSION)
            {
                return;
            }

            sphereEnts.Clear();
            sphereGrids.Clear();

            var originalSphere = explosionInfo.ExplosionSphere;
            var sphereRadiusMultiplier = 0.35;

            if (explosionInfo.Damage > 150)
            {
                sphereRadiusMultiplier = 1.8;
            }

            scorchSphere = originalSphere;
            scorchSphere.Radius = MathHelperD.Clamp(scorchSphere.Radius * sphereRadiusMultiplier, 1, 10);

            //MyAPIGateway.Utilities.ShowMessage("", $"Dmg: {explosionInfo.Damage}");
            //MyAPIGateway.Utilities.ShowMessage("", $"Radius: {originalSphere.Radius}");

            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref scorchSphere, sphereEnts);
            foreach (var ent in sphereEnts)
            {
                IMyCubeGrid grid = ent as IMyCubeGrid;
                if (grid != null && grid.Physics != null)
                {
                    sphereGrids.Add(grid);
                }
            }

            float workingRadius = (float)scorchSphere.Radius;

            foreach (var grid in sphereGrids)
            {
                sphereBlocks.Clear();
                sphereBlocks = grid.GetBlocksInsideSphere(ref scorchSphere);
                MyCubeGrid cGrid = grid as MyCubeGrid;

                foreach (var block in sphereBlocks)
                {
                    Vector3D worldCenter;
                    block.ComputeWorldCenter(out worldCenter);

                    var distanceFromExplosion = Vector3.Distance(worldCenter, originalSphere.Center);
                    var distanceFraction = MathHelper.Clamp((distanceFromExplosion / workingRadius), 0, 1);
                    var finalColor = block.ColorMaskHSV;

                    if (distanceFraction < 0.5)
                    {
                        finalColor.X = -0.15f;//original colour 0.5 (Hue)
                        finalColor.Y = -0.7f;//original -0.8 (Saturation)
                        finalColor.Z = -0.33f;//original -0.45 (Value) - put clamp in here to stop it turning bright red in servers, which don't have limits
                        if (finalColor.X <= 0)
                        {
                            finalColor.X = 0.05f;
                        }
                        grid.ColorBlocks(block.Min, block.Max, finalColor);
                        var ranInt = MyUtils.GetRandomInt(0, 40);
                        if (ranInt > 20)
                        {
                            grid.SkinBlocks(block.Min, block.Max, finalColor, "HeavyRust_Armor");
                            cGrid.ChangeColorAndSkin(cGrid.GetCubeBlock(block.Position), finalColor, MyStringHash.GetOrCompute("HeavyRust_Skin"));
                        }
                        else
                        {
                        }
                    }
                    else
                    {
                        finalColor.Z = MathHelper.Lerp(-0.7f, finalColor.Z, distanceFraction);
                        //if (finalColor.Z <= 0)
                        //{
                        //    finalColor.Z = 0.05f;
                        //}
                        grid.ColorBlocks(block.Min, block.Max, finalColor);
                    }

                    //23-06-21 addition, detaches blocks in explosions below 30%

                    if (block.Integrity <= (block.MaxIntegrity*0.35))//if below 30%
                    {
                        
                        block.RemoveNeighbours();
                        cGrid.UpdateDirty(null, true);
                        //MyAPIGateway.Utilities.ShowNotification($"Explosive Detach!", 10000, MyFontEnum.White);
                        cGrid.DetectDisconnectsAfterFrame();

                        //var DetachChance = MyUtils.GetRandomInt(1, 2);//50% chance
                        //if (DetachChance == 2)
                        //{

                        //};
                    }


                }


            }
        }

        protected override void UnloadData()
        {
            if (MyAPIGateway.Session.IsServer)
            {
                MyExplosions.OnExplosion -= OnExplosion;
            }
        }
    }
}