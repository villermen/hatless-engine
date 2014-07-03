﻿using System;
using System.IO;
using System.Collections.Generic;
using OpenTK.Audio.OpenAL;
using System.Reflection;
using SDL2;

namespace HatlessEngine
{
	/// <summary>
	/// Will contain all references to the resource files.
	/// Keeps resources loaded until they are no longer needed, or aren't used for a while.
	/// </summary>
	public static class Resources
	{
		/// <summary>
		/// If set this will be checked before the program's location will be checked.
		/// It will do relative checks if no drive is supplied.
		/// It will also work for embedded resources.
		/// Make sure to add a trailing slash and leave out a leading one. (if relative)
		/// </summary>
		public static string RootDirectory = "";

		//external
		internal static List<IExternalResource> ExternalResources = new List<IExternalResource>();
		public static Dictionary<string, Cursor> Cursors = new Dictionary<string, Cursor>();
		public static Dictionary<string, Font> Fonts = new Dictionary<string, Font>();
		public static Dictionary<string, Music> Music = new Dictionary<string, Music>();
		public static Dictionary<string, Sound> Sounds = new Dictionary<string, Sound>();
		public static Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

		//logical
		public static Dictionary<string, View> Views = new Dictionary<string, View>();

		//collections
		public static Dictionary<string, Objectmap> Objectmaps = new Dictionary<string, Objectmap>();
		public static Dictionary<string, Spritemap> Spritemaps = new Dictionary<string, Spritemap>();

		//objects
		public static List<LogicalObject> Objects = new List<LogicalObject>();
		public static List<PhysicalObject> PhysicalObjects = new List<PhysicalObject>();
		public static Dictionary<Type, List<PhysicalObject>> PhysicalObjectsByType = new Dictionary<Type, List<PhysicalObject>>();

		//addition/removal (has to be done after looping)
		internal static List<LogicalObject> AddObjects = new List<LogicalObject>();
		internal static List<LogicalObject> RemoveObjects = new List<LogicalObject>();

		internal static List<WeakReference> ManagedSprites = new List<WeakReference>();

		//audio helpers
		internal static List<int> AudioSources = new List<int>();
		internal static Dictionary<int, AudioControl> AudioControls = new Dictionary<int, AudioControl>();

		private static Assembly EntryAssembly = Assembly.GetEntryAssembly();
		private static Assembly HatlessEngineAssembly = Assembly.GetExecutingAssembly();

		/// <summary>
		/// Gets the BinaryReader of a file with the given filename.
		/// All resources are loaded this way.
		/// Priority: 
		/// 1: Embedded Resource in the RootDirectory within the entry assembly.
		/// 2: Embedded Resource in the entry assembly.
		/// 3: File in the RootDirectory.
		/// 4: File in the application's directory, or an absolute filepath.
		/// Also, don't work with backslashes, they are nasty and unaccounted for.
		/// </summary>
		public static BinaryReader GetStream(string filename)
		{
			Stream stream;

			//sneaky extra option: embedded resource in hatlessengine
			stream = HatlessEngineAssembly.GetManifestResourceStream(HatlessEngineAssembly.GetName().Name + "." + filename);
			if (stream != null)
				return new BinaryReader(stream);

			if (RootDirectory != "")
			{
				stream = EntryAssembly.GetManifestResourceStream(EntryAssembly.GetName().Name + "." + (RootDirectory + filename).Replace('/', '.'));
				if (stream != null)
					return new BinaryReader(stream);
			}

			stream = EntryAssembly.GetManifestResourceStream(EntryAssembly.GetName().Name + "." + filename.Replace('/', '.'));
			if (stream != null)
				return new BinaryReader(stream);

			if (RootDirectory != "" && File.Exists(RootDirectory + filename))
			{
				stream = File.Open(RootDirectory + filename, FileMode.Open, FileAccess.Read, FileShare.Read);			
				if (stream != null)
					return new BinaryReader(stream);
			}

			if (File.Exists(filename))
			{
				stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
				if (stream != null)
					return new BinaryReader(stream);
			}

			throw new FileNotFoundException("The file could not be found in any of the possible locations.");
		}

		/// <summary>
		/// Creates an SDL RW resource from the entire file, using GetStream to resolve the filename.
		/// </summary>
		internal static IntPtr CreateRWFromFile(string filename)
		{
			using (BinaryReader reader = GetStream(filename))
			{
				int length = (int)reader.BaseStream.Length;
				return SDL.SDL_RWFromMem(reader.ReadBytes(length), length);
			}
		}

		public static void LoadAllExternalResources()
		{
			foreach (IExternalResource resource in ExternalResources)
				resource.Load();
		}
		public static void UnloadAllExternalResources()
		{
			foreach (IExternalResource resource in ExternalResources)
				resource.Unload();
		}

		internal static void ObjectAdditionAndRemoval()
		{
			//object addition
			Objects.AddRange(AddObjects);
			AddObjects.Clear();

			//object removal
			foreach (LogicalObject logicalObject in RemoveObjects)
				Objects.Remove(logicalObject);
			RemoveObjects.Clear();
		}

		/// <summary>
		/// Gets an OpenAL source identifier.
		/// Source will be managed by HatlessEngine to prevent not playing of sound after all device channels are occupied.
		/// </summary>
		internal static int GetSource()
		{
			int source;
			//will execute multiple times if cleanup has not removed the source from AudioControls yet
			while (AudioControls.ContainsKey(source = AL.GenSource())) { }
			AudioSources.Add(source);
			return source;
		}

		/// <summary>
		/// Removes all stopped sources.
		/// </summary>
		internal static void SourceRemoval()
		{
			List<int> removeSources = new List<int>();
			foreach(int source in AudioSources)
			{
				if (AL.GetSourceState(source) == ALSourceState.Stopped)
				{
					AL.DeleteSource(source);
					removeSources.Add(source);
				}
			}

			foreach(int source in removeSources)
			{
				AudioSources.Remove(source);
				AudioControls[source].PerformStopped();
				AudioControls.Remove(source);
			}
		}

		internal static void UpdateManagedSprites()
		{
			List<WeakReference> removeManagedSprites = new List<WeakReference>();
			foreach(WeakReference managedSprite in ManagedSprites)
			{
				//check if alive and add to remove list if not
				if (managedSprite.IsAlive)
				{
					//perform step
					((ManagedSprite)managedSprite.Target).Step();
				}
				else
					removeManagedSprites.Add(managedSprite);
			}

			foreach(WeakReference managedSprite in removeManagedSprites)
			{
				ManagedSprites.Remove(managedSprite);
			}
		}

		internal static void CleanupFontTextures()
		{
			foreach (Font font in Fonts.Values)
			{
				List<Tuple<string, Color>> removeTextures = new List<Tuple<string, Color>>();

				foreach(KeyValuePair<Tuple<string, Color>, IntPtr> texture in font.Textures)
				{
					//delete texture if it hasn't been used for 10 steps
					if (font.TexturesDrawsUnused[texture.Key] == 10)
					{
						SDL.SDL_DestroyTexture(texture.Value);
						removeTextures.Add(texture.Key);
					}

					font.TexturesDrawsUnused[texture.Key]++;
				}

				foreach (Tuple<string, Color> texture in removeTextures)
				{
					font.Textures.Remove(texture);
					font.TexturesDrawsUnused.Remove(texture);
				}
			}
		}
	}
}