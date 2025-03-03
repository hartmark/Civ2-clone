using System.Numerics;
using System.Security.Cryptography;
using Civ2engine;
using Civ2engine.MapObjects;
using Civ2engine.Terrains;
using Civ2engine.UnitActions.Move;
using Model;
using Model.Menu;
using Raylib_cs;
using RaylibUI.BasicTypes.Controls;
using RaylibUI.RunGame.GameControls;
using RaylibUI.RunGame.GameControls.Mapping;
using RaylibUI.RunGame.GameControls.Mapping.Views;

namespace RaylibUI.RunGame.GameModes;

public class MovingPieces : IGameMode
{
    private readonly GameScreen _gameScreen;
    private readonly LabelControl _title;

    public MovingPieces(GameScreen gameScreen)
    {
        _title = new LabelControl(gameScreen,  Labels.For(LabelIndex.MovingUnits), true, alignment: TextAlignment.Center);

        _gameScreen = gameScreen;
        Actions = new Dictionary<Shortcut, Action>
        {
            /*{
                Keys.Enter, () =>
                {
                    if (main.StatusPanel.WaitingAtEndOfTurn) main.StatusPanel.End_WaitAtEndOfTurn();
                }
            },*/

            {new Shortcut(KeyboardKey.Kp7), MovementFunctions.TryMoveNorthWest}, {new Shortcut(KeyboardKey.Kp8), MovementFunctions.TryMoveNorth},
            {new Shortcut(KeyboardKey.Kp9), MovementFunctions.TryMoveNorthEast},
            {new Shortcut(KeyboardKey.Kp1), MovementFunctions.TryMoveSouthWest}, {new Shortcut(KeyboardKey.Kp2), MovementFunctions.TryMoveSouth},
            {new Shortcut(KeyboardKey.Kp3), MovementFunctions.TryMoveSouthEast},
            {new Shortcut(KeyboardKey.Kp4), MovementFunctions.TryMoveWest}, {new Shortcut(KeyboardKey.Kp6), MovementFunctions.TryMoveEast},

            {new Shortcut(KeyboardKey.Up), MovementFunctions.TryMoveNorth}, {new Shortcut(KeyboardKey.Down), MovementFunctions.TryMoveSouth},
            {new Shortcut(KeyboardKey.Left), MovementFunctions.TryMoveWest}, {new Shortcut(KeyboardKey.Right), MovementFunctions.TryMoveEast},
        };
    }

    public Dictionary<Shortcut, Action> Actions { get; set; }

    public IGameView GetDefaultView(GameScreen gameScreen, IGameView? currentView, int viewHeight, int viewWidth,
        bool forceRedraw)
    {
        if (!forceRedraw && currentView is UnitReadyView animation)
        {
            if (animation.ViewWidth == viewWidth && animation.ViewHeight == viewHeight && animation.Unit == gameScreen.Player.ActiveUnit)

            {
                animation.Reset();
                return animation;
            }
        }

        _gameScreen.StatusPanel.Update();
        return new UnitReadyView(gameScreen, currentView, viewHeight, viewWidth, gameScreen.Player.ActiveUnit, forceRedraw);
    }

    public bool MapClicked(Tile tile, MouseButton mouseButton, bool longClick)
    {
        if (mouseButton == MouseButton.Left)
        {
            //TODO: port GOTO support
            // if (longClick && (e.Modifiers & Keys.Control) != Keys.Control)
            // {
            //     var unit = _player.ActiveUnit;
            //     var path = Path.CalculatePathBetween(_game, _player.ActiveTile, clickedXy, unit.Domain, unit.MaxMovePoints,
            //         unit.Owner, unit.Alpine, unit.IgnoreZonesOfControl);
            //     if (path != null)
            //     {
            //         unit.GoToX = clickedXy.X;
            //         unit.GoToY = clickedXy.Y;
            //         unit.Order = OrderType.GoTo;
            //         path.Follow(_game, unit);
            //         if (!unit.AwaitingOrders)
            //         {
            //             _game.ChooseNextUnit();
            //         }
            //         return false;
            //     }
            // }
            var city = tile.CityHere;
            if (city == null)
            {
                return _gameScreen.ActivateUnits(tile);
            }

            _gameScreen.ShowCityWindow(city);
        }
        else
        {
            _gameScreen.Game.ActiveTile = tile;
            _gameScreen.ActiveMode = _gameScreen.ViewPiece;
        }

        return true;
    }

    public bool HandleKeyPress(Shortcut command)
    {
        if (Actions.ContainsKey(command))
        {
            Actions[command]();
            return true;
        }

        return false;
    }

    public bool Activate()
    {
        if (_gameScreen.Player != _gameScreen.Game.ActivePlayer)
        {
            return false;
        }
            
        if (_gameScreen.Player.ActiveUnit is not {MovePoints: > 0})
        {
            _gameScreen.Game.ChooseNextUnit();
        }
        return _gameScreen.Player.ActiveUnit != null;
    }

    public void PanelClick()
    {
        _gameScreen.ActiveMode = _gameScreen.ViewPiece;
    }

    public IList<IControl> GetSidePanelContents(Rectangle bounds)
    {
        var controls = new List<IControl> { _title };
        _title.Bounds = bounds with { Height = _title.GetPreferredHeight() };

        var activeUnit = _gameScreen.Player.ActiveUnit;
        var unitDisplay = new UnitDisplay(_gameScreen, activeUnit,
            new Vector2(bounds.X, bounds.Y + _title.Height), _gameScreen.Main.ActiveInterface);
        controls.Add(unitDisplay);

        var currentY = bounds.Y + controls.Sum(c => c.Height);
        
        // Show move points correctly
        var commonMultiplier = _gameScreen.Game.Rules.Cosmic.MovementMultiplier;
        var remainingFullPoints = activeUnit.MovePoints / commonMultiplier;
        var fractionalMove = activeUnit.MovePoints % commonMultiplier;

        string moveText;
        if (fractionalMove > 0)
        {
            var gcf = Utils.GreatestCommonFactor(fractionalMove, commonMultiplier);
            moveText =
                $"{Labels.For(LabelIndex.Moves)}: {(remainingFullPoints > 0 ? remainingFullPoints : "")} {fractionalMove / gcf}/{commonMultiplier / gcf}";
        }
        else
        {
            moveText = $"{Labels.For(LabelIndex.Moves)}: {remainingFullPoints}";
        }

        var labelHeight = 18;
        controls.Add(new LabelControl(_gameScreen, moveText, true)
        {
            Bounds = bounds with { Y = currentY, Height = labelHeight }
        });
        currentY += labelHeight;

        // Show other unit info
        var cityName = (activeUnit.HomeCity == null) ? Labels.For(LabelIndex.NONE) : activeUnit.HomeCity.Name;
        controls.Add(
            new ControlGroup(_gameScreen)
            {
                Children =
                {
                    new LabelControl(_gameScreen, cityName, true),
                    new LabelControl(_gameScreen, _gameScreen.Player.Civilization.Adjective, true,
                        alignment: TextAlignment.Center)
                },
                Bounds = bounds with { Y = currentY, Height = labelHeight }
            });
        currentY += labelHeight;


        controls.Add(new LabelControl(_gameScreen,
            activeUnit.Veteran ? $"{activeUnit.Name} ({Labels.For(LabelIndex.Veteran)})" : activeUnit.Name, true)
        {
            Bounds = bounds with { Y = currentY, Height = labelHeight }
        });
        currentY += labelHeight;

        var activeTile = _gameScreen.Player.ActiveTile;
        controls.Add(new LabelControl(_gameScreen, $"({activeTile.Name})", true)
        {
            Bounds = bounds with { Y = currentY, Height = labelHeight }
        });
        currentY += labelHeight;

        // If road/railroad/irrigation/farmland/mine present
        var improvements = activeTile.Improvements.Select(c => new
            { Imp = _gameScreen.Game.TerrainImprovements[c.Improvement], Const = c }).ToList();

        var improvementText = string.Join(", ",
            improvements.Where(i => i.Imp.ExclusiveGroup != ImprovementTypes.DefenceGroup && !i.Imp.Negative)
                .Select(i => i.Imp.Levels[i.Const.Level].Name));

        if (!string.IsNullOrWhiteSpace(improvementText))
        {
            controls.Add(new LabelControl(_gameScreen, $"({improvementText})", true)
            {
                Bounds = bounds with { Y = currentY, Height = labelHeight }
            });
            currentY += labelHeight;
        }

        // If airbase/fortress present
        if (improvements.Any(i => i.Imp.ExclusiveGroup == ImprovementTypes.DefenceGroup))
        {
            var airbaseText = string.Join(", ",
                improvements.Where(i => i.Imp.ExclusiveGroup == ImprovementTypes.DefenceGroup)
                    .Select(i => i.Imp.Levels[i.Const.Level].Name));
            controls.Add(new LabelControl(_gameScreen, $"({airbaseText})", true)
            {
                Bounds = bounds with { Y = currentY, Height = labelHeight }
            });
            currentY += labelHeight;
        }

        // If pollution present
        var pollutionText = string.Join(", ",
            improvements.Where(i => i.Imp.Negative)
                .Select(i => i.Imp.Levels[i.Const.Level].Name));
        if (!string.IsNullOrWhiteSpace(pollutionText))
        {
            controls.Add(new LabelControl(_gameScreen, $"({pollutionText})", true)
            {
                Bounds = bounds with { Y = currentY, Height = labelHeight }
            });
            currentY += labelHeight;
        }

        var unitsOnTile = activeTile.UnitsHere.Where(u => u != activeUnit).ToList();
        var remainingSpace = bounds.Height - controls.Sum(c => c.Height);
        for (int i = 0; i < unitsOnTile.Count && remainingSpace > unitDisplay.Height; i++)
        {
            var unit = unitsOnTile[i];
            var unitImage = new UnitDisplay(_gameScreen, unit,
                new Vector2(bounds.X, currentY), _gameScreen.Main.ActiveInterface);
            controls.Add(unitImage);
            currentY += unitImage.Height + 2;
            cityName = (unit.HomeCity == null) ? Labels.For(LabelIndex.NONE) : unit.HomeCity.Name;
            controls.Add(new LabelControl(_gameScreen, cityName, true)
            {
                Bounds = new Rectangle(unitImage.Location.X + 80, unitImage.Location.Y,bounds.Width - 80 , 18)
            });
            controls.Add(new LabelControl(_gameScreen, _gameScreen.Game.Order2String(unit.Order), true)
            {
                Bounds = new Rectangle(unitImage.Location.X + 80, unitImage.Location.Y + 18,bounds.Width - 80 , 18)
            });
            controls.Add(new LabelControl(_gameScreen, unit.Veteran ? $"{unit.Name} ({Labels.For(LabelIndex.Veteran)})" : unit.Name, true)
            {
                Bounds = new Rectangle(unitImage.Location.X + 80, unitImage.Location.Y + 36,bounds.Width - 80 , 18)
            });
        }

        controls.ForEach(c => c.OnResize());
        return controls;
    }
}