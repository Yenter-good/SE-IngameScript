using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        private Dictionary<string, PairDoor> _pairDoors = new Dictionary<string, PairDoor>();
        private Dictionary<IMyDoor, string> _mustCloseDoor = new Dictionary<IMyDoor, string>();

        public Program()
        {
            InitPairDoor();
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            foreach (var keyvalue in _pairDoors)
            {
                var pairDoor = keyvalue.Value;
                if (pairDoor.ExceptStatus)
                {
                    pairDoor.Door1.CloseDoor();
                    pairDoor.Door2.CloseDoor();
                    _mustCloseDoor[pairDoor.ExceptDoor] = keyvalue.Key;
                }
            }

            foreach (var key in _mustCloseDoor.Keys)
            {
                if (key.Status == DoorStatus.Closed)
                {
                    var pairDoor = _pairDoors[_mustCloseDoor[key]];
                    if (pairDoor.Door1 == key)
                        pairDoor.Door2.OpenDoor();
                    else
                        pairDoor.Door1.OpenDoor();
                    _mustCloseDoor.Remove(key);
                }
            }
        }

        /// <summary>
        /// 初始化门的配对关系
        /// </summary>
        private void InitPairDoor()
        {
            List<IMyDoor> allDoors = new List<IMyDoor>();
            GridTerminalSystem.GetBlocksOfType(allDoors);

            foreach (var door in allDoors)
            {
                var name = door.DisplayNameText.ToUpper();
                if (name.Contains("PAIR"))
                {
                    var groupName = name.Substring(name.LastIndexOf("PAIR"));
                    if (!_pairDoors.ContainsKey(groupName))
                        _pairDoors[groupName] = new PairDoor() { Door1 = door };
                    else
                    {
                        if (_pairDoors[groupName].Door1 != null)
                            _pairDoors[groupName].Door2 = door;
                        else
                            _pairDoors[groupName].Door1 = door;
                    }
                }
            }
        }

        class PairDoor
        {
            public IMyDoor Door1 { get; set; }
            public IMyDoor Door2 { get; set; }

            public IMyDoor ExceptDoor { get; private set; }

            public bool ExceptStatus
            {
                get
                {
                    if (Door1.Status == DoorStatus.Opening && Door2.Status != DoorStatus.Closed)
                    {
                        ExceptDoor = Door2;
                        return true;
                    }
                    else if (Door2.Status == DoorStatus.Opening && Door1.Status != DoorStatus.Closed)
                    {
                        ExceptDoor = Door1;
                        return true;
                    }
                    return false;
                }
            }
        }
    }
}
