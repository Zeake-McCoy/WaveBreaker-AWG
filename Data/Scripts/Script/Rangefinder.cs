using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using Sandbox.Game.Weapons;
using VRage.Game.ModAPI;
using VRageMath;
using Sandbox.Game;
using VRage.Game.Entity;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.ModAPI;
using Sandbox.Definitions;

namespace Whiplash.Rangefinder
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CameraBlock), false, "LaserCam")]
    public class Rangefinder : MyGameLogicComponent
    {
        const float _scanRange = 5000f;

        IMyCameraBlock _camera = null;
        static bool _terminalControlsInit = false;

        float _raycastRechargeRate = 0;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            try
            {
                

                _camera = (IMyCameraBlock)Entity;

                var cube = (IMyCubeBlock)Entity;
                var def = cube.SlimBlock.BlockDefinition as MyCameraBlockDefinition;
                _raycastRechargeRate = def.RaycastTimeMultiplier * 1000f;
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("Exception in rangefinder init", 10000, MyFontEnum.Red);
                MyLog.Default.WriteLine(e);
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            _camera.EnableRaycast = true;
            InitTerminalControls();
        }

        void InitTerminalControls()
        {
            if (_terminalControlsInit)
                return;

            IMyTerminalControlButton raycastButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyCameraBlock>("Scan");
            raycastButton.Title = MyStringId.GetOrCompute("Scan");
            raycastButton.Enabled = x => x.BlockDefinition.SubtypeId.Equals("LaserCam");
            raycastButton.Visible = x => x.BlockDefinition.SubtypeId.Equals("LaserCam");
            raycastButton.SupportsMultipleBlocks = false;
            raycastButton.Action = x =>
            {
                var y = x.GameLogic.GetAs<Rangefinder>();
                if (y == null)
                    return;

                var camera = x as IMyCameraBlock;
                if (camera == null)
                    return;

                Raycast(camera);
            };
            MyAPIGateway.TerminalControls.AddControl<IMyCameraBlock>(raycastButton);

            //Recharge toggle action
            IMyTerminalAction raycastAction = MyAPIGateway.TerminalControls.CreateAction<IMyCameraBlock>("Scan");
            raycastAction.Action = (x) =>
            {
                var y = x.GameLogic.GetAs<Rangefinder>();
                if (y == null)
                    return;

                var camera = x as IMyCameraBlock;
                if (camera == null)
                    return;

                Raycast(camera);
            };
            raycastAction.ValidForGroups = true;
            raycastAction.Writer = (x, s) => GetWriter(x, s);
            raycastAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds"; //change this
            raycastAction.Enabled = x => x.BlockDefinition.SubtypeId.Equals("LaserCam");
            raycastAction.Name = new StringBuilder("Scan");
            MyAPIGateway.TerminalControls.AddAction<IMyCameraBlock>(raycastAction);

            _terminalControlsInit = true;
        }

        public void GetWriter(IMyTerminalBlock x, StringBuilder s)
        {
            s.Clear();
            s.Append("Scan");
        }

        void Raycast(IMyCameraBlock camera)
        {
            if (!camera.EnableRaycast)
                camera.EnableRaycast = true;

            if (!camera.CanScan(_scanRange))
            {
                var timeToNextScan = 5000;// Used to be this, Math.Max(0, _scanRange - camera.AvailableScanRange) / _raycastRechargeRate, now is two seconds for rangefinders, 5s for FCS. Think of way to delay this.
                MyAPIGateway.Utilities.ShowNotification($"Scan ready in {timeToNextScan:n2} second(s)", 5000, MyFontEnum.Red);
                return;
            }

            var detectedInfo = camera.Raycast(_scanRange);
            if (detectedInfo.IsEmpty())
            {
                MyAPIGateway.Utilities.ShowNotification("No target found", 5000, MyFontEnum.White);
                return;
            }

            var distance = Vector3D.Distance((Vector3D)detectedInfo.HitPosition, camera.GetPosition());
            MyAPIGateway.Utilities.ShowNotification($"Range to target: {distance:n0} m", 5000, MyFontEnum.Green);
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CameraBlock), false, "RangeFinderLate")]
    public class RangeFinderLateSmall : MyGameLogicComponent//Class name can't be same as subtype I think
    {
        const float _scanRange = 3500f;

        IMyCameraBlock _camera = null;
        static bool _terminalControlsInit = false;

        float _raycastRechargeRate = 0;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            try
            {
                InitTerminalControls();

                _camera = (IMyCameraBlock)Entity;

                var cube = (IMyCubeBlock)Entity;
                var def = cube.SlimBlock.BlockDefinition as MyCameraBlockDefinition;
                _raycastRechargeRate = def.RaycastTimeMultiplier * 1000f;
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("Exception in rangefinder init", 10000, MyFontEnum.Red);
                MyLog.Default.WriteLine(e);
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            _camera.EnableRaycast = true;
        }

        void InitTerminalControls()
        {
            if (_terminalControlsInit)
                return;

            IMyTerminalControlButton raycastButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyCameraBlock>("Scan");
            raycastButton.Title = MyStringId.GetOrCompute("Scan");
            raycastButton.Enabled = x => x.BlockDefinition.SubtypeId.Equals("RangeFinderLate");
            raycastButton.Visible = x => x.BlockDefinition.SubtypeId.Equals("RangeFinderLate");
            raycastButton.SupportsMultipleBlocks = false;
            raycastButton.Action = x =>
            {
                var y = x.GameLogic.GetAs<RangeFinderLateSmall>();
                if (y == null)
                    return;

                var camera = x as IMyCameraBlock;
                if (camera == null)
                    return;

                Raycast(camera);
            };
            MyAPIGateway.TerminalControls.AddControl<IMyCameraBlock>(raycastButton);

            //Recharge toggle action
            IMyTerminalAction raycastAction = MyAPIGateway.TerminalControls.CreateAction<IMyCameraBlock>("Scan");
            raycastAction.Action = (x) =>
            {
                var y = x.GameLogic.GetAs<RangeFinderLateSmall>();
                if (y == null)
                    return;

                var camera = x as IMyCameraBlock;
                if (camera == null)
                    return;

                MyAPIGateway.Utilities.ShowNotification("Scanning", 1000, MyFontEnum.White);

                Raycast(camera);
            };
            raycastAction.ValidForGroups = true;
            raycastAction.Writer = (x, s) => GetWriter(x, s);
            raycastAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds"; //change this
            raycastAction.Enabled = x => x.BlockDefinition.SubtypeId.Equals("RangeFinderLate");
            raycastAction.Name = new StringBuilder("Scan");
            MyAPIGateway.TerminalControls.AddAction<IMyCameraBlock>(raycastAction);

            _terminalControlsInit = true;
        }

        public void GetWriter(IMyTerminalBlock x, StringBuilder s)
        {
            s.Clear();
            s.Append("Scan");
        }

        void Raycast(IMyCameraBlock camera)
        {
            if (!camera.EnableRaycast)
                camera.EnableRaycast = true;

            if (!camera.CanScan(_scanRange))
            {
                var timeToNextScan = 2000;// Used to be this, Math.Max(0, _scanRange - camera.AvailableScanRange) / _raycastRechargeRate, now is two seconds for rangefinders, 5s for FCS. Think of way to delay this.
                MyAPIGateway.Utilities.ShowNotification($"Scan ready in {timeToNextScan:n2} second(s)", 5000, MyFontEnum.Red);
                return;
            }

            var detectedInfo = camera.Raycast(_scanRange);
            if (detectedInfo.IsEmpty())
            {
                MyAPIGateway.Utilities.ShowNotification("No target found", 5000, MyFontEnum.White);
                return;
            }

            var distance = Vector3D.Distance((Vector3D)detectedInfo.HitPosition, camera.GetPosition());
            MyAPIGateway.Utilities.ShowNotification($"Range to target: {distance:n0} m", 5000, MyFontEnum.Green);
        }
    }


    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CameraBlock), false, "FCSCamNew")]
    public class FCSNew : MyGameLogicComponent
    {
        const float _scanRange = 1500f;

        IMyCameraBlock _camera = null;
        static bool _terminalControlsInit = false;

        float _raycastRechargeRate = 1;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            try
            {
                InitTerminalControls();

                _camera = (IMyCameraBlock)Entity;

                var cube = (IMyCubeBlock)Entity;
                var def = cube.SlimBlock.BlockDefinition as MyCameraBlockDefinition;
                _raycastRechargeRate = def.RaycastTimeMultiplier * 1000f;
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("Exception in rangefinder init", 10000, MyFontEnum.Red);
                MyLog.Default.WriteLine(e);
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            _camera.EnableRaycast = true;
        }

        void InitTerminalControls()
        {
            if (_terminalControlsInit)
                return;

            IMyTerminalControlButton raycastButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyCameraBlock>("Range");
            raycastButton.Title = MyStringId.GetOrCompute("Range");
            raycastButton.Enabled = x => x.BlockDefinition.SubtypeId.Equals("FCSCamNew");
            raycastButton.Visible = x => x.BlockDefinition.SubtypeId.Equals("FCSCamNew");
            raycastButton.SupportsMultipleBlocks = false;
            raycastButton.Action = x =>
            {
                var y = x.GameLogic.GetAs<FCSNew>();
                if (y == null)
                    return;

                var camera = x as IMyCameraBlock;
                if (camera == null)
                    return;

                Raycast(camera);
            };
            MyAPIGateway.TerminalControls.AddControl<IMyCameraBlock>(raycastButton);

            //Recharge toggle action
            IMyTerminalAction raycastAction = MyAPIGateway.TerminalControls.CreateAction<IMyCameraBlock>("Range");
            raycastAction.Action = (x) =>
            {
                var y = x.GameLogic.GetAs<FCSNew>();
                if (y == null)
                    return;

                var camera = x as IMyCameraBlock;
                if (camera == null)
                    return;

                Raycast(camera);
            };
            raycastAction.ValidForGroups = true;
            raycastAction.Writer = (x, s) => GetWriter(x, s);
            raycastAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds"; //change this
            raycastAction.Enabled = x => x.BlockDefinition.SubtypeId.Equals("FCSCamNew");
            raycastAction.Name = new StringBuilder("Range");
            MyAPIGateway.TerminalControls.AddAction<IMyCameraBlock>(raycastAction);

            _terminalControlsInit = true;
        }

        public void GetWriter(IMyTerminalBlock x, StringBuilder s)
        {
            s.Clear();
            s.Append("Range");
        }

        void Raycast(IMyCameraBlock camera)
        {
            if (!camera.EnableRaycast)
                camera.EnableRaycast = true;

            if (!camera.CanScan(_scanRange))
            {
                var timeToNextScan = 5000;// Used to be this, Math.Max(0, _scanRange - camera.AvailableScanRange) / _raycastRechargeRate, now is two seconds for rangefinders, 5s for FCS. Think of way to delay this.
                MyAPIGateway.Utilities.ShowNotification($"Scan ready in {timeToNextScan:n2} second(s)", 5000, MyFontEnum.Red);
                return;
            }

            var detectedInfo = camera.Raycast(_scanRange);
            if (detectedInfo.IsEmpty())
            {
                MyAPIGateway.Utilities.ShowNotification("No target found", 5000, MyFontEnum.White);
                return;
            }

            var distance = Vector3D.Distance((Vector3D)detectedInfo.HitPosition, camera.GetPosition());
            MyAPIGateway.Utilities.ShowNotification($"Range to target: {distance:n0} m", 5000, MyFontEnum.Green);
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CameraBlock), false, "FCSCamNewLeft")]
    public class FCSCamNewLeft : MyGameLogicComponent
    {
        const float _scanRange = 1500f;

        IMyCameraBlock _camera = null;
        static bool _terminalControlsInit = false;

        float _raycastRechargeRate = 1;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            try
            {
                InitTerminalControls();

                _camera = (IMyCameraBlock)Entity;

                var cube = (IMyCubeBlock)Entity;
                var def = cube.SlimBlock.BlockDefinition as MyCameraBlockDefinition;
                _raycastRechargeRate = def.RaycastTimeMultiplier * 1000f;
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("Exception in rangefinder init", 10000, MyFontEnum.Red);
                MyLog.Default.WriteLine(e);
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            _camera.EnableRaycast = true;
        }

        void InitTerminalControls()
        {
            if (_terminalControlsInit)
                return;

            IMyTerminalControlButton raycastButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyCameraBlock>("Range");
            raycastButton.Title = MyStringId.GetOrCompute("Range");
            raycastButton.Enabled = x => x.BlockDefinition.SubtypeId.Equals("FCSCamNewLeft");
            raycastButton.Visible = x => x.BlockDefinition.SubtypeId.Equals("FCSCamNewLeft"); //correct subtypes on the rest lol
            raycastButton.SupportsMultipleBlocks = false;
            raycastButton.Action = x =>
            {
                var y = x.GameLogic.GetAs<FCSCamNewLeft>();
                if (y == null)
                    return;

                var camera = x as IMyCameraBlock;
                if (camera == null)
                    return;

                Raycast(camera);
            };
            MyAPIGateway.TerminalControls.AddControl<IMyCameraBlock>(raycastButton);

            //Recharge toggle action
            IMyTerminalAction raycastAction = MyAPIGateway.TerminalControls.CreateAction<IMyCameraBlock>("Range");
            raycastAction.Action = (x) =>
            {
                var y = x.GameLogic.GetAs<FCSCamNewLeft>();
                if (y == null)
                    return;

                var camera = x as IMyCameraBlock;
                if (camera == null)
                    return;

                Raycast(camera);
            };
            raycastAction.ValidForGroups = true;
            raycastAction.Writer = (x, s) => GetWriter(x, s);
            raycastAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds"; //change this
            raycastAction.Enabled = x => x.BlockDefinition.SubtypeId.Equals("FCSCamNewLeft");
            raycastAction.Name = new StringBuilder("Range");
            MyAPIGateway.TerminalControls.AddAction<IMyCameraBlock>(raycastAction);

            _terminalControlsInit = true;
        }

        public void GetWriter(IMyTerminalBlock x, StringBuilder s)
        {
            s.Clear();
            s.Append("Range");
        }

        void Raycast(IMyCameraBlock camera)
        {
            if (!camera.EnableRaycast)
                camera.EnableRaycast = true;

            if (!camera.CanScan(_scanRange))
            {
                var timeToNextScan = 5000;// Used to be this, Math.Max(0, _scanRange - camera.AvailableScanRange) / _raycastRechargeRate, now is two seconds for rangefinders, 5s for FCS. Think of way to delay this.
                MyAPIGateway.Utilities.ShowNotification($"Scan ready in {timeToNextScan:n2} second(s)", 5000, MyFontEnum.Red);
                return;
            }

            var detectedInfo = camera.Raycast(_scanRange);
            if (detectedInfo.IsEmpty())
            {
                MyAPIGateway.Utilities.ShowNotification("No target found", 5000, MyFontEnum.White);
                return;
            }

            var distance = Vector3D.Distance((Vector3D)detectedInfo.HitPosition, camera.GetPosition());
            MyAPIGateway.Utilities.ShowNotification($"Range to target: {distance:n0} m", 5000, MyFontEnum.Green);
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CameraBlock), false, "FCSCamNewRight")]
    public class FCSCamNewRight : MyGameLogicComponent
    {
        const float _scanRange = 1500f;

        IMyCameraBlock _camera = null;
        static bool _terminalControlsInit = false;

        float _raycastRechargeRate = 1;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            try
            {
                InitTerminalControls();

                _camera = (IMyCameraBlock)Entity;

                var cube = (IMyCubeBlock)Entity;
                var def = cube.SlimBlock.BlockDefinition as MyCameraBlockDefinition;
                _raycastRechargeRate = def.RaycastTimeMultiplier * 1000f;
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("Exception in rangefinder init", 10000, MyFontEnum.Red);
                MyLog.Default.WriteLine(e);
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            _camera.EnableRaycast = true;
        }

        void InitTerminalControls()
        {
            if (_terminalControlsInit)
                return;

            IMyTerminalControlButton raycastButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyCameraBlock>("Range");
            raycastButton.Title = MyStringId.GetOrCompute("Range");
            raycastButton.Enabled = x => x.BlockDefinition.SubtypeId.Equals("FCSCamNewRight");
            raycastButton.Visible = x => x.BlockDefinition.SubtypeId.Equals("FCSCamNewRight");
            raycastButton.SupportsMultipleBlocks = false;
            raycastButton.Action = x =>
            {
                var y = x.GameLogic.GetAs<FCSCamNewRight>();
                if (y == null)
                    return;

                var camera = x as IMyCameraBlock;
                if (camera == null)
                    return;

                Raycast(camera);
            };
            MyAPIGateway.TerminalControls.AddControl<IMyCameraBlock>(raycastButton);

            //Recharge toggle action
            IMyTerminalAction raycastAction = MyAPIGateway.TerminalControls.CreateAction<IMyCameraBlock>("Range");
            raycastAction.Action = (x) =>
            {
                var y = x.GameLogic.GetAs<FCSCamNewRight>();
                if (y == null)
                    return;

                var camera = x as IMyCameraBlock;
                if (camera == null)
                    return;

                Raycast(camera);
            };
            raycastAction.ValidForGroups = true;
            raycastAction.Writer = (x, s) => GetWriter(x, s);
            raycastAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds"; //change this
            raycastAction.Enabled = x => x.BlockDefinition.SubtypeId.Equals("FCSCamNewRight");
            raycastAction.Name = new StringBuilder("Range");
            MyAPIGateway.TerminalControls.AddAction<IMyCameraBlock>(raycastAction);

            _terminalControlsInit = true;
        }

        public void GetWriter(IMyTerminalBlock x, StringBuilder s)
        {
            s.Clear();
            s.Append("Range");
        }

        void Raycast(IMyCameraBlock camera)
        {
            if (!camera.EnableRaycast)
                camera.EnableRaycast = true;

            if (!camera.CanScan(_scanRange))
            {
                var timeToNextScan = 5000;// Used to be this, Math.Max(0, _scanRange - camera.AvailableScanRange) / _raycastRechargeRate, now is two seconds for rangefinders, 5s for FCS. Think of way to delay this.
                MyAPIGateway.Utilities.ShowNotification($"Scan ready in {timeToNextScan:n2} second(s)", 5000, MyFontEnum.Red);
                return;
            }

            var detectedInfo = camera.Raycast(_scanRange);
            if (detectedInfo.IsEmpty())
            {
                MyAPIGateway.Utilities.ShowNotification("No target found", 5000, MyFontEnum.White);
                return;
            }

            var distance = Vector3D.Distance((Vector3D)detectedInfo.HitPosition, camera.GetPosition());
            MyAPIGateway.Utilities.ShowNotification($"Range to target: {distance:n0} m", 5000, MyFontEnum.Green);
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CameraBlock), false, "FCSCamNewOffset")]
    public class FCSNewOffset : MyGameLogicComponent
    {
        const float _scanRange = 1500f;

        IMyCameraBlock _camera = null;
        static bool _terminalControlsInit = false;

        float _raycastRechargeRate = 1;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            try
            {
                InitTerminalControls();

                _camera = (IMyCameraBlock)Entity;

                var cube = (IMyCubeBlock)Entity;
                var def = cube.SlimBlock.BlockDefinition as MyCameraBlockDefinition;
                _raycastRechargeRate = def.RaycastTimeMultiplier * 1000f;
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("Exception in rangefinder init", 10000, MyFontEnum.Red);
                MyLog.Default.WriteLine(e);
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            _camera.EnableRaycast = true;
        }

        void InitTerminalControls()
        {
            if (_terminalControlsInit)
                return;

            IMyTerminalControlButton raycastButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyCameraBlock>("Range");
            raycastButton.Title = MyStringId.GetOrCompute("Range");
            raycastButton.Enabled = x => x.BlockDefinition.SubtypeId.Equals("FCSCamNewOffset");
            raycastButton.Visible = x => x.BlockDefinition.SubtypeId.Equals("FCSCamNewOffset");
            raycastButton.SupportsMultipleBlocks = false;
            raycastButton.Action = x =>
            {
                var y = x.GameLogic.GetAs<FCSNewOffset>();
                if (y == null)
                    return;

                var camera = x as IMyCameraBlock;
                if (camera == null)
                    return;

                Raycast(camera);
            };
            MyAPIGateway.TerminalControls.AddControl<IMyCameraBlock>(raycastButton);

            //Recharge toggle action
            IMyTerminalAction raycastAction = MyAPIGateway.TerminalControls.CreateAction<IMyCameraBlock>("Range");
            raycastAction.Action = (x) =>
            {
                var y = x.GameLogic.GetAs<FCSNewOffset>();
                if (y == null)
                    return;

                var camera = x as IMyCameraBlock;
                if (camera == null)
                    return;

                Raycast(camera);
            };
            raycastAction.ValidForGroups = true;
            raycastAction.Writer = (x, s) => GetWriter(x, s);
            raycastAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds"; //change this
            raycastAction.Enabled = x => x.BlockDefinition.SubtypeId.Equals("FCSCamNewOffset");
            raycastAction.Name = new StringBuilder("Range");
            MyAPIGateway.TerminalControls.AddAction<IMyCameraBlock>(raycastAction);

            _terminalControlsInit = true;
        }

        public void GetWriter(IMyTerminalBlock x, StringBuilder s)
        {
            s.Clear();
            s.Append("Range");
        }

        void Raycast(IMyCameraBlock camera)
        {
            if (!camera.EnableRaycast)
                camera.EnableRaycast = true;

            if (!camera.CanScan(_scanRange))
            {
                var timeToNextScan = 5000;// Used to be this, Math.Max(0, _scanRange - camera.AvailableScanRange) / _raycastRechargeRate, now is two seconds for rangefinders, 5s for FCS. Think of way to delay this.
                MyAPIGateway.Utilities.ShowNotification($"Scan ready in {timeToNextScan:n2} second(s)", 5000, MyFontEnum.Red);
                return;
            }

            var detectedInfo = camera.Raycast(_scanRange);
            if (detectedInfo.IsEmpty())
            {
                MyAPIGateway.Utilities.ShowNotification("No target found", 5000, MyFontEnum.White);
                return;
            }

            var distance = Vector3D.Distance((Vector3D)detectedInfo.HitPosition, camera.GetPosition());
            MyAPIGateway.Utilities.ShowNotification($"Range to target: {distance:n0} m", 5000, MyFontEnum.Green);
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CameraBlock), false, "FCSCamNewOffsetLeft")]
    public class FCSCamNewOffsetLeft : MyGameLogicComponent
    {
        const float _scanRange = 1500f;

        IMyCameraBlock _camera = null;
        static bool _terminalControlsInit = false;

        float _raycastRechargeRate = 1;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            try
            {
                InitTerminalControls();

                _camera = (IMyCameraBlock)Entity;

                var cube = (IMyCubeBlock)Entity;
                var def = cube.SlimBlock.BlockDefinition as MyCameraBlockDefinition;
                _raycastRechargeRate = def.RaycastTimeMultiplier * 1000f;
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("Exception in rangefinder init", 10000, MyFontEnum.Red);
                MyLog.Default.WriteLine(e);
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            _camera.EnableRaycast = true;
        }

        void InitTerminalControls()
        {
            if (_terminalControlsInit)
                return;

            IMyTerminalControlButton raycastButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyCameraBlock>("Range");
            raycastButton.Title = MyStringId.GetOrCompute("Range");
            raycastButton.Enabled = x => x.BlockDefinition.SubtypeId.Equals("FCSCamNewOffsetLeft");
            raycastButton.Visible = x => x.BlockDefinition.SubtypeId.Equals("FCSCamNewOffsetLeft");
            raycastButton.SupportsMultipleBlocks = false;
            raycastButton.Action = x =>
            {
                var y = x.GameLogic.GetAs<FCSCamNewOffsetLeft>();
                if (y == null)
                    return;

                var camera = x as IMyCameraBlock;
                if (camera == null)
                    return;

                Raycast(camera);
            };
            MyAPIGateway.TerminalControls.AddControl<IMyCameraBlock>(raycastButton);

            //Recharge toggle action
            IMyTerminalAction raycastAction = MyAPIGateway.TerminalControls.CreateAction<IMyCameraBlock>("Range");
            raycastAction.Action = (x) =>
            {
                var y = x.GameLogic.GetAs<FCSCamNewOffsetLeft>();
                if (y == null)
                    return;

                var camera = x as IMyCameraBlock;
                if (camera == null)
                    return;

                Raycast(camera);
            };
            raycastAction.ValidForGroups = true;
            raycastAction.Writer = (x, s) => GetWriter(x, s);
            raycastAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds"; //change this
            raycastAction.Enabled = x => x.BlockDefinition.SubtypeId.Equals("FCSCamNewOffsetLeft");
            raycastAction.Name = new StringBuilder("Range");
            MyAPIGateway.TerminalControls.AddAction<IMyCameraBlock>(raycastAction);

            _terminalControlsInit = true;
        }

        public void GetWriter(IMyTerminalBlock x, StringBuilder s)
        {
            s.Clear();
            s.Append("Range");
        }

        void Raycast(IMyCameraBlock camera)
        {
            if (!camera.EnableRaycast)
                camera.EnableRaycast = true;

            if (!camera.CanScan(_scanRange))
            {
                var timeToNextScan = 5000;// Used to be this, Math.Max(0, _scanRange - camera.AvailableScanRange) / _raycastRechargeRate, now is two seconds for rangefinders, 5s for FCS. Think of way to delay this.
                MyAPIGateway.Utilities.ShowNotification($"Scan ready in {timeToNextScan:n2} second(s)", 5000, MyFontEnum.Red);
                return;
            }

            var detectedInfo = camera.Raycast(_scanRange);
            if (detectedInfo.IsEmpty())
            {
                MyAPIGateway.Utilities.ShowNotification("No target found", 5000, MyFontEnum.White);
                return;
            }

            var distance = Vector3D.Distance((Vector3D)detectedInfo.HitPosition, camera.GetPosition());
            MyAPIGateway.Utilities.ShowNotification($"Range to target: {distance:n0} m", 5000, MyFontEnum.Green);
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CameraBlock), false, "FCSCamNewOffsetRight")]
    public class FCSCamNewOffsetRight : MyGameLogicComponent
    {
        const float _scanRange = 1500f;

        IMyCameraBlock _camera = null;
        static bool _terminalControlsInit = false;

        float _raycastRechargeRate = 1;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;

            try
            {
                InitTerminalControls();

                _camera = (IMyCameraBlock)Entity;

                var cube = (IMyCubeBlock)Entity;
                var def = cube.SlimBlock.BlockDefinition as MyCameraBlockDefinition;
                _raycastRechargeRate = def.RaycastTimeMultiplier * 1000f;
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("Exception in rangefinder init", 10000, MyFontEnum.Red);
                MyLog.Default.WriteLine(e);
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            _camera.EnableRaycast = true;
        }

        void InitTerminalControls()
        {
            if (_terminalControlsInit)
                return;

            IMyTerminalControlButton raycastButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyCameraBlock>("Range");
            raycastButton.Title = MyStringId.GetOrCompute("Range");
            raycastButton.Enabled = x => x.BlockDefinition.SubtypeId.Equals("FCSCamNewOffsetRight");
            raycastButton.Visible = x => x.BlockDefinition.SubtypeId.Equals("FCSCamNewOffsetRight");
            raycastButton.SupportsMultipleBlocks = false;
            raycastButton.Action = x =>
            {
                var y = x.GameLogic.GetAs<FCSCamNewOffsetRight>();
                if (y == null)
                    return;

                var camera = x as IMyCameraBlock;
                if (camera == null)
                    return;

                Raycast(camera);
            };
            MyAPIGateway.TerminalControls.AddControl<IMyCameraBlock>(raycastButton);

            //Recharge toggle action
            IMyTerminalAction raycastAction = MyAPIGateway.TerminalControls.CreateAction<IMyCameraBlock>("Range");
            raycastAction.Action = (x) =>
            {
                var y = x.GameLogic.GetAs<FCSCamNewOffsetRight>();
                if (y == null)
                    return;

                var camera = x as IMyCameraBlock;
                if (camera == null)
                    return;

                Raycast(camera);
            };
            raycastAction.ValidForGroups = true;
            raycastAction.Writer = (x, s) => GetWriter(x, s);
            raycastAction.Icon = @"Textures\GUI\Icons\Actions\Toggle.dds"; //change this
            raycastAction.Enabled = x => x.BlockDefinition.SubtypeId.Equals("FCSCamNewOffsetRight");
            raycastAction.Name = new StringBuilder("Range");
            MyAPIGateway.TerminalControls.AddAction<IMyCameraBlock>(raycastAction);

            _terminalControlsInit = true;
        }

        public void GetWriter(IMyTerminalBlock x, StringBuilder s)
        {
            s.Clear();
            s.Append("Range");
        }

        void Raycast(IMyCameraBlock camera)
        {
            if (!camera.EnableRaycast)
                camera.EnableRaycast = true;

            if (!camera.CanScan(_scanRange))
            {
                var timeToNextScan = 5000;// Used to be this, Math.Max(0, _scanRange - camera.AvailableScanRange) / _raycastRechargeRate, now is two seconds for rangefinders, 5s for FCS. Think of way to delay this.
                MyAPIGateway.Utilities.ShowNotification($"Scan ready in {timeToNextScan:n2} second(s)", 5000, MyFontEnum.Red);
                return;
            }

            var detectedInfo = camera.Raycast(_scanRange);
            if (detectedInfo.IsEmpty())
            {
                MyAPIGateway.Utilities.ShowNotification("No target found", 5000, MyFontEnum.White);
                return;
            }

            var distance = Vector3D.Distance((Vector3D)detectedInfo.HitPosition, camera.GetPosition());
            MyAPIGateway.Utilities.ShowNotification($"Range to target: {distance:n0} m", 5000, MyFontEnum.Green);
        }
    }
}
