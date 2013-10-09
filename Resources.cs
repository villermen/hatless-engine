﻿using System;
using System.IO;
using System.Collections.Generic;

namespace HatlessEngine
{
    /// <summary>
    /// Will contain all references to the resource files.
    /// Keeps resources loaded until they are no longer needed, or aren't used for a while.
    /// </summary>
    public static class Resources
    {
        private static string RootDirectory = System.Environment.CurrentDirectory + "/res/";

        public static Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        public static List<View> Views = new List<View>();

        public static List<Window> Windows = new List<Window>();

        public static Dictionary<string, Music> Musics = new Dictionary<string, Music>();

        public static Dictionary<string, Sound> Sounds = new Dictionary<string, Sound>();

        public static Dictionary<string, Font> Fonts = new Dictionary<string, Font>();

        public static List<LogicalObject> Objects = new List<LogicalObject>();
        public static Dictionary<Type, List<PhysicalObject>> PhysicalObjectsByType = new Dictionary<Type, List<PhysicalObject>>();

        public static Sprite AddSprite(string id, string filename, uint width = 0)
        {
            if (!File.Exists(RootDirectory + filename))
                Log.WriteLine("Resources.AddSprite: file '" + RootDirectory + filename + "' does not exist.", ErrorLevel.FATAL);
            if (Sprites.ContainsKey(id))
                Log.WriteLine("Resources.AddSprite: id '" + id + "' already exists.", ErrorLevel.FATAL);

            Sprite sprite;
            if (width == 0)
                sprite = new Sprite(id, RootDirectory + filename);
            else
                sprite = new Sprite(id, RootDirectory + filename, width);

            Sprites.Add(id, sprite);

            return sprite;
        }
        public static View AddView(string id, float viewX, float viewY, float viewWidth, float viewHeight, Window targetWindow, float windowXFraction = 0, float windowYFraction = 0, float windowWidthFraction = 1, float windowHeightFraction = 1)
        {
            View view = new View(id, viewX, viewY, viewWidth, viewHeight, targetWindow, windowXFraction, windowYFraction, windowWidthFraction);
            Views.Add(view);
            return view;
        }
        public static Window AddWindow(string id, uint width, uint height, string title)
        {
            Window window = new Window(id, width, height, title);
            Windows.Add(window);
            return window;
        }
        public static Music AddMusic(string id, string filename)
        {
            if (!File.Exists(RootDirectory + filename))
                Log.WriteLine("Resources.AddMusic: file '" + RootDirectory + filename + "' does not exist.", ErrorLevel.FATAL);
            if (Musics.ContainsKey(id))
                Log.WriteLine("Resources.AddMusic: id '" + id + "' already exists.", ErrorLevel.FATAL);

            Music music = new Music(id, RootDirectory + filename);
            Musics.Add(id, music);

            return music;
        }
        public static Sound AddSound(string id, string filename)
        {
            if (!File.Exists(RootDirectory + filename))
                Log.WriteLine("Resources.AddSound: file '" + RootDirectory + filename + "' does not exist.", ErrorLevel.FATAL);
            if (Sounds.ContainsKey(id))
                Log.WriteLine("Resources.AddSound: id '" + id + "' already exists.", ErrorLevel.FATAL);

            Sound sound = new Sound(id, RootDirectory + filename);
            Sounds.Add(id, sound);

            return sound;
        }
        public static Font AddFont(string id, string filename)
        {
            if (!File.Exists(RootDirectory + filename))
                Log.WriteLine("Resources.AddFont: file '" + RootDirectory + filename + "' does not exist.", ErrorLevel.FATAL);
            if (Fonts.ContainsKey(id))
                Log.WriteLine("Resources.AddFont: id '" + id + "' already exists.", ErrorLevel.FATAL);

            Font font = new Font(id, RootDirectory + filename);
            Fonts.Add(id, font);

            return font;
        }

        public static Sprite Sprite(string id)
        {
            if (!Sprites.ContainsKey(id))
                Log.WriteLine("Resources.Sprite: id '" + id + "' does not exist.", ErrorLevel.FATAL);

            return Sprites[id];
        }
        public static View ViewById(string id)
        {
            foreach (View view in Views)
            {
                if (view.Id == id)
                    return view;
            }

            Log.WriteLine("Resources.ViewById: id '" + id + "' does not exist.", ErrorLevel.FATAL);
            return null;
        }
        public static Window WindowById(string id)
        {
            foreach (Window window in Windows)
            {
                if (window.Id == id)
                    return window;
            }

            Log.WriteLine("Resources.WindowById: id '" + id + "' does not exist.", ErrorLevel.FATAL);
            return null;
        }
        public static Music Music(string id)
        {
            if (!Musics.ContainsKey(id))
                Log.WriteLine("Resources.Sprite: id '" + id + "' does not exist.", ErrorLevel.FATAL);

            return Musics[id];
        }
        public static Sound Sound(string id)
        {
            if (!Sounds.ContainsKey(id))
                Log.WriteLine("Resources.Sprite: id '" + id + "' does not exist.", ErrorLevel.FATAL);

            return Sounds[id];
        }
        public static Font Font(string id)
        {
            if (!Fonts.ContainsKey(id))
                Log.WriteLine("Resources.Font: id '" + id + "' does not exist.", ErrorLevel.FATAL);

            return Fonts[id];
        }
    }
}
