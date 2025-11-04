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


namespace Klime.ERA
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class ERA : MySessionComponentBase
    {

        List<string> era_subtype_ids = new List<string>()
        {
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
        };

        //Your particles must be set in the files themselves!
        private float explodeThreshold = 25f;
        private float selfDamage = 9999f;
        private float belowDamage = 85f;

        private IMySlimBlock useBlock;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, DHandle);
            }
        }

        private void DHandle(object target, ref MyDamageInformation info)
        {
            if (target is IMySlimBlock)
            {
                useBlock = target as IMySlimBlock;
                IMyCubeGrid pp;
                if (era_subtype_ids.Contains(useBlock.BlockDefinition.Id.SubtypeName))
                {
                    if (info.Amount >= explodeThreshold)
                    {
                        info.Amount = selfDamage;
                    }
                    else
                    {
                        info.Amount = belowDamage;
                    }
                }
            }
        }

        protected override void UnloadData()
        {

        }
    }
}