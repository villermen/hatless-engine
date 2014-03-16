﻿using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using OpenTK.Graphics.OpenGL;

namespace HatlessEngine
{
    public class Sprite : IExternalResource
    {   
        public string Filename { get; private set; }
        public string Id { get; private set; }
        public bool Loaded { get; private set; }

		private int OpenGLTextureId;

        internal Dictionary<string, uint[]> Animations = new Dictionary<string, uint[]>();

        private bool AutoSize = false;
		public Point FrameSize { get; private set; }
		public Point IndexSize { get; private set; }
        
		internal Sprite(string id, string filename) 
			: this(id, filename, new Point(1f, 1f))
        {
            AutoSize = true;
        }
		internal Sprite(string id, string filename, Point frameSize)
        {
            Id = id;
            Filename = filename;
            Loaded = false;

			FrameSize = frameSize;
			IndexSize = new Point(1f, 1f);
        }

		public void Draw(Point pos, uint frameIndex, Point scale, Point origin, float rotation)

        {
			if (!Loaded)
			{
				if (Resources.JustInTimeLoading)
					Load();
				else
					throw new NotLoadedException();
			}

			//calculate frame coordinates within the texture
			float texX1 = 1f / IndexSize.X * (frameIndex % IndexSize.Y);
			float texY1 = 1f / IndexSize.Y * (frameIndex / IndexSize.Y);
			float texX2 = 1f / IndexSize.X * (frameIndex % IndexSize.Y + 1);
			float texY2 = 1f / IndexSize.Y * (frameIndex / IndexSize.Y + 1);

			//get transformed coordinates on screen using a rectangle
			Rectangle screenRect = new Rectangle(pos, FrameSize * scale, origin * scale, rotation);

			//draw if its in view
			if (screenRect.IntersectsWith(Game.CurrentDrawArea))
			{
				GL.Enable(EnableCap.Texture2D);
				GL.BindTexture(TextureTarget.Texture2D, OpenGLTextureId);
				GL.Color4((OpenTK.Graphics.Color4)Color.White);

				GL.Begin(PrimitiveType.Quads);

				GL.TexCoord2(texX1, texY1);
				GL.Vertex3(screenRect.Point1.X, screenRect.Point1.Y, DrawX.GLDepth);
				GL.TexCoord2(texX2, texY1);
				GL.Vertex3(screenRect.Point2.X, screenRect.Point2.Y, DrawX.GLDepth);
				GL.TexCoord2(texX2, texY2);
				GL.Vertex3(screenRect.Point3.X, screenRect.Point3.Y, DrawX.GLDepth);
				GL.TexCoord2(texX1, texY2);
				GL.Vertex3(screenRect.Point4.X, screenRect.Point4.Y, DrawX.GLDepth);

				GL.End();

				GL.Disable(EnableCap.Texture2D);
			}
        }
		public void Draw(Point pos)
		{
			Draw(pos, 0, new Point(1, 1), new Point(0, 0), 0);
		}
		public void Draw(Point pos, uint frameIndex)
		{
			Draw(pos, frameIndex, new Point(1, 1), new Point(0, 0), 0);
		}
		public void Draw(Point pos, uint frameIndex, float scale)
		{
			Draw(pos, frameIndex, new Point(scale, scale), new Point(0, 0), 0);
		}
		public void Draw(Point pos, uint frameIndex, Point scale)
		{
			Draw(pos, frameIndex, scale, new Point(0, 0), 0);
		}

        public void Load()
        {
			if (!Loaded)
			{
				System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(Resources.GetStream(Filename));
				BitmapData bitmapData = bitmap.LockBits((System.Drawing.Rectangle)new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

				OpenGLTextureId = GL.GenTexture();
				GL.BindTexture(TextureTarget.Texture2D, OpenGLTextureId);

				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmapData.Width, bitmapData.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bitmapData.Scan0);
				bitmap.UnlockBits(bitmapData);
				bitmap.Dispose();

				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

				if (AutoSize)
				{
					FrameSize = new Point(bitmapData.Width, bitmapData.Height);
					IndexSize = new Point(1, 1);
				}
				else
				{
					IndexSize = new Point(bitmapData.Width / FrameSize.X, bitmapData.Height / FrameSize.Y);
				}
                    
				Loaded = true;
			}
        }

        public void Unload()
        {
			if (Loaded)
			{
				GL.DeleteTexture(OpenGLTextureId);
				Loaded = false;
			}
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
