using System;
using System.Collections.Generic;
using System.Linq;
using Civ2engine.Enums;
using Civ2engine.Events;
using Civ2engine.MapObjects;
using Civ2engine.Terrains;
using Civ2engine.Units;

namespace Civ2engine.UnitActions.Move
{
    public static class MovementFunctions
    {
        public static void TryMoveNorth()
        {
            var instance = Game.Instance;
            if (instance.ActiveUnit.X > 1)
            {
                MoveC2(0, -2);
            }

            CheckForUnitTurnEnded(instance);
        }

        public static void TryMoveNorthEast()
        {
            var game = Game.Instance;
            if (game.ActiveUnit.Y == 0)
            {
                return;
            }
            if (game.ActiveUnit.X < game.CurrentMap.XDimMax-2)
            {
                MoveC2(1, -1);
            }else if (!game.Options.FlatEarth)
            {
                MoveC2(-game.CurrentMap.XDimMax +2 , -1);
            }

            CheckForUnitTurnEnded(game);
        }

        private static void CheckForUnitTurnEnded(Game game)
        {
            if (game.ActiveUnit.MovePoints <= 0 || game.ActiveUnit.Dead)
            {
                game.ChooseNextUnit();
            }
        }

        public static void TryMoveEast()
        {
            var instance = Game.Instance;
            if (instance.ActiveUnit.X < instance.CurrentMap.XDimMax -2)
            {
                MoveC2(2, 0);
            }else if (!instance.Options.FlatEarth)
            {
                MoveC2(-instance.CurrentMap.XDimMax +2 , 0);
            }
            CheckForUnitTurnEnded(instance);
        }
        
        public static void TryMoveSouthEast()
        {
            var instance = Game.Instance;
            if (instance.ActiveUnit.Y < instance.CurrentMap.YDim - 1)
            {
                if (instance.ActiveUnit.X < instance.CurrentMap.XDimMax - 1)
                {
                    MoveC2(1, 1);
                }
                else if (!instance.Options.FlatEarth)
                {
                    MoveC2(-instance.CurrentMap.XDimMax + 2, 1);
                }

            }
            CheckForUnitTurnEnded(instance);
        }
        
        public static void TryMoveSouth()
        {
            var instance = Game.Instance;
            if (instance.ActiveUnit.Y < instance.CurrentMap.YDim-2)
            {
                MoveC2(0, 2);
            }
            CheckForUnitTurnEnded(instance);
        }

        public static void TryMoveSouthWest()
        {
            var instance = Game.Instance;
            if (instance.ActiveUnit.Y < instance.CurrentMap.YDim - 1)
            {
                if (instance.ActiveUnit.X > 0)
                {
                    MoveC2(-1, 1);
                }
                else if (!instance.Options.FlatEarth)
                {
                    MoveC2(instance.CurrentMap.XDimMax - 2, 1);
                }
            }
            CheckForUnitTurnEnded(instance);
        }

        public static void TryMoveWest()
        {
            var instance = Game.Instance;
            if (instance.ActiveUnit.X > 0)
            {
                MoveC2(-2, 0);
            }
            else if(!instance.Options.FlatEarth)
            {
                MoveC2(instance.CurrentMap.XDimMax-2, 0);
            }
            CheckForUnitTurnEnded(instance);
        }

        public static void TryMoveNorthWest()
        {
            var instance = Game.Instance;
            if (instance.ActiveUnit.Y != 0)
            {
                if (instance.ActiveUnit.X > 0)
                {
                    MoveC2(-1, -1);
                }
                else if (!instance.Options.FlatEarth)
                {
                    MoveC2(instance.CurrentMap.XDimMax - 1, -1);
                }
            }
            CheckForUnitTurnEnded(instance);
        }

        public static void MoveC2(int deltaX, int deltaY)
        {
            var game = Game.Instance;
            var unit = game.ActiveUnit;
            if (unit == null)
            {
                throw new NotSupportedException("No unit selected");
            }

            var destX = unit.X + deltaX;
            var destY = unit.Y + deltaY;

            var tileTo = game.CurrentMap.TileC2(destX, destY);
            if (tileTo.UnitsHere.Count > 0 && tileTo.UnitsHere[0].Owner != unit.Owner)
            {
                if (!AttackAtTile(unit, game, tileTo)) return;
            }
            else
            {
                Moveto(game, unit, destX, destY);
            }

            if (unit.Domain == UnitGas.Air && unit.MovePoints == 0)
            {
                //TODO: Air unit out of fuel check
            }
        }

        internal static bool AttackAtTile(Unit unit, Game game, Tile tileTo)
        {
            if (unit.AttackBase == 0)
            {
                game.TriggerUnitEvent(UnitEventType.MovementBlocked, unit, BlockedReason.ZeroAttackStrength);
                return false;
            }


            if (tileTo.CityHere != null)
            {
                //Anything can attack or defend a city
                Attack(game, unit, tileTo);
                return true;
            }

            if (tileTo.Type == TerrainType.Ocean)
            {
                if (unit.Domain == UnitGas.Ground)
                {
                    // Ground units cannot attack into the sea
                    return false;
                }
            }
            else
            {
                if (unit.SubmarineAdvantagesDisadvantages)
                {
                    //Submarines can't attack land
                    return false;
                }
            }

            if (!unit.CanAttackAirUnits && tileTo.UnitsHere.Any(u => u.Domain == UnitGas.Air))
            {
                game.TriggerUnitEvent(UnitEventType.MovementBlocked, unit, BlockedReason.CannotAttackAirUnits);
                return false;
            }

            Attack(game, unit, tileTo);
            return true;
        }

        private static void Attack(Game game, Unit attacker, Tile tile)
        {           

            // Primary defender is the unit with largest defense factor
            var defender = tile.UnitsHere[0];
            var groundDefenceFactor = tile.EffectsList.Where(e => e.Target == ImprovementConstants.GroundDefence).Sum(e=>e.Value);
            var defenseFactor = defender.DefenseFactor(attacker, tile, groundDefenceFactor);
            for (var i = 1; i < tile.UnitsHere.Count; i++)
            {
                var altDefenseFactor = tile.UnitsHere[i].DefenseFactor(attacker, tile, groundDefenceFactor);
                if (altDefenseFactor > defenseFactor)
                {
                    defender = tile.UnitsHere[i];
                    defenseFactor = altDefenseFactor;
                }
            }

            // Calculate odds of attacker winning combat (a round of battle)
            var attackFactor = attacker.AttackFactor(defender);
            if (attacker.MovePoints < game.Rules.Cosmic.MovementMultiplier)
            {
                //if attacker has less than one move point left attack at reduced strength
                attackFactor = attackFactor * attacker.MovePoints / game.Rules.Cosmic.MovementMultiplier;
            }

            //Calculate the firepower of the attacker and defender
            var fpA = attacker.FirepowerBase;
            var fpD = defender.FirepowerBase;

            if (attacker.Domain == UnitGas.Sea && defender.Domain == UnitGas.Ground)
            {
                // When a sea unit attacks a land unit, both units have their firepower reduced to 1
                fpA = 1;
                fpD = 1;
            }else if (attacker.Domain != UnitGas.Sea && defender.Domain == UnitGas.Sea &&
                      tile.Type != TerrainType.Ocean)
            {
                // Caught in port (A sea unit’s firepower is reduced to 1 when it is caught in port (or on a land square) by a land or air unit; The attacking air or land unit’s firepower is doubled)
                fpA *= 2;
                fpD = 1;
            }else if (attacker.Domain == UnitGas.Air && defender.Domain == UnitGas.Air && defender.FuelRange == 0)
            {
                // Helicopters attacked by fighters have firepower reduced to 1
                fpD = 1;
            }
            
            var probAttackerWins = defenseFactor >= attackFactor ? (attackFactor * 8 - 1) / (2 * defenseFactor * 8) : 1 - (defenseFactor * 8 + 1) / (2 * attackFactor * 8);

            // Battle -> Loop through combat rounds until a unit loses its HP
            var random = new Random();
            var combatRoundsAttackerWins = new List<bool>();  // Register combat outcomes
            var attackerHitpoints = new List<int>();  // Register attacker hitpoints in each round
            var defenderHitpoints = new List<int>();  // Register defender hitpoints in each round
            do
            {
                var rand = random.Next(0, 1000) / 1000.0;
                var attackerWinsRound = probAttackerWins > rand;
                attackerHitpoints.Add(attacker.RemainingHitpoints);
                defenderHitpoints.Add(defender.RemainingHitpoints);
                if (attackerWinsRound)
                {
                    defender.HitPointsLost += fpA;
                    combatRoundsAttackerWins.Add(true);
                }
                else 
                { 
                    attacker.HitPointsLost += fpD;
                    combatRoundsAttackerWins.Add(false);
                }
            } while (attacker.RemainingHitpoints > 0 && defender.RemainingHitpoints > 0);

            var attackerWinsBattle = defender.RemainingHitpoints <= 0;

            game.TriggerUnitEvent(new CombatEventArgs(UnitEventType.Attack, attacker, defender, combatRoundsAttackerWins, attackerHitpoints, defenderHitpoints));
            
            if (attackerWinsBattle)
            {
                attacker.MovePointsLost += game.Rules.Cosmic.MovementMultiplier;
                // Defender loses - kill all units on the tile (except if on city & if in fortress/airbase)
                if (tile.CityHere != null || tile.EffectsList.Any(e=>e.Target == ImprovementConstants.NoStackElimination))
                {
                    defender.Dead = true;
                    //_casualties.Add(defender);
                    //_units.Remove(defender);
                }
                else
                {
                    var deadUnits = tile.UnitsHere.ToList();
                    tile.UnitsHere.Clear();
                    foreach (var unit in deadUnits)
                    {
                        unit.Dead = true;
                        //_casualties.Add(unit);
                        //_units.Remove(unit);
                    }
                }
            }
            else
            {
                attacker.Dead = true;
                //_casualties.Add(attacker);
                //_units.Remove(attacker);
            }

        }

        private static void Moveto(Game game, Unit unit, int destX, int destY)
        {  
            var tileFrom = game.CurrentMap.TileC2(unit.X, unit.Y);
            var tileTo = game.CurrentMap.TileC2(destX, destY);
            if (!unit.IgnoreZonesOfControl && !IsFriendlyTile(tileTo, unit.Owner) && IsNextToEnemy(tileFrom, unit.Owner, unit.Domain) && IsNextToEnemy(tileTo, unit.Owner, unit.Domain))
            {
                game.TriggerUnitEvent(UnitEventType.MovementBlocked, unit, BlockedReason.Zoc);
                return;
            }

            if (UnitMoved(game, unit, tileTo, tileFrom))
            {
                game.ActiveTile = tileTo;
                var neighbours = game.CurrentMap.Neighbours(tileTo).Where(n => !n.IsVisible(unit.Owner.Id)).ToList();
                if (neighbours.Count > 0)
                {
                    neighbours.ForEach(n => n.SetVisible(unit.Owner.Id));
                    game.TriggerMapEvent(MapEventType.UpdateMap, neighbours);
                }
                
                game.TriggerUnitEvent(new MovementEventArgs(unit, tileFrom, tileTo));
            }
        }

        internal static bool UnitMoved(Game game, Unit unit, Tile tileTo, Tile tileFrom)
        {
            var isCity = tileTo.IsCityPresent;
            var unitMoved = isCity;
            var cosmicRules = game.Rules.Cosmic;
            var moveCost = cosmicRules.MovementMultiplier;
            switch (unit.Domain)
            {
                case UnitGas.Ground:
                {
                    if (tileTo.Type == TerrainType.Ocean)
                    {
                        //Check if we can board a ship there
                        var availableShip = tileTo.UnitsHere.FirstOrDefault(u =>
                            u.Owner == unit.Owner && u.ShipHold > u.CarriedUnits.Count);
                        if (availableShip != null)
                        {
                            availableShip.CarriedUnits.Add(unit);
                            moveCost = unit.MovePoints;
                            unit.InShip = availableShip;
                            unitMoved = true;
                            unit.Order = OrderType.Sleep;
                        }

                        break;
                    }

                    moveCost *= tileTo.MoveCost;
                    moveCost = MoveCost(tileTo, tileFrom, moveCost, cosmicRules);

                    // If alpine movement could be less use that
                    if (cosmicRules.AlpineMovement < moveCost && unit.Alpine)
                    {
                        moveCost = cosmicRules.AlpineMovement;
                    }

                    unitMoved = true;
                    break;
                }
                case UnitGas.Sea:
                {
                    if (tileTo.Type != TerrainType.Ocean)
                    {
                        if (!isCity && unit.CarriedUnits.Count > 0)
                        {
                            //Make landfall
                            unit.CarriedUnits.ForEach(u =>
                            {
                                u.Order = OrderType.NoOrders;
                                UnitMoved(game, u, tileTo, tileFrom);
                                u.InShip = null;
                            });
                            unit.CarriedUnits.Clear();
                            return true;
                        }

                        break;
                    }

                    if (unit.ShipHold > 0 && unit.CarriedUnits.Count < unit.ShipHold)
                    {
                        if (tileFrom.Terrain.Type == TerrainType.Ocean)
                        {
                            foreach (var unaccountedUnit in tileFrom.UnitsHere
                                .Where(u => u.InShip == null &&
                                            u.Domain == UnitGas.Ground)
                                .Take(unit.ShipHold - unit.CarriedUnits.Count))
                            {
                                unaccountedUnit.InShip = unit;
                                unaccountedUnit.Order = OrderType.Sleep;
                                unit.CarriedUnits.Add(unaccountedUnit);
                            }
                        }
                        else if (tileFrom.IsCityPresent)
                        {
                            foreach (var unaccountedUnit in tileFrom.UnitsHere
                                .Where(u => u.InShip == null &&
                                            u.Domain == UnitGas.Ground && u.Order == OrderType.Sleep)
                                .Take(unit.ShipHold - unit.CarriedUnits.Count))
                            {
                                unaccountedUnit.InShip = unit;
                                unit.CarriedUnits.Add(unaccountedUnit);
                            }
                        }
                    }

                    unitMoved = true;
                    break;
                }
                case UnitGas.Air:
                {
                    if (unit.InShip != null)
                    {
                        unit.InShip.CarriedUnits.Remove(unit);
                        unit.InShip = null;
                    }

                    if (tileTo.EffectsList.Any(e=>e.Target == ImprovementConstants.Airbase) || tileTo.IsCityPresent)
                    {
                        moveCost = unit.MovePoints;
                    }
                    else
                    {
                        var carrier = tileTo.UnitsHere.FirstOrDefault(u =>
                            u.CanCarryAirUnits && u.CarriedUnits.Count < 20);
                        if (carrier != null)
                        {
                            moveCost = unit.MovePoints;
                            carrier.CarriedUnits.Add(unit);
                            unit.InShip = carrier;
                        }
                    }

                    unitMoved = true;
                    break;
                }
                case UnitGas.Special:
                    unitMoved = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // If unit moved, update its X-Y coords
            if (unitMoved)
            {
                unit.MovePointsLost += moveCost;
                // Set previous coords
                unit.PrevXy = new[] { unit.X, unit.Y };

                // Set new coords
                unit.X = tileTo.X;
                unit.Y = tileTo.Y;
                unit.CurrentLocation = tileTo;
                if (unit.CarriedUnits.Count > 0)
                {
                    unit.CarriedUnits.ForEach(u =>
                    {
                        u.PrevXy = unit.PrevXy;
                        u.X = unit.X;
                        u.Y = unit.Y;
                        u.CurrentLocation = tileTo;
                    });
                    if (isCity) //If we're docking activate units carried
                    {
                        unit.CarriedUnits.ForEach(u =>
                        {
                            u.Order = OrderType.NoOrders;
                            u.InShip = null;
                        });
                        unit.CarriedUnits.Clear();
                    }
                }

                if (unit.Order != OrderType.GoTo)
                {
                    unit.Order = OrderType.NoOrders;
                }
            }

            return unitMoved;
        }

        internal static int MoveCost(Tile tileTo, Tile tileFrom, int moveCost, CosmicRules cosmicRules)
        {
            foreach (var movementEffect in tileFrom.EffectsList.Where(e =>
                         e.Target == ImprovementConstants.Movement)
                    )
            {
                var matchingEffect = tileTo.EffectsList.FirstOrDefault(e =>
                    e.Source == movementEffect.Source && e.Target == ImprovementConstants.Movement);
                if (matchingEffect == null) continue;

                if (matchingEffect.Level < movementEffect.Level)
                {
                    if (matchingEffect.Value < moveCost)
                    {
                        moveCost = matchingEffect.Value;
                    }
                }
                else
                {
                    if (movementEffect.Value < moveCost)
                    {
                        moveCost = movementEffect.Value;
                    }
                }
            }

            if (cosmicRules.RiverMovement < moveCost && tileFrom.River && tileTo.River &&
                Math.Abs(tileTo.X - tileFrom.X) == 1 &&
                Math.Abs(tileTo.Y - tileFrom.Y) == 1) //For rivers only for diagonal movement
            {
                moveCost = cosmicRules.RiverMovement;
            }

            return moveCost;
        }

        internal static bool IsFriendlyTile(Tile tileTo, Civilization unitOwner)
        {
            return tileTo.UnitsHere.Any(u => u.Owner == unitOwner) ||
                   (tileTo.CityHere != null && tileTo.CityHere.Owner == unitOwner);
        }

        internal static bool IsNextToEnemy(Tile tile, Civilization civ, UnitGas domain)
        {
            return tile.Neighbours().Any(t => t.UnitsHere.Any(u => u.Owner != civ && u.InShip == null && u.Domain == domain));
        }

        public static IEnumerable<Tile> GetPossibleMoves(Game game, Tile tile, Unit unit)
        {
            var neighbours = unit.Domain switch
            {
                UnitGas.Ground => game.CurrentMap.Neighbours(tile).Where(n => n.Type != TerrainType.Ocean || n.UnitsHere.Any(u=> u.Owner == unit.Owner && u.ShipHold > 0 && u.CarriedUnits.Count < u.ShipHold)),
                UnitGas.Sea => game.CurrentMap.Neighbours(tile).Where(t=> t.CityHere != null || t.Terrain.Type == TerrainType.Ocean || (t.UnitsHere.Count > 0 && t.UnitsHere[0].Owner != unit.Owner)),
                _ => game.CurrentMap.Neighbours(tile)
            };
            if (unit.IgnoreZonesOfControl || !IsNextToEnemy(tile, unit.Owner, unit.Domain))
            {
                return neighbours;
            }
            return neighbours
                .Where(n => n.UnitsHere.Count > 0 || !IsNextToEnemy(n, unit.Owner, unit.Domain));
        }

        public static IList<int> GetIslandsFor(Unit unit)
        {
            if (unit.Domain == UnitGas.Sea)
            {
                return unit.CurrentLocation.Type == TerrainType.Ocean
                    ? new List<int> { unit.CurrentLocation.Island }
                    : unit.CurrentLocation.Neighbours().Where(t => t.Type == TerrainType.Ocean).Select(t => t.Island)
                        .Distinct().ToList();
                
                
            }

            if (unit.CurrentLocation.Type == TerrainType.Ocean)
            {
                return unit.CurrentLocation.Neighbours().Where(n => n.Type != TerrainType.Ocean)
                    .Select(n => n.Island).Distinct().ToList();
            }

            return new[] { unit.CurrentLocation.Island };
        }
    }
}