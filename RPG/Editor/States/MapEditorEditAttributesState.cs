﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TeamStor.Engine;
using TeamStor.Engine.Graphics;
using TeamStor.Engine.Tween;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SpriteBatch = TeamStor.Engine.Graphics.SpriteBatch;

namespace TeamStor.RPG.Editor.States
{
    public class MapEditorEditAttributesState : MapEditorModeState
    {
        public override bool PauseEditor
        {
            get
            {
                return SelectedTile != new Point(-1, -1);
            }
        }

        /// <summary>
        /// The current selected layer.
        /// </summary>
        public Tile.MapLayer Layer
        {
            get;
            set;
        }

        public Point HoveredTile
        {
            get
            {
                Vector2 mousePos = Input.MousePosition / BaseState.Camera.Zoom;
                mousePos.X -= BaseState.Camera.Transform.Translation.X / BaseState.Camera.Zoom;
                mousePos.Y -= BaseState.Camera.Transform.Translation.Y / BaseState.Camera.Zoom;

                Point point = new Point((int)Math.Floor(mousePos.X / 16), (int)Math.Floor(mousePos.Y / 16));

                if(point.X < 0)
                    point.X = 0;
                if(point.Y < 0)
                    point.Y = 0;

                if(point.X >= BaseState.Map.Width)
                    point.X = BaseState.Map.Width - 1;
                if(point.Y >= BaseState.Map.Height)
                    point.Y = BaseState.Map.Height - 1;

                return point;
            }
        }

        private TileAttributeEditor[] _editors;
        public Point SelectedTile { get; private set; } = new Point(-1, -1);

        public override void OnEnter(GameState previousState)
        {
            BaseState.Buttons.Add("layer-terrain", new Button
            {
                Text = "",
                Icon = Assets.Get<Texture2D>("editor/tile/terrain.png"),
                Position = new TweenedVector2(Game, new Vector2(48, 114)),

                Active = true,
                Clicked = (btn) =>
                {
                    Layer = Tile.MapLayer.Terrain;
                },
                Font = Game.DefaultFonts.Normal
            });

            BaseState.Buttons.Add("layer-decoration", new Button
            {
                Text = "",
                Icon = Assets.Get<Texture2D>("editor/tile/decoration.png"),
                Position = new TweenedVector2(Game, new Vector2(48, 114 + 32)),

                Active = false,
                Clicked = (btn) =>
                {
                    Layer = Tile.MapLayer.Decoration;
                },
                Font = Game.DefaultFonts.Normal
            });

            BaseState.Buttons.Add("layer-npc", new Button
            {
                Text = "",
                Icon = Assets.Get<Texture2D>("editor/tile/npc.png"),
                Position = new TweenedVector2(Game, new Vector2(48, 114 + 32 * 2)),

                Active = false,
                Clicked = (btn) =>
                {
                    Layer = Tile.MapLayer.NPC;
                },
                Font = Game.DefaultFonts.Normal
            });

            BaseState.Buttons.Add("layer-control", new Button
            {
                Text = "",
                Icon = Assets.Get<Texture2D>("editor/tile/control.png"),
                Position = new TweenedVector2(Game, new Vector2(48, 114 + 32 * 3)),

                Active = false,
                Clicked = (btn) =>
                {
                    Layer = Tile.MapLayer.Control;
                },
                Font = Game.DefaultFonts.Normal
            });
        }

        public override void OnLeave(GameState nextState)
        {
            if(SelectedTile != new Point(-1, -1))
                FinalizeEdit();
            
            BaseState.Buttons.Remove("layer-terrain");
            BaseState.Buttons.Remove("layer-decoration");
            BaseState.Buttons.Remove("layer-npc");
            BaseState.Buttons.Remove("layer-control");
        }

        public override string CurrentHelpText
        {
            get
            {
                if(!BaseState.Buttons["layer-terrain"].Active && !BaseState.Buttons["layer-terrain"].Disabled && BaseState.Buttons["layer-terrain"].Rectangle.Contains(Input.MousePosition))
                    return "Select the terrain layer";
                if(!BaseState.Buttons["layer-decoration"].Active && !BaseState.Buttons["layer-decoration"].Disabled && BaseState.Buttons["layer-decoration"].Rectangle.Contains(Input.MousePosition))
                    return "Select the decoration layer";
                if(!BaseState.Buttons["layer-npc"].Active && !BaseState.Buttons["layer-npc"].Disabled && BaseState.Buttons["layer-npc"].Rectangle.Contains(Input.MousePosition))
                    return "Select the NPC layer";
                if(!BaseState.Buttons["layer-control"].Active && !BaseState.Buttons["layer-control"].Disabled && BaseState.Buttons["layer-control"].Rectangle.Contains(Input.MousePosition))
                    return "Select the control layer";

                return "";
            }
        }

        private void FinalizeEdit()
        {
            SortedDictionary<string, string> newAttribs = new SortedDictionary<string, string>();
            foreach(TileAttributeEditor editor in _editors)
            {
                if(!editor.IsDefaultValue)
                    newAttribs.Add(editor.Name, editor.ValueAsString);
                editor.Dispose();
            }

            if(newAttribs.Count == 0)
                newAttribs = null;
            
            BaseState.Map.SetMetadata(Layer, SelectedTile.X, SelectedTile.Y, newAttribs);
            
            _editors = null;
            SelectedTile = new Point(-1, -1);
        }

        public override void Update(double deltaTime, double totalTime, long count)
        {
            BaseState.Buttons["layer-terrain"].Active = Layer == Tile.MapLayer.Terrain;
            BaseState.Buttons["layer-decoration"].Active = Layer == Tile.MapLayer.Decoration;
            BaseState.Buttons["layer-npc"].Active = Layer == Tile.MapLayer.NPC;
            BaseState.Buttons["layer-control"].Active = Layer == Tile.MapLayer.Control;

            BaseState.Buttons["layer-terrain"].Disabled = SelectedTile != new Point(-1, -1);
            BaseState.Buttons["layer-decoration"].Disabled = SelectedTile != new Point(-1, -1);
            BaseState.Buttons["layer-npc"].Disabled = SelectedTile != new Point(-1, -1);
            BaseState.Buttons["layer-control"].Disabled = SelectedTile != new Point(-1, -1);

            if(Game.Input.KeyPressed(Keys.T))
            {
                MapEditorTileEditState state = new MapEditorTileEditState();
                state.Layer = Layer;
                BaseState.CurrentState = state;
                
                return;
            }

            if(Game.Input.MousePressed(MouseButton.Left) && !BaseState.IsPointObscured(Input.MousePosition) && SelectedTile == new Point(-1, -1))
            {
                bool canCurrentTileHaveMetadata = Tile.Find(BaseState.Map[Layer, HoveredTile.X, HoveredTile.Y], Layer).HasEditableAttributes;

                if(canCurrentTileHaveMetadata)
                {
                    SelectedTile = HoveredTile;
                    SortedDictionary<string, string> attribs = BaseState.Map.GetMetadata(Layer, SelectedTile.X, SelectedTile.Y);

                    int currentY = Game.GraphicsDevice.Viewport.Bounds.Height / 2 - 200 / 2 + 30;
                    _editors = Tile.Find(BaseState.Map[Layer, SelectedTile.X, SelectedTile.Y], Layer).AttributeEditors(BaseState, ref currentY);

                    if(attribs != null)
                    {
                        foreach(TileAttributeEditor editor in _editors)
                        {
                            if(attribs.ContainsKey(editor.Name))
                                editor.ValueFromExistingTile(attribs[editor.Name]);
                        }
                    }
                }
            }
        }

        public override void FixedUpdate(long count)
        {

        }

        public override void Draw(SpriteBatch batch, Vector2 screenSize)
        {
            batch.SamplerState = SamplerState.PointWrap;

            if(SelectedTile != new Point(-1, -1))
            {
                batch.Rectangle(new Rectangle(Point.Zero, screenSize.ToPoint()), Color.Black * 0.6f);
            }

            if(!BaseState.IsPointObscured(Input.MousePosition) && SelectedTile == new Point(-1, -1))
            {
                batch.Transform = BaseState.Camera.Transform;

                bool canCurrentTileHaveMetadata = Tile.Find(BaseState.Map[Layer, HoveredTile.X, HoveredTile.Y], Layer).HasEditableAttributes;
                SortedDictionary<string, string> attribs = BaseState.Map.GetMetadata(Layer, HoveredTile.X, HoveredTile.Y);
                batch.Outline(new Rectangle(HoveredTile.X * 16, HoveredTile.Y * 16, 16, 16),
                    canCurrentTileHaveMetadata ? Color.White * 0.6f : Color.DarkRed * 0.6f, 1, false);

                batch.Reset();

                string tileName = Tile.Find(BaseState.Map[Layer, HoveredTile.X, HoveredTile.Y], Layer).Name(BaseState.Map.GetMetadata(Layer, HoveredTile.X, HoveredTile.Y), BaseState.Map.Info.Environment);
                string str = "";
                if(canCurrentTileHaveMetadata)
                {
                    if(attribs == null)
                        str = "Edit \"" + tileName + "\" (" + HoveredTile.X + ", " + HoveredTile.Y + ") [0.00 KB, no attributes set]";
                    else
                    {
                        int size = 0;
                        unsafe
                        {
                            // https://stackoverflow.com/questions/1128315/find-size-of-object-instance-in-bytes-in-c-sharp
                            RuntimeTypeHandle th = attribs.GetType().TypeHandle;
                            size = *(*(int**)&th + 1);
                        }

                        str = "Edit \"" + tileName + "\" (" + HoveredTile.X + ", " + HoveredTile.Y + ") [" + (size / (double) 1024).ToString("0.00") + " KB, " + attribs.Count + " attributes set]";
                    }
                }
                else
                    str = "No attributes exist for \"" + tileName + "\"";

                Vector2 pos = new Vector2(HoveredTile.X * 16, HoveredTile.Y * 16) * BaseState.Camera.Zoom + BaseState.Camera.Translation -
                              new Vector2(0, 12 * BaseState.Camera.Zoom);

                if(pos.X < 5 * BaseState.Camera.Zoom)
                    pos.X = 5 * BaseState.Camera.Zoom;

                if(pos.Y < 5 * BaseState.Camera.Zoom)
                {
                    pos.Y = 5 * BaseState.Camera.Zoom;
                    pos.X += 3 * BaseState.Camera.Zoom;
                }

                batch.Text(
                    SpriteBatch.FontStyle.MonoBold,
                    (uint)(8 * BaseState.Camera.Zoom),
                    str,
                    pos,
                    canCurrentTileHaveMetadata ? Color.White * 0.6f : Color.DarkRed * 0.6f);
            }
        }
    }
}