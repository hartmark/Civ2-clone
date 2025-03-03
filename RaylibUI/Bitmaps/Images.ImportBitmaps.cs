﻿using System.IO;
using System.Net.Mime;
using System.Text;
using Civ2.ImageLoader;
using Raylib_cs;
using Civ2engine;
using Civ2engine.MapObjects;
using Civ2engine.Terrains;
using Model;
using Model.Images;
using RaylibUI.RunGame.GameControls.Mapping;

namespace RaylibUI
{
    public static class Images
    {
   
        //public static void LoadPeopleIcons(Ruleset ruleset)
        //{
        //    using var iconsImage = Common.LoadBitmapFrom("PEOPLE", ruleset.Paths);

        //    var peopleL = new Bitmap[11, 4];
        //    var peopleLshadow = new Bitmap[11, 4];

        //    var transparentPink = Color.FromArgb(255, 0, 255);

        //    for (int row = 0; row < 4; row++)
        //    {
        //        for (int col = 0; col < 11; col++)
        //        {
        //            peopleL[col, row] = iconsImage.Clone(new Rectangle((27 * col) + 2 + col, (30 * row) + 6 + row, 27, 30));
        //            peopleL[col, row].SetTransparent(new Color[] { transparentPink });

        //            peopleLshadow[col, row] = peopleL[col, row].Clone();
        //            peopleLshadow[col, row].ToSingleColor(Colors.Black);
        //        }
        //    }

        //    CityImages.PeopleLarge = peopleL;
        //    CityImages.PeopleShadowLarge = peopleLshadow;
        //}

        //public static void LoadCityWallpaper(Ruleset ruleset)
        //{
        //    var wallpaper = Common.LoadBitmapFrom("CITY", ruleset.Paths);
        //    CityImages.Wallpaper = wallpaper.CropImage(new Rectangle(0, 0, 636, 421));
        //}

        //public static void ImportWallpapersFromIconsFile()
        //{
        //    using var icons = Common.LoadBitmapFrom("ICONS", Settings.SearchPaths);
        //    MapImages.PanelOuterWallpaper = icons.Clone(new Rectangle(199, 322, 64, 32));
        //    MapImages.PanelInnerWallpaper = icons.Clone(new Rectangle(298, 190, 32, 32));
        //}

        /// <summary>
        /// Convert indexed to non-indexed images (required for making transparent pixels, etc.)
        /// </summary>
        /// <param name="src">Source indexed image</param>
        /// <returns>Non-indexed image</returns>
        //public static Bitmap CreateNonIndexedImage(Image src)
        //{
        //    var newBmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppRgba);

        //    using var g = new Graphics(newBmp);
        //    g.DrawImage(src, 0, 0);

        //    return newBmp;
        //}

        private static Dictionary<string, Image> _imageCache = new();

        private const string TempPath = "temp";

        public static Image ExtractBitmap(IImageSource imageSource)
        {
            return ExtractBitmap(imageSource, null);
        }
        
        public static Image ExtractBitmap(IImageSource imageSource, IUserInterface? active, int owner = -1)
        {
            if (!Directory.Exists(TempPath))
            {
                Directory.CreateDirectory(TempPath);
            }

            var key = imageSource.GetKey(owner);
            if (_imageCache.TryGetValue(key, out var bitmap)) return bitmap;
            
            switch (imageSource)
            {
                case BinaryStorage binarySource:
                    _imageCache[key] = ExtractBitmap(binarySource.FileName, binarySource.DataStart, binarySource.Length, key);
                    break;
                case BitmapStorage bitmapStorage:
                {
                    var sourceKey = $"{bitmapStorage.Filename}-Source";
                    if (!_imageCache.ContainsKey(sourceKey))
                    {
                        var path = Utils.GetFilePath(bitmapStorage.Filename, Settings.SearchPaths, bitmapStorage.Extension);
                        _imageCache[sourceKey] = Raylib.LoadImageFromMemory(Path.GetExtension(path).ToLowerInvariant(), File.ReadAllBytes(path));
                    }

                    var sourceImage = _imageCache[sourceKey];
                    var rect = bitmapStorage.Location;
                    if (rect.Width == 0)
                    {
                        rect = new Rectangle(0, 0, sourceImage.Width, sourceImage.Height);
                    }
                    var image = Raylib.ImageFromImage(_imageCache[sourceKey], rect);
                    if (bitmapStorage.Transparencies != null)
                    {
                        foreach (var transparency in bitmapStorage.Transparencies)
                        {
                            Raylib.ImageColorReplace(ref image, transparency,
                                new Color(transparency.R, transparency.G, transparency.B, (byte)0));
                        }
                    }
                    _imageCache[bitmapStorage.Key] = image;
                    break;
                }
                case MemoryStorage memoryStorage:
                {
                    if (owner != -1 && memoryStorage.ReplacementColour != null && active != null)
                    {
                        var image = Raylib.ImageCopy(memoryStorage.Image);
                        Raylib.ImageColorReplace(ref image, memoryStorage.ReplacementColour.Value,
                            memoryStorage.Dark
                                ? active.PlayerColours[owner].DarkColour
                                : active.PlayerColours[owner].LightColour);
                        _imageCache[key] = image;
                    }
                    else
                    {
                        _imageCache[key] = memoryStorage.Image;
                    }
                    break;
                }
                default:
                    throw new NotImplementedException("Other image sources not currently implemented");
            }

            return _imageCache[key];
        }
        
        public static Image ExtractBitmap(string filename, int start, int length, string key)
        {
            if (!Files.ContainsKey(filename))
            {
                var path = Utils.GetFilePath(filename);
                if (string.IsNullOrEmpty(path))
                {
                    Console.Error.WriteLine("Failed to load file " + filename + " please check value");
                    return ImageUtils.NewImage(1, 1);
                }
                Files[filename] = File.ReadAllBytes(path);
            }

            return ExtractBitmap(Files[filename], start, length, key);
        }

        public static Dictionary<string, byte[]> Files { get; } = new();

        static byte[] _extn = Encoding.ASCII.GetBytes("Gif\0");

        private static Image ExtractBitmap(byte[] byteArray, int start, int length, string key)
        {
            // Make empty byte array to hold GIF bytes
            byte[] newBytesRange = new byte[length];

            // Copy GIF bytes in DLL byte array into empty array
            Array.Copy(byteArray, start, newBytesRange, 0, length);
            var fileName = Path.Combine(TempPath, key + ".gif");
            using (var file = File.Create(fileName))
            {
                file.Write(newBytesRange);
                file.Flush();
            }

            return Raylib.LoadImage(fileName);

        }
    }
}
