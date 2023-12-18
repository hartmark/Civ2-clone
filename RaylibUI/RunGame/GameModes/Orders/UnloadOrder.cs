using System.Linq;
using Civ2engine;
using Civ2engine.Enums;
using Civ2engine.MapObjects;
using Civ2engine.Units;
using Raylib_cs;

namespace RaylibUI.RunGame.GameModes.Orders;

public class UnloadOrder : Order
{

    public UnloadOrder(GameScreen gameScreen, string defaultLabel) : 
        base(gameScreen, KeyboardKey.KEY_U, defaultLabel, 3)
    {
    }

    public override Order Update(Tile activeTile, Unit activeUnit)
    {
        if (activeTile == null || activeUnit == null)
        {
            SetCommandState(OrderStatus.Illegal);
        }
        else
        {
            SetCommandState(activeUnit.CarriedUnits.Count > 0 && activeUnit.CarriedUnits.Any(u=>u.MovePoints > 0) ? OrderStatus.Active : OrderStatus.Disabled);
        }

        return this;
    }

    protected override void Execute(LocalPlayer player)
    {
        player.ActiveUnit.CarriedUnits.ForEach(u =>
        {
            u.Order = OrderType.NoOrders;
            u.InShip = null;
        });
        var next = player.ActiveUnit.CarriedUnits.FirstOrDefault(u=>u.AwaitingOrders);
        player.ActiveUnit.CarriedUnits.Clear();
        player.ActiveUnit = next;
    }
}