﻿using Eto.Forms;
using Civ2engine.Enums;

namespace EtoFormsUI
{
    public partial class Main : Form
    {
        private void KeyPressedEvent(object sender, KeyEventArgs e)
        {
            if (suppressKeyEvent) return;

            switch (e.Key)
            {
                case Keys.Keypad1:
                    {
                        if (!Map.ViewPieceMode)
                        {
                            Game.IssueUnitOrder(OrderType.MoveSW);
                        }
                        break;
                    }
                case Keys.Keypad2:
                    {
                        if (!Map.ViewPieceMode)
                        {
                            Game.IssueUnitOrder(OrderType.MoveS);
                        }
                        break;
                    }
                case Keys.Keypad3:
                    {
                        if (!Map.ViewPieceMode)
                        {
                            Game.IssueUnitOrder(OrderType.MoveSE);
                        }
                        break;
                    }
                case Keys.Keypad4:
                    {
                        if (!Map.ViewPieceMode)
                        {
                            Game.IssueUnitOrder(OrderType.MoveW);
                        }
                        break;
                    }
                case Keys.Keypad6:
                    {
                        if (!Map.ViewPieceMode)
                        {
                            Game.IssueUnitOrder(OrderType.MoveE);
                        }
                        break;
                    }
                case Keys.Keypad7:
                    {
                        if (!Map.ViewPieceMode)
                        {
                            Game.IssueUnitOrder(OrderType.MoveNW);
                        }
                        break;
                    }
                case Keys.Keypad8:
                    {
                        if (!Map.ViewPieceMode)
                        {
                            Game.IssueUnitOrder(OrderType.MoveN);
                        }
                        break;
                    }
                case Keys.Keypad9:
                    {
                        if (!Map.ViewPieceMode)
                        {
                            Game.IssueUnitOrder(OrderType.MoveNE);
                        }
                        break;
                    }
                case Keys.Space:
                    {
                        if (!Map.ViewPieceMode)
                        {
                            Game.IssueUnitOrder(OrderType.SkipTurn);
                        }
                        break;
                    }
            }
        }
    }
}
