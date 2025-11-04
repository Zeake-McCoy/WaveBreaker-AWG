using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.Entities.Blocks;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace AWG.DroneDetacher
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TerminalBlock), false, "Detacher2")]
    public class DroneDetacher : MyGameLogicComponent
    {
        private string subtypeID = "Detacher2";
        private string particle_effect_name = "BlockDestroyedExplosion_Tiny";

        //Core
        private IMyTerminalBlock base_panel;
        private const ushort bolt_net_id = 32772;


        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base_panel = Entity as IMyTerminalBlock;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (base_panel.CubeGrid.Physics != null)
            {
                MyAPIGateway.Multiplayer.RegisterMessageHandler(bolt_net_id, bolt_net_handler);
                var property_check = base_panel.GetActionWithName("Explode");
                if (property_check == null)
                {
                    IMyTerminalControlButton explode_button = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyTerminalBlock>("Explode");
                    explode_button.Title = MyStringId.GetOrCompute("Explode");
                    explode_button.Tooltip = MyStringId.GetOrCompute("Detaches grid");
                    explode_button.SupportsMultipleBlocks = true;
                    explode_button.Action = detach_action;
                    explode_button.Visible = (b) => b.BlockDefinition.SubtypeName == subtypeID;
                    explode_button.Enabled = (b) => b.BlockDefinition.SubtypeName == subtypeID;

                    IMyTerminalAction explode_toolbar = MyAPIGateway.TerminalControls.CreateAction<IMyTerminalBlock>("Explode");
                    StringBuilder detach_sb = new StringBuilder("Explode");
                    explode_toolbar.Name = detach_sb;
                    explode_toolbar.Action = split_toolbar_action;
                    explode_toolbar.Enabled = (b) => b.BlockDefinition.SubtypeName == subtypeID;

                    MyAPIGateway.TerminalControls.AddControl<IMyTerminalBlock>(explode_button);
                    MyAPIGateway.TerminalControls.AddAction<IMyTerminalBlock>(explode_toolbar);
                }
            }
        }

        private void split_toolbar_action(IMyTerminalBlock obj)
        {
            var logic = GetLogic(obj);
            if (logic != null)
            {
                if (obj.EntityId == logic.base_panel.EntityId)
                {
                    MyAPIGateway.Multiplayer.SendMessageToOthers(bolt_net_id, MyAPIGateway.Utilities.SerializeToBinary<long>(logic.base_panel.EntityId));
                    MyVisualScriptLogicProvider.CreateParticleEffectAtPosition(particle_effect_name, logic.base_panel.WorldMatrix.Translation);
                    if (MyAPIGateway.Session.IsServer)
                    {
                        logic.DoDetach();
                    }
                }
            }
        }

        private void detach_action(IMyTerminalBlock obj)
        {
            var logic = GetLogic(obj);
            if (logic != null)
            {
                if (obj.EntityId == logic.base_panel.EntityId)
                {
                    MyAPIGateway.Multiplayer.SendMessageToOthers(bolt_net_id, MyAPIGateway.Utilities.SerializeToBinary<long>(logic.base_panel.EntityId));
                    MyVisualScriptLogicProvider.CreateParticleEffectAtPosition(particle_effect_name, logic.base_panel.WorldMatrix.Translation);
                    if (MyAPIGateway.Session.IsServer)
                    {
                        logic.DoDetach();
                    }
                }
            }
        }


        private void bolt_net_handler(byte[] obj)
        {
            var block = MyAPIGateway.Entities.GetEntityById(MyAPIGateway.Utilities.SerializeFromBinary<long>(obj)) as IMyTerminalBlock;
            var logic = GetLogic(block);
            if (logic != null && logic.base_panel.EntityId == block.EntityId)
            {              
                MyVisualScriptLogicProvider.CreateParticleEffectAtPosition(particle_effect_name, logic.base_panel.WorldMatrix.Translation);
                if (MyAPIGateway.Session.IsServer)
                {
                    logic.DoDetach();
                }
            }
        }

        private void DoDetach()
        {
            if (base_panel != null)
            {
                base_panel.CubeGrid.RazeBlock(base_panel.Position);
            }
        }

        private static DroneDetacher GetLogic(IMyTerminalBlock b)
        {
            return b?.GameLogic?.GetAs<DroneDetacher>();
        }



        public override void Close()
        {
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(bolt_net_id, bolt_net_handler);
        }
    }
}