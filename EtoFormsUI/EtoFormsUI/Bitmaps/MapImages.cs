using System.Collections.Generic;
using System.Linq;
using Civ2engine;
using Eto.Drawing;
using EtoFormsUIExtensionMethods;

namespace EtoFormsUI
{
    public static class MapImages
    {
        /// <summary>
        //  Read file in local directory. If it doesn't exist there, read it in root civ2 directory.
        /// </summary>
        /// <param name="name">the filename to load</param>
        /// <param name="path">the local directory to load from</param>
        /// <returns></returns>
        private static Bitmap LoadBitmapFrom(string name, Ruleset ruleset)
        {
            var filePath = Utils.GetFilePath(name, ruleset.Paths, "bmp", "gif");

            return filePath == null ? new Bitmap(640, 480, PixelFormat.Format32bppRgba) : new Bitmap(filePath);
        }

        private static readonly Color _flagColour = Color.FromArgb(0, 0, 255);

        public static CityImage[] Cities { get; set; }

        public static void LoadCities(Ruleset path)
        {
            // Read file in local directory. If it doesn't exist there, read it in root civ2 directory.
            using var citiesImage = LoadBitmapFrom("CITIES", path);

            // Initialize objects
            var cities = new List<CityImage>();
            
            // Load special colours
            var colours = new List<Color>();
            var first = new List<int>();
            var last = new List<int>();
            var frequency = new List<int>();
            var max = 0;

            for (var i = 0; i < citiesImage.Height; i++)
            {
                var colour = citiesImage.GetPixel(0, i);
                var index = colours.IndexOf(colour);
                if (index == -1)
                {
                    colours.Add(colour);
                    first.Add(i);
                    last.Add(i);
                    frequency.Add(1);
                }
                else
                {
                    last[index] = i;
                    frequency[index]++;
                    if (index != max && frequency[index] > frequency[max])
                    {
                        max = index;
                    }
                }
            }

            var borderColour = colours[max];
            var firstRow = first[max];

            var firstTransparent = citiesImage.GetPixel(0, 0);
            var secondTransparent = citiesImage.GetPixel(1, firstRow + 1);
            
            var borderColours = colours.Where((_, i) => first[i] >= firstRow).ToList();
            
            var height = 0;
            for (var i = firstRow + 1; i < citiesImage.Height; i++)
            {
                if (borderColours.IndexOf(citiesImage.GetPixel(1, i)) == -1) continue;
                height = i - firstRow;
                break;
            }

            var width = 0;

            
            for (var i = 1; i < citiesImage.Width; i++)
            {
                if (borderColours.IndexOf(citiesImage.GetPixel(i, firstRow + 1)) == -1) continue;
                width = i;
                break;
            }
            
            citiesImage.ReplaceColors(firstTransparent, Colors.Transparent);
            citiesImage.ReplaceColors(secondTransparent, Colors.Transparent);

            for (var i = firstRow; i < citiesImage.Height - firstRow; i++)
            {
                if (borderColours.IndexOf(citiesImage.GetPixel(1, i)) == -1 ||
                    borderColours.IndexOf(citiesImage.GetPixel(1, i + height)) == -1) continue;
                //We have a candidate row
                for (var j = 0; j < citiesImage.Width - width; j++)
                {
                    if (citiesImage.GetPixel(j, i + 1) != borderColour ||
                        citiesImage.GetPixel(j + width, i + 1) != borderColour) continue;
                    //This looks like a city image
                    var cityImage = MakeCityImage(citiesImage, i, j, width, height);
                    if (cityImage != null)
                    {
                        cities.Add(cityImage);
                    }
                }
            }

            Cities = cities.ToArray();

            var lastRow = last[max];
            var flagHeight = 0;
            for (var i = lastRow - 1; i > 0; i--)
            {
                if (citiesImage.GetPixel(1, i) != borderColour) continue;
                flagHeight = lastRow - i;
                break;
            }

            var flagWidth = 0;
            for (var i = 1; i < flagHeight; i++)
            {
                if (citiesImage.GetPixel(i, lastRow -1) != borderColour) continue;
                flagWidth = i;
                break;
            }

            var flags = new List<PlayerFlag>();
            var textColours = new List<Color>();
            var darkColours = new List<Color>();
            var lightColours = new List<Color>();
            var topLeft = lastRow - 2 * flagHeight + 1;
            var col = 1;
            for (; col < citiesImage.Width; col += flagWidth)
            {
                if (citiesImage.GetPixel(col + flagWidth -1, topLeft) == borderColour)
                {
                    flags.Add(new PlayerFlag
                    {
                        Smaller = citiesImage.Clone(new Rectangle(col, topLeft + flagHeight, flagWidth -1, flagHeight -1)),
                        Normal = citiesImage.Clone(new Rectangle(col, topLeft, flagWidth -1, flagHeight -1))
                    });
                    textColours.Add(citiesImage.GetPixel(col, topLeft -2));
                    darkColours.Add(citiesImage.GetPixel(col, topLeft - 3) == Colors.Transparent
                        ? citiesImage.GetPixel(col + 6, topLeft + 5)
                        : citiesImage.GetPixel(col, topLeft - 3));
                    var lightCandidate = citiesImage.GetPixel(col, topLeft - 4);
                    lightColours.Add(lightCandidate == Colors.Transparent || lightCandidate == borderColour
                        ? citiesImage.GetPixel(col + 5, topLeft + 6)
                        : lightCandidate);
                }
                else
                {
                    break;
                }
            }

            TextColours = textColours.ToArray();
            DarkColours = darkColours.ToArray();
            PlayerColours = lightColours.ToArray();
            Flags = flags.ToArray();

            var specials = new List<Bitmap>();
            var specialStart = col + 1;
            while (citiesImage.GetPixel(specialStart, topLeft) == Colors.Transparent)
            {
                specialStart++;
            }

            var specialTop = topLeft;
            while (citiesImage.GetPixel(specialStart, specialTop) == borderColour)
            {
                specialTop--;
            }

            specialTop += 2;

            for (var i = specialStart; i < citiesImage.Width - width; i += width)
            {
                if (citiesImage.GetPixel(i + width, specialTop) == borderColour)
                {
                    specials.Add(citiesImage.Clone(new Rectangle(i+ 1, specialTop, width -1, height -1)));
                }
            }

            Specials = specials.ToArray();
        }

        public static Bitmap[] Specials { get; set; }

        public static Color[] DarkColours { get; set; }

        public static PlayerFlag[] Flags { get; set; }

        public static Color[] PlayerColours { get; set; }
        
        public static Color[] TextColours { get; set; }

        private static CityImage MakeCityImage(Bitmap citiesImage, int y, int x, int width, int height)
        {
            int flagX = 0;
            int flagY = 0;
            int sizeX = 0;
            int sizeY = 0;
            var borderColour = citiesImage.GetPixel(x, y);
            for (var i = x; i < x + width; i++)
            {
                var colour = citiesImage.GetPixel(i, y);
                if (colour == borderColour) continue;
                
                if (colour != _flagColour)
                {
                    sizeX = i - x;
                    if (flagX == 0) flagX = sizeX;
                }
                else
                {
                    flagX = i - x;
                    if (sizeX == 0) sizeX = flagX;
                }
            }
            for (var i = y; i < y + height; i++)
            {
                var colour = citiesImage.GetPixel(x, i);
                if (colour == borderColour) continue;
                
                if (colour != _flagColour)
                {
                    sizeY = i - y;
                    if (flagY == 0) flagY = sizeY;
                }
                else
                {
                    flagY = i - y;
                    if (sizeY == 0) sizeY = flagY;
                }
            }
            

            if (flagX == 0 || flagY == 0 || sizeX == 0 || sizeY == 0)
            {
                return null;
            }

            return new CityImage
            {
                Bitmap = citiesImage.Clone(new Rectangle(x + 1, y + 1, width - 1, height - 1)),
                FlagLoc = new Point(flagX, flagY),
                SizeLoc = new Point(sizeX, sizeY)
            };
        }
    }

    public class PlayerFlag
    {
        public Bitmap Normal { get; set; }
        public Bitmap Smaller { get; set; }
    }

    public sealed class CityImage
    {
        public Bitmap Bitmap { get; set; }
        public Point FlagLoc { get; set; }
        public Point SizeLoc { get; set; }
    }
}