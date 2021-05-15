﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eto.Drawing;
using Eto.Forms;
using Civ2engine;
using Civ2engine.Events;

namespace EtoFormsUI
{
    public partial class Main : Form
    {
        public void LoadGameInitialization(string directoryPath, string SAVname)
        {
            Game.LoadGame(directoryPath, SAVname);
            Images.LoadGraphicsAssetsFromFiles(directoryPath);
        }

        public void LoadScenarioInit(string directoryPath, string SCNname)
        {
            
        }
        
        public void StartPremadeInit(string directoryPath, string SCNname)
        {
            
        }

        public void StartGame()
        {
            // Generate map tile graphics
            Images.MapTileGraphic = new Bitmap[Map.Xdim, Map.Ydim];
            for (int col = 0; col < Map.Xdim; col++)
            {
                for (int row = 0; row < Map.Ydim; row++)
                {
                    Images.MapTileGraphic[col, row] = Draw.MakeTileGraphic(Map.Tile[col, row], col, row, Game.Options.FlatEarth);
                }
            }
            
            foreach (MenuItem item in this.Menu.Items) item.Enabled = true;

            mapPanel = new MapPanel(this, ClientSize.Width - 262, ClientSize.Height);
            layout.Add(mapPanel, 0, 0);

            minimapPanel = new MinimapPanel(this, 262, 149);
            layout.Add(minimapPanel, ClientSize.Width - 262, 0);

            statusPanel = new StatusPanel(this, 262, ClientSize.Height - 148);
            layout.Add(statusPanel, ClientSize.Width - 262, 148);

            Content = layout;

            BringToFront();
        }

        private void NewGame(bool customizeWorld)
        {
            var rulesFiles = LocateRules(Settings.SearchPaths);
            var selectedRulesPath = rulesFiles[0].Item2; 
            if (rulesFiles.Count > 1)
            {
                var popupBox = new Civ2dialogV2(this,
                    new PopupBox
                    {
                        Title = "Select game version", Options = rulesFiles.Select(f => f.Item1).ToList(),
                        Button = new List<string> {"OK", "Cancel"}
                    });
                popupBox.ShowModal(Parent);

                if (popupBox.SelectedIndex == int.MinValue)
                {
                    OnPopupboxEvent?.Invoke(null, new PopupboxEventArgs("MAINMENU"));
                    return;
                }

                selectedRulesPath = rulesFiles[popupBox.SelectedIndex].Item2;
            }

            var worldSizeDialog = new Civ2dialogV2(this, popupBoxList.Find(b => b.Name == "SIZEOFMAP"));
            
            worldSizeDialog.ShowModal(Parent);                
            if (worldSizeDialog.SelectedIndex == int.MinValue)
            {
                OnPopupboxEvent?.Invoke(null, new PopupboxEventArgs("MAINMENU"));
                return;
            }

            int[] worldSize = worldSizeDialog.SelectedIndex switch
            {
                1 => new[] {50, 80},
                2 => new[] {75, 120},
                _ => new[] {40, 50}
            };
            if (worldSizeDialog.SelectedButton == "Custom")
            {
                var customSize = new Civ2dialogV2(this, popupBoxList.Find(b => b.Name == "CUSTOMSIZE"), textBoxes: new List<TextBoxDefinition>
                {
                    new TextBoxDefinition
                    {
                        index = 3, Name = "Width", Numeric = true, InitialValue = worldSize[0].ToString()
                    },  new TextBoxDefinition
                    {
                    index = 4, Name = "Height", Numeric = true, InitialValue = worldSize[1].ToString()
                    }
                } );
                
                customSize.ShowModal();
                if (int.TryParse(customSize.TextValues["Width"], out var width))
                {
                    worldSize[0] = width;
                }
                if (int.TryParse(customSize.TextValues["Height"], out var height))
                {
                    worldSize[1] = height;
                }
            }
        }

        private IList<Tuple<string, string>> LocateRules(params string[] searchPaths)
        {
            var foundRules = new List<Tuple<string, string>>();
            foreach (var searchPath in searchPaths)
            {
                var rules = searchPath + Path.DirectorySeparatorChar + "rules.txt";
                if (File.Exists(rules))
                {
                    var game = searchPath + Path.DirectorySeparatorChar + "game.txt";
                    var name = "Default";
                    if (File.Exists(game))
                    {
                        foreach (var line in File.ReadLines(game))

                        {
                            if (!line.StartsWith("@title")) continue;
                            name = line[7..];
                            break;
                        }
                    }

                    foundRules.Add(Tuple.Create(name, rules));
                }
            }

            return foundRules;
        }
    }
}
