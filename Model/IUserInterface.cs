﻿using Civ2engine;
using Civ2engine.IO;
using Model.Images;
using Model.ImageSets;
using Model.InterfaceActions;
using Model.Interface;
using RaylibUI;
using Raylib_cs;
using System.Numerics;

namespace Model;

public interface IUserInterface
{
    bool CanDisplay(string? title);
    InterfaceStyle Look { get; }
    string Title { get; }
    void Initialize();
    IInterfaceAction ProcessDialog(string dialogName, DialogResult dialogResult);
    IInterfaceAction GetInitialAction();
    
    IImageSource? BackgroundImage { get; }
    int GetCityIndexForStyle(int cityStyleIndex, City city, int citySize);
    void LoadPlayerColours();

    Padding GetPadding(float headerLabelHeight, bool footer);

    List<TerrainSet> TileSets { get; }
    
    CityImageSet CityImages { get; }
    
    UnitSet UnitImages { get; }

    PlayerColour[] PlayerColours { get; }

    CommonMapImageSet MapImages { get; }
    int DefaultDialogWidth { get; }
    bool IsButtonInOuterPanel { get; }
    Padding DialogPadding { get; }
    IList<DropdownMenuContents> ConfigureGameCommands(IList<IGameCommand> commands);
    
    CityWindowLayout GetCityWindowDefinition();

    IList<ResourceImage> ResourceImages { get; }
    PopupBox? GetDialog(string dialogName);

    UnitShield UnitShield(int unitType);
    void DrawBorderWallpaper(Wallpaper wallpaper, ref Image destination, int height, int width, Padding padding, bool statusPanel);
    void DrawBorderLines(ref Image destination, int height, int width, Padding padding, bool statusPanel);
    void DrawButton(Texture2D texture, int x, int y, int w, int h);
}