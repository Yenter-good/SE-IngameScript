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
        private List<VRage.Game.ModAPI.Ingame.IMyInventory> _allInventories = new List<VRage.Game.ModAPI.Ingame.IMyInventory>();

        private Dictionary<string, string> _translate = new Dictionary<string, string>();

        private int _delayTick = 0;
        private const int DELAY_TIME = 5;

        private Vector2 _vectorTmp = new Vector2(0, 0);
        private List<IMyTextSurface> _textSurfaces;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            List<IMyCargoContainer> allCargos = new List<IMyCargoContainer>();
            GridTerminalSystem.GetBlocksOfType(allCargos);
            foreach (var cargo in allCargos)
            {
                var inventory = cargo.GetInventory();
                if (inventory != null)
                    _allInventories.Add(inventory);
            }

            List<IMyRefinery> allRefineries = new List<IMyRefinery>();
            List<IMyAssembler> allAssemblers = new List<IMyAssembler>();
            List<IMyGasGenerator> allOxygenGenerators = new List<IMyGasGenerator>();
            GridTerminalSystem.GetBlocksOfType(allRefineries);
            GridTerminalSystem.GetBlocksOfType(allAssemblers);
            GridTerminalSystem.GetBlocksOfType(allOxygenGenerators);
            allRefineries.ForEach(p =>
            {
                var inventory = p.GetInventory();
                if (inventory != null)
                    _allInventories.Add(inventory);
            });
            allAssemblers.ForEach(p =>
            {
                var inventory = p.GetInventory();
                if (inventory != null)
                    _allInventories.Add(inventory);
            });
            allOxygenGenerators.ForEach(p =>
            {
                var inventory = p.GetInventory();
                if (inventory != null)
                    _allInventories.Add(inventory);
            });

            List<IMyTextPanel> allTextPanels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType(allTextPanels);
            foreach (var textPanel in allTextPanels)
            {
                var name = textPanel.DisplayNameText;
                if (name.Contains("材料"))
                    _textSurfaces.Add(textPanel as IMyTextSurface);
            }

            _translate["Cobalt"] = "钴";
            _translate["Gold"] = "金";
            _translate["Stone"] = "石头";
            _translate["Iron"] = "铁";
            _translate["Magnesium"] = "镁";
            _translate["Nickel"] = "镍";
            _translate["Scrap"] = "废金属";
            _translate["Platinum"] = "铂金";
            _translate["Silicon"] = "硅";
            _translate["Silver"] = "银";
            _translate["Uranium"] = "铀";
            _translate["Ice"] = "冰";
            _translate["Organic"] = "有机物";
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (_delayTick++ > DELAY_TIME)
                _delayTick = 0;
            else
                return;

            SortedDictionary<string, long> ingotsCount = new SortedDictionary<string, long>();
            SortedDictionary<string, long> oresCount = new SortedDictionary<string, long>();

            this.GetInventoryCount(ingotsCount, oresCount);

            foreach (var surface in _textSurfaces)
            {
                var frame = surface.DrawFrame();
                var splitLines = this.GetSplitLine(surface.SurfaceSize.X, surface.SurfaceSize.Y);
                frame.AddRange(splitLines);

                float x = 2, y = 2;
                foreach (var ingot in ingotsCount)
                {
                    var translateName = _translate[ingot.Key];
                    if (translateName == "石头")
                        translateName = "沙石";
                    else
                        translateName += "锭";
                    translateName += "：" + (ingot.Value / 1000000).ToString() + "千克";

                    frame.Add(this.GetTextSprite(translateName, x, y));
                    y += 20;
                }

                x += surface.SurfaceSize.Y / 2;
                y = 2;
                foreach (var ore in oresCount)
                {
                    var translateName = _translate[ore.Key];
                    if (translateName != "石头" && translateName != "废金属")
                        translateName += "矿";
                    translateName += "：" + (ore.Value / 1000000).ToString() + "千克";

                    frame.Add(this.GetTextSprite(translateName, x, y));
                    y += 20;
                }

                frame.Dispose();
            }
        }

        private void GetInventoryCount(SortedDictionary<string, long> ingotsCount, SortedDictionary<string, long> oresCount)
        {

            foreach (var inventory in _allInventories)
            {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                inventory.GetItems(items);
                foreach (var item in items)
                {
                    var typeId = item.Type.TypeId;
                    if (typeId == "MyObjectBuilder_Ingot")
                    {
                        if (ingotsCount.ContainsKey(item.Type.SubtypeId))
                            ingotsCount[item.Type.SubtypeId] += item.Amount.RawValue;
                        else
                            ingotsCount[item.Type.SubtypeId] = item.Amount.RawValue;
                    }
                    else if (typeId == "MyObjectBuilder_Ore")
                    {
                        if (oresCount.ContainsKey(item.Type.SubtypeId))
                            oresCount[item.Type.SubtypeId] += item.Amount.RawValue;
                        else
                            oresCount[item.Type.SubtypeId] = item.Amount.RawValue;
                    }
                }
            }
        }

        private MySprite GetTextSprite(string text, float x, float y)
        {
            _vectorTmp.X = x;
            _vectorTmp.Y = y;
            return new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = text,
                Position = _vectorTmp,
                RotationOrScale = 0.8f,
                Color = Color.Red,
                Alignment = TextAlignment.CENTER,
                FontId = "White"
            };
        }

        private List<MySprite> GetSplitLine(float width, float height)
        {
            List<MySprite> result = new List<MySprite>();

            var sprite1 = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "Grid",
                Color = Color.White.Alpha(0.66f),
                Alignment = TextAlignment.CENTER
            };
            _vectorTmp.X = width / 4;
            _vectorTmp.Y = height / 2;
            sprite1.Position = _vectorTmp;
            _vectorTmp.X = width / 2;
            _vectorTmp.Y = height;
            sprite1.Size = _vectorTmp;

            var sprite2 = new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Data = "Grid",
                Color = Color.White.Alpha(0.66f),
                Alignment = TextAlignment.CENTER
            };
            _vectorTmp.X = width * 3 / 4;
            _vectorTmp.Y = height / 2;
            sprite2.Position = _vectorTmp;
            _vectorTmp.X = width / 2;
            _vectorTmp.Y = height;
            sprite2.Size = _vectorTmp;

            result.Add(sprite1);
            result.Add(sprite2);

            return result;
        }
    }
}
