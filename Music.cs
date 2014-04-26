﻿using System;
using OpenTK.Audio.OpenAL;

namespace HatlessEngine
{
	public class Music :  AudioControl, IExternalResource
    {
        public string Filename { get; private set; }
        public string Id { get; private set; }
        public bool Loaded { get; private set; }

		public bool Loop = false; //implement in audiocontrol

		internal bool Streaming = false;

		internal int[] BufferIds;
		internal byte ActiveBufferId;

		internal bool JustStartedPlaying = false;

		//for triggering event in old music when this one is set as the PlayAfter one (needs to be triggered on a delay)
		internal byte PerformMusicChangedEventDelay = 3;
		internal Music PerformMusicChangedEventMusic = null;

		internal WaveReader WaveReader;

		/// <summary>
		/// Sample to start after when this music is looped.
		/// One sample is both channels for stereo. 
		/// Audacity is a good way to find out what sample you need.
		/// Just split the files into an intro and loop region if you don't want to use this.
		/// </summary>
		public uint LoopStartSample = 0;

		/// <summary>
		/// Music to play directly after this one ends.
		/// Of course this doesn't work when this one's in looping mode.
		/// </summary>
		public string PlayAfterMusic = "";

		internal Music(string id, string filename)
        {
            Id = id;
            Filename = filename;
            Loaded = false;
        }

        /// <summary>
        /// Start playing the music.
        /// volume and balance will set the Volume and Balance before the music starts. (For convenience.)
        /// </summary>
        public void Play(float volume, float balance)
        {
			if (!Loaded)
			{
				if (Resources.JustInTimeLoading)
					Load();
				else
					throw new NotLoadedException();
			}

			if (Streaming)
			{
                Stop(); //needs better solution
                Streaming = false;
			}

			WaveReader.Rewind();

			SourceId = Resources.GetSource();

			//fill first buffer to make sure the music can start playing after this point
			int readSamples;
			short[] waveData = WaveReader.ReadSamples(WaveReader.SampleRate / 2 * WaveReader.Channels, out readSamples);
			AL.BufferData(BufferIds[0], WaveReader.ALFormat, waveData, readSamples * 2, WaveReader.SampleRate);

			//attach
			AL.SourceQueueBuffer(SourceId, BufferIds[0]);

            //set volume and balance before playback
            Volume = volume;
            Balance = balance;

			AL.SourcePlay(SourceId);

			JustStartedPlaying = true;
            Streaming = true;

			MusicStreamer.Launch();

			Resources.AudioSources.Add(SourceId);
			Resources.AudioControls.Add(SourceId, this);
        }
        public void Play(float volume)
        {
            Play(volume, _Balance);
        }
        public void Play()
        {
            Play(_Volume, _Balance);
        }

        public void Load()
        {
            if (!Loaded)
            {
                WaveReader = new WaveReader(Resources.GetStream(Filename));
                if (WaveReader.MetaLoaded)
                    Loaded = true;
                else
                    throw new System.IO.FileLoadException();
                BufferIds = AL.GenBuffers(3);
            }
            else
                throw new AlreadyLoadedException();
        }

        /// <summary>
        /// For rearming after AudioContext has been destroyed.
        /// </summary>
        internal void LoadForced()
        {
            Loaded = false;
            Load();

            //we don't have the old buffers and source so streaming won't do much. (Don't hog the streamer is what im getting at.)
            Streaming = false;
        }

        public void Unload()
        {
			if (Loaded)
			{
				WaveReader.Dispose();
				WaveReader = null;
				AL.DeleteBuffers(BufferIds);
				Loaded = false;
			}
        }

		/// <summary>
		/// Occurs when this music is done playing and it's switching over the source to the given PlayAfterMusic.
		/// </summary>
		public event EventHandler<MusicChangedEventArgs> MusicChanged;

		internal void PerformMusicChanged(Music newMusic)
		{
			if (MusicChanged != null)
				MusicChanged(this, new MusicChangedEventArgs(this, newMusic));
		}

        public override string ToString()
        {
            if (Loaded)
                return "'" + Id + "' (" + Filename + ", " + WaveReader.Format.ToString() + ", " + WaveReader.ALFormat.ToString() + ", " + WaveReader.Duration.ToString() + ")";
            else
                return "'" + Id + "' (" + Filename + ", Unloaded)";            
        }
    }
}
