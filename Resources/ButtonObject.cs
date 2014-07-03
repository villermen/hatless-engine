﻿using System;

namespace HatlessEngine
{
	/// <summary>
	/// Class using PhysicalObject to easily define buttons.
	/// Frame 1: nothing, 2: mouseover, 3: mousedown
	/// </summary>
	public class ButtonObject : PhysicalObject
	{
		private int _State = 0;
		/// <summary>
		/// Current state of the button
		/// 0: nothing, 1: mouseover, 2: mousedown
		/// </summary>
		public int State
		{
			get { return _State; }
			private set { _State = value; } 
		}
		private Sprite Sprite;

		public ButtonObject(Point position, Sprite sprite) : base(position)
		{
			Sprite = sprite;
			Bounds = new Rectangle(position, sprite.FrameSize);
		}

		public sealed override void Step()
		{
			if (Bounds.IntersectsWith(Input.MousePosition))
			{
				//mouseover
				if (State == 0)
					State = 1;

				//pressed
				if (State == 1 && Input.IsPressed(Button.MouseLeft))
					State = 2;

				//clicked
				if (State == 2 && Input.IsReleased(Button.MouseLeft))
				{
					OnClick();
					State = 1;
				}
			}
			else
				State = 0;
		}

		public sealed override void Draw()
		{
			Sprite.Draw(Position, State);
		}

		/// <summary>
		/// Triggers once when the mouse is released after being pressed on the button and never having left it.
		/// </summary>
		public virtual void OnClick() { }
	}
}