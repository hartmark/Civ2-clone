﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using RTciv2.Imagery;
using RTciv2.Units;

namespace RTciv2.Forms
{
    public partial class MapPanel : Civ2panel
    {
        DoubleBufferedPanel DrawPanel;
        public static int BoxNoX { get; set; }      //No of 32px wide visible squares on map
        public static int BoxNoY { get; set; }      //No of 16px high visible squares on map
        public static int OffsetX { get; set; }     //Offset squares from (0,0) for showing map
        public static int OffsetY { get; set; }
        private int MapGridVar { get; set; }

        public MapPanel(int width, int height)
        {
            Size = new Size(width, height);
            this.Paint += new PaintEventHandler(MapPanel_Paint);

            DrawPanel = new DoubleBufferedPanel()
            {
                Location = new Point(11, 38),
                Size = new Size(Width - 22, Height - 49),
                BackColor = Color.Black
            };
            Controls.Add(DrawPanel);
            DrawPanel.Paint += DrawPanel_Paint;

            BoxNoX = (int)Math.Ceiling((double)DrawPanel.Width / 64);
            BoxNoY = (int)Math.Ceiling((double)DrawPanel.Height / 16);
            OffsetX = 0;
            OffsetY = 0;
            MapGridVar = 0;
        }

        private void MapPanel_Paint(object sender, PaintEventArgs e)
        {
            //Title
            StringFormat sf = new StringFormat();
            sf.LineAlignment = StringAlignment.Center;
            sf.Alignment = StringAlignment.Center;
            //Civilization humanPlayer = Game.Civs.Find(civ => civ.Id == Game.Data.HumanPlayerUsed);
            e.Graphics.DrawString("Roman Map", new Font("Times New Roman", 18), new SolidBrush(Color.Black), new Point(this.Width / 2 + 1, 20 + 1), sf);
            e.Graphics.DrawString("Roman Map", new Font("Times New Roman", 18), new SolidBrush(Color.FromArgb(135, 135, 135)), new Point(this.Width / 2, 20), sf);
            sf.Dispose();
            //Draw line borders of panel
            e.Graphics.DrawLine(new Pen(Color.FromArgb(67, 67, 67)), 9, 36, 9 + (Width - 18 - 1), 36);   //1st layer of border
            e.Graphics.DrawLine(new Pen(Color.FromArgb(67, 67, 67)), 9, 36, 9, Height - 9 - 1);
            e.Graphics.DrawLine(new Pen(Color.FromArgb(223, 223, 223)), Width - 9 - 1, 36, Width - 9 - 1, Height - 9 - 1);
            e.Graphics.DrawLine(new Pen(Color.FromArgb(223, 223, 223)), 9, Height - 9 - 1, Width - 9 - 1, Height - 9 - 1);
            e.Graphics.DrawLine(new Pen(Color.FromArgb(67, 67, 67)), 10, 37, 9 + (Width - 18 - 2), 37);   //2nd layer of border
            e.Graphics.DrawLine(new Pen(Color.FromArgb(67, 67, 67)), 10, 37, 10, Height - 9 - 2);
            e.Graphics.DrawLine(new Pen(Color.FromArgb(223, 223, 223)), Width - 9 - 2, 37, Width - 9 - 2, Height - 9 - 2);
            e.Graphics.DrawLine(new Pen(Color.FromArgb(223, 223, 223)), 10, Height - 9 - 2, Width - 9 - 2, Height - 9 - 2);
        }

        private void DrawPanel_Paint(object sender, PaintEventArgs e)
        {
            //Draw map
            for (int col = 0; col < BoxNoX; col++)
                for (int row = 0; row < BoxNoY; row++)
                    e.Graphics.DrawImage(Game.Map[col, row].Graphic, 64 * col + 32 * (row % 2), 16 * row);

            //Draw cities
            StringFormat sf = new StringFormat();
            sf.LineAlignment = StringAlignment.Center;
            sf.Alignment = StringAlignment.Center;
            foreach (City city in Game.Cities) { 
                e.Graphics.DrawImage(city.Graphic, 32 * (city.X2 - OffsetX), 16 * (city.Y2 - OffsetY) - 16);
                e.Graphics.DrawString(city.Name, new Font("Times New Roman", 14.0f), new SolidBrush(Color.Black), 32 * (city.X2 - OffsetX) + 32 + 2, 16 * (city.Y2 - OffsetY) + 32, sf);    //Draw shadow around font
                e.Graphics.DrawString(city.Name, new Font("Times New Roman", 14.0f), new SolidBrush(Color.Black), 32 * (city.X2 - OffsetX) + 32, 16 * (city.Y2 - OffsetY) + 32 + 2, sf);    //Draw shadow around font
                e.Graphics.DrawString(city.Name, new Font("Times New Roman", 14.0f), new SolidBrush(CivColors.Light[city.Owner]), 32 * (city.X2 - OffsetX) + 32, 16 * (city.Y2 - OffsetY) + 32, sf); }
            sf.Dispose();

            //Draw units
            foreach (IUnit unit in Game.Units)
                if (unit == Game.Instance.ActiveUnit) e.Graphics.DrawImage(unit.GraphicMapPanel, 32 * (unit.X2 - OffsetX), 16 * (unit.Y2 - OffsetY) - 16);
                else if (!(unit.IsInCity || (unit.IsInStack && unit.IsLastInStack))) e.Graphics.DrawImage(unit.GraphicMapPanel, 32 * (unit.X2 - OffsetX), 16 * (unit.Y2 - OffsetY) - 16);

            //Draw gridlines
            if (Options.Grid)
            {
                for (int col = 0; col < BoxNoX; col++)
                    for (int row = 0; row < BoxNoY; row++)
                    {
                        if (MapGridVar > 0) e.Graphics.DrawImage(Images.GridLines, 64 * col + 32 * (row % 2), 16 * row);
                        if (MapGridVar == 2)    //XY coords
                        {
                            int x = col * 64 + 12;
                            int y = row * 32 + 8;
                            e.Graphics.DrawString(String.Format("({0},{1})", col + OffsetX, row + OffsetY), new Font("Arial", 8), new SolidBrush(Color.Yellow), x, y, new StringFormat()); //for first horizontal line
                            e.Graphics.DrawString(String.Format("({0},{1})", col + OffsetX + 1, row + OffsetY + 1), new Font("Arial", 8), new SolidBrush(Color.Yellow), x + 32, y + 16, new StringFormat()); //for second horizontal line
                        }
                        if (MapGridVar == 3)    //civXY coords
                        {
                            int x = col * 64 + 12;
                            int y = row * 32 + 8;
                            e.Graphics.DrawString(String.Format("({0},{1})", 2 * col + OffsetX, 2 * row + OffsetY), new Font("Arial", 8), new SolidBrush(Color.Yellow), x, y, new StringFormat()); //for first horizontal line
                            e.Graphics.DrawString(String.Format("({0},{1})", 2 * col + 1 + OffsetX, 2 * row + 1 + OffsetY), new Font("Arial", 8), new SolidBrush(Color.Yellow), x + 32, y + 16, new StringFormat()); //for second horizontal line
                        }
                    }
            }

        }

        public int ToggleMapGrid()
        {
            MapGridVar++;
            if (MapGridVar > 3) MapGridVar = 0;
            if (MapGridVar != 0) Options.Grid = true;
            else Options.Grid = false;
            Refresh();
            return MapGridVar;
        }
    }
}
