﻿using System;
using System.Collections.Generic;

namespace HatlessEngine
{
    public class Sprite : IExternalResource
    {   
        public string Filename { get; private set; }
        public string Id { get; private set; }
        public bool IsLoaded { get; private set; }
        internal SFML.Graphics.Sprite SFMLSprite;

        internal Dictionary<string, uint[]> Animations = new Dictionary<string, uint[]>();

        private bool AutoSize = false;
        private uint FrameWidth;
        private uint FrameHeight;
        public uint IndexWidth { get; private set; }
        public uint IndexHeight { get; private set; }
        public uint IndexLength { get; private set; }

        public Size Size 
        {
            get { return new Size(FrameWidth, FrameHeight); }
            set
            {
                FrameWidth = (uint)value.Width;
                FrameHeight = (uint)value.Height;
            }
        }
        
        internal Sprite(string id, string filename) : this(id, filename, new Size(0, 0))
        {
            AutoSize = true;
        }
        internal Sprite(string id, string filename, Size size)
        {
            Id = id;
            Size = size;
            Filename = filename;
            IsLoaded = false;
            IndexWidth = 1;
            IndexHeight = 1;
            IndexLength = 1;
        }

        public void Draw(Position pos, uint frameIndex = 0, params DrawTransformation[] transformations)
        {
            Load();
            SFMLSprite.TextureRect = new SFML.Graphics.IntRect((int)(frameIndex % IndexWidth * FrameWidth), (int)(frameIndex / IndexWidth * FrameHeight), (int)FrameWidth, (int)FrameHeight);

            //apply transformations

            foreach(DrawTransformation transformation in transformations)
            {
                switch (transformation.Type)
                {
                    case DrawTransformation.TransformationType.ScaleAll:
                        SFMLSprite.Scale = new Position((float)transformation.Argument1, (float)transformation.Argument1);
                        break;
                    case DrawTransformation.TransformationType.ScaleAxes:
                        SFMLSprite.Scale = new Position((float)transformation.Argument1, (float)transformation.Argument2);
                        break;
                    case DrawTransformation.TransformationType.Rotate:
                        SFMLSprite.Origin = (Position)transformation.Argument1;
                        SFMLSprite.Rotation = (float)transformation.Argument2;
                        break;
                }
            }
            
            //draw
            SFMLSprite.Position = new SFML.Window.Vector2f(pos.X + SFMLSprite.Origin.X, pos.Y + SFMLSprite.Origin.Y);
            Resources.RenderPlane.Draw(SFMLSprite);

            //clear transformations
            SFMLSprite.Origin = new Position(0, 0);
            SFMLSprite.Scale = new Position(1, 1);
            SFMLSprite.Rotation = 0;
        }

        public void Load()
        {
            if (!IsLoaded)
            {
                SFMLSprite = new SFML.Graphics.Sprite(new SFML.Graphics.Texture(Filename));

                if (AutoSize)
                {
                    FrameWidth = (uint)SFMLSprite.GetLocalBounds().Width;
                    FrameHeight = (uint)SFMLSprite.GetLocalBounds().Height;
                    IndexWidth = 1;
                    IndexHeight = 1;
                    IndexLength = 1;
                }
                else
                {
                    IndexWidth = (uint)(SFMLSprite.GetLocalBounds().Width / FrameWidth);
                    IndexHeight = (uint)(SFMLSprite.GetLocalBounds().Height / FrameHeight);
                    IndexLength = IndexWidth * IndexHeight;
                }
                    
                IsLoaded = true;
            }
        }

        public void Unload()
        {
            SFMLSprite.Dispose();
            SFMLSprite = null;
            IsLoaded = false;
        }

        public void AddAnimation(string id, uint[] animation)
        {
            //add error catching
            Animations.Add(id, animation);
        }
        public void AddAnimation(string id, uint startIndex, uint frames)
        {
            uint[] animationArray = new uint[frames];

            for (uint i = 0; i < frames; i++)
            {
                animationArray[i] = startIndex + i;
            }

            Animations.Add(id, animationArray);
        }
    }
}
