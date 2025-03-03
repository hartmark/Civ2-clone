﻿using System;
using System.Collections.Generic;

namespace Civ2engine
{
    public class PopupBoxReader : IFileHandler
    {
        // Read Game.txt
        public static Dictionary<string, PopupBox?> LoadPopupBoxes(string root)
        {
            var boxes = new Dictionary<string, PopupBox?>();
            var filePath = Utils.GetFilePath("game.txt", new[] { root });
            TextFileParser.ParseFile(filePath, new PopupBoxReader { Boxes = boxes }, true);

            // Add this two manually
            boxes.Add("SCENCHOSECIV", new PopupBox()
            {
                Options = new List<string> { "a", "b" },
                Button = new List<string> { Labels.Ok, Labels.Cancel },
                Title = "    ",
                Name = "SCENCHOSECIV",
                Width = 457,
            });
            boxes.Add("SCENINTRO", new PopupBox()
            {
                Button = new List<string> { Labels.Ok, Labels.Cancel },
                Name = "SCENINTRO",
                Title = "",
            });
            boxes.Add("SCENDIFFICULTY", new PopupBox()
            {
                Button = new List<string> { Labels.Ok, Labels.Cancel },
                Options = new List<string> { "Chieftain (easiest)", "Warlord",
                "Prince", "King", "Emperor", "Deity (toughest)"},
                Title = "Select Difficulty Level",
                Name = "SCENDIFFICULTY",
                Width = 320
            });
            boxes.Add("SCENGENDER", new PopupBox()
            {
                Button = new List<string> { Labels.Ok, Labels.Cancel },
                Options = new List<string> { "Male", "Female" },
                Title = "Select Gender",
                Name = "SCENGENDER",
                Width = 320
            });
            boxes.Add("SCENENTERNAME", new PopupBox()
            {
                Button = new List<string> { Labels.Ok, Labels.Cancel },
                Title = "Please enter your name",
                Name = "SCENENTERNAME",
                Width = 440
            });
            return boxes;
        }

        private Dictionary<string, PopupBox?> Boxes { get; set; }

        public void ProcessSection(string section, List<string> contents)
        {
            var popupBox = new PopupBox {Name = section, Checkbox = false};
            Action<string> contentHandler;
            var addOkay = true;

            void TextHandler(string line)
            {
                if (string.IsNullOrWhiteSpace(line) && popupBox.Text?.Count > 0 && popupBox.Options == null)
                {
                    contentHandler = (line) =>
                    {
                        if (string.IsNullOrWhiteSpace(line)) return;
                        (popupBox.Options ??= new List<string>()).Add(line);
                    };
                    return;
                }

                popupBox.AddText(line);
            }

            contentHandler = TextHandler;

            var optionsHandler = new Action<string>((line) =>
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    contentHandler = TextHandler;
                }
                else
                {
                    (popupBox.Options ??= new List<string>()).Add(line);
                }
            });
            var optionsStart = false;
            foreach (var line in contents)
            {
                if (line.StartsWith("@"))
                {
                    var parts = line.Split(new[] { '@', '=' }, 2,
                        StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0)
                    {
                        continue;
                    }

                    switch (parts[0])
                    {
                        case "width":
                            popupBox.Width = int.Parse(parts[1]);
                            break;
                        case "title":
                            popupBox.Title = parts[1];
                            break;
                        case "default":
                            popupBox.Default = int.Parse(parts[1]);
                            break;
                        case "x":
                            popupBox.X = int.Parse(parts[1]);
                            break;
                        case "y":
                            popupBox.Y = int.Parse(parts[1]);
                            break;
                        case "options":
                            contentHandler = optionsHandler;
                            break;
                        case "checkbox":
                            popupBox.Checkbox = true;
                            break;
                        case "listbox":
                            popupBox.Listbox = true;
                            popupBox.ListboxLines = parts.Length > 1 ? int.Parse(parts[1]) : 16;
                            break;
                        case "button":
                            (popupBox.Button ??= new List<string>()).Add(parts[1]);
                            break;
                    }
                }

                else
                {
                    contentHandler(line);
                }
            }

            popupBox.Button ??= new List<string>();
            popupBox.Button.Add("OK");
            // Add cancel buttons if @options exist
            if (popupBox.Options != null)
            {
                popupBox.Button.Add("Cancel");
            }


            Boxes[popupBox.Name] = popupBox;

        }
    }
}
