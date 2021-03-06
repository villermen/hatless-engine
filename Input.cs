﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SDL2;

namespace HatlessEngine
{
	public static class Input
	{
		private static List<Button> _previousState = new List<Button>();
		private static readonly List<Button> CurrentState = new List<Button>();

		/// <summary>
		/// Buttons mapped to other buttons.
		/// Use this to manage maps.
		/// E.g. Add(Button.KeyboardUp, Button.KeyboardW) will simulate KeyboardW pressed when you press keyboardUp afterwards.
		/// KeyboardW will still function correctly unless KeyboardUp is released when it is held down.
		/// </summary>
		public static Dictionary<Button, Button> ButtonMaps = new Dictionary<Button, Button>();

		public static Point UntranslatedMousePosition { get; private set; }
		public static Point MousePosition { get; private set; }

		/// <summary>
		/// Value between 0 and 1 that the axes of a gamepad must have before triggering the corresponding Gamepad_ buttons.
		/// </summary>
		public static float GamepadDeadZone = 0.2f;

		private static readonly IntPtr[] GamepadHandles = new IntPtr[8];
		private static readonly Dictionary<int, int> GamepadInstanceIDs = new Dictionary<int, int>();
		private static readonly IntPtr[] GamepadHapticHandles = new IntPtr[8];

		private static readonly List<Button>[] GamepadPreviousStates = new List<Button>[8];
		private static readonly List<Button>[] GamepadCurrentStates = new List<Button>[8];
		private static readonly float[,] GamepadAxisValues = new float[8,6];

		/// <summary>
		/// Strings added to this list will have text characters appended or removed by keyboard input.
		/// </summary>
		public static List<StringBuilder> TextInputReceivers = new List<StringBuilder>();

		/// <summary>
		/// Returns true when the specified button is pressed (one step only).
		/// Searches the gamepadstate if gamepadNumber is given. (1-8)
		/// </summary>
		public static bool IsPressed(Button button, int gamepad = 1)
		{
			if ((int)button < 3000)
				return (CurrentState.Contains(button) && !_previousState.Contains(button));
			
			if (GamepadConnected(gamepad))
				return (GamepadCurrentStates[gamepad - 1].Contains(button) && !GamepadPreviousStates[gamepad - 1].Contains(button));
			
			return false;
		}

		/// <summary>
		/// Returns true when the specified button is being held down (every step).
		/// Searches the gamepadstate if gamepadNumber is given. (1-8)
		/// </summary>
		public static bool IsDown(Button button, int gamepad = 1)
		{
			if ((int)button < 3000)
				return CurrentState.Contains(button);

			if (GamepadConnected(gamepad))
				return (GamepadCurrentStates[gamepad - 1].Contains(button));

			return false;
		}
		/// <summary>
		/// Returns true when the specified button is released (one step only).
		/// Searches the gamepadstate if gamepadNumber is given. (1-8)
		/// </summary>
		public static bool IsReleased(Button button, int gamepad = 1)
		{
			if ((int)button < 3000)
				return (!CurrentState.Contains(button) && _previousState.Contains(button));

			if (GamepadConnected(gamepad))
				return (!GamepadCurrentStates[gamepad - 1].Contains(button) && GamepadPreviousStates[gamepad - 1].Contains(button));
			
			return false;
		}

		/// <summary>
		/// Returns whether the given gamepad is connected.
		/// </summary>
		public static bool GamepadConnected(int gamepad = 1)
		{
			if (gamepad < 1 || gamepad > 8)
				throw new ArgumentOutOfRangeException("gamepad", "gamepad can be 1-8");

			return GamepadHandles[gamepad - 1] != IntPtr.Zero;
		}

		/// <summary>
		/// Returns whether the given gamepad supports the rumble feature.
		/// </summary>
		public static bool GamepadRumbleSupported(int gamepad)
		{
			if (gamepad < 1 || gamepad > 8)
				throw new ArgumentOutOfRangeException("gamepad", "gamepad can be 1-8");

			return GamepadHandles[gamepad] != IntPtr.Zero;
		}

		/// <summary>
		/// Gets the coordinate-system corrected stick position for a gamepad.
		/// </summary>
		public static Point GetGamepadStickPosition(int gamepad, bool leftStick = true, bool respectDeadZone = true)
		{
			if (!GamepadConnected(gamepad)) 
				return Point.Zero;

			Point position = Point.Zero;
			byte startAxis = 0;
			if (!leftStick)
				startAxis = 2;

			if (!respectDeadZone || GamepadAxisValues[gamepad - 1, startAxis] <= -GamepadDeadZone || GamepadAxisValues[gamepad - 1, startAxis] >= GamepadDeadZone)
				position.X = GamepadAxisValues[gamepad - 1, startAxis];
			if (!respectDeadZone || GamepadAxisValues[gamepad - 1, startAxis + 1] <= -GamepadDeadZone || GamepadAxisValues[gamepad - 1, startAxis + 1] >= GamepadDeadZone)
				position.Y = GamepadAxisValues[gamepad - 1, startAxis + 1];

			return position;
		}

		/// <summary>
		/// Gets a trigger's value for a gamepad.
		/// </summary>
		public static float GetTriggerValue(int gamepad, bool leftTrigger = true, bool respectDeadZone = true)
		{
			if (!GamepadConnected(gamepad)) 
				return 0f;

			byte axis = 4;
			if (!leftTrigger)
				axis = 5;

			if (!respectDeadZone || GamepadAxisValues[gamepad - 1, axis] >= GamepadDeadZone)
				return GamepadAxisValues[gamepad - 1, axis];

			return 0f;
		}

		/// <summary>
		/// Will make the specified controller rumble if it supports it.
		/// Duration is in seconds.
		/// </summary>
		public static void Rumble(int gamepad, float strength = 1f, float duration = 1f)
		{
			if (strength < 0 || strength > 1)
				throw new ArgumentOutOfRangeException("strength", "strength can be 0f-1f");
			if (duration < 0)
				throw new ArgumentOutOfRangeException("duration", "duration can't be negative");

			if (GamepadConnected(gamepad) && GamepadRumbleSupported(gamepad))
				SDL.SDL_HapticRumblePlay(GamepadHapticHandles[gamepad - 1], strength, (uint)(1000 * duration));
		}

		/// <summary>
		/// Sets PreviousState to the CurrentState and removes any buttons that are only supposed to be pressed.
		/// </summary>
		internal static void PushState()
		{
			//remove buttons that can technically only be pressed from both states
			CurrentState.RemoveAll(b => 
			(
				b == Button.MousewheelUp || 
				b == Button.MousewheelDown || 
				b == Button.MousewheelLeft || 
				b == Button.MousewheelRight
			));

			_previousState = new List<Button>(CurrentState);

			for (int i = 0; i < 8; i++)
			{
				if (GamepadHandles[i] != IntPtr.Zero)
				{
					GamepadPreviousStates[i] = new List<Button>(GamepadCurrentStates[i]);
				}
			}
		}

		internal static void InputEvent(SDL.SDL_Event e)
		{
			switch (e.type)
			{
				case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
				{
					CurrentState.Add((Button)(1000 + e.button.button));
					break;
				}

				case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
				{
					CurrentState.Remove((Button)(1000 + e.button.button));
					break;
				}

				case SDL.SDL_EventType.SDL_MOUSEWHEEL:
				{
					if (e.wheel.y > 0)
						CurrentState.Add(Button.MousewheelUp);
					if (e.wheel.y < 0)
						CurrentState.Add(Button.MousewheelDown);
					if (e.wheel.x < 0)
						CurrentState.Add(Button.MousewheelLeft);
					if (e.wheel.x > 0)
						CurrentState.Add(Button.MousewheelRight);
					break;
				}

				case SDL.SDL_EventType.SDL_MOUSEMOTION:
				{
					UntranslatedMousePosition = new Point(e.motion.x, e.motion.y);
					break;
				}

				case SDL.SDL_EventType.SDL_KEYDOWN:
				{
					if (e.key.repeat == 0) //we dont do repeats (yet?)
					{
						int sdlKeyDown = (int)e.key.keysym.sym;
						if (sdlKeyDown < 65536)
							CurrentState.Add((Button)(2000 + sdlKeyDown));
						else
							CurrentState.Add((Button)(sdlKeyDown - 1073739381));
					}

					//textinput special keys
					switch (e.key.keysym.sym)
					{
						//backspace removes last character from all text input builders
						case SDL.SDL_Keycode.SDLK_BACKSPACE:
							TextInputReceivers.ForEach(builder =>
							{
								int length = builder.Length;
								if (length > 0)
									builder.Remove(length - 1, 1);
							});
							break;

						case SDL.SDL_Keycode.SDLK_RETURN:
							TextInputReceivers.ForEach(builder => builder.Append('\n'));
							break;

						case SDL.SDL_Keycode.SDLK_TAB:
							TextInputReceivers.ForEach(builder => builder.Append('\t'));
							break;
					}

					break;
				}

				case SDL.SDL_EventType.SDL_KEYUP:
				{
					int sdlKeyUp = (int)e.key.keysym.sym;
					if (sdlKeyUp < 65536)
						CurrentState.Remove((Button)(2000 + sdlKeyUp));
					else
						CurrentState.Remove((Button)(sdlKeyUp - 1073739381));
					break;
				}

				case SDL.SDL_EventType.SDL_TEXTINPUT:
				{
					byte[] rawBytes = new byte[SDL.SDL_TEXTINPUTEVENT_TEXT_SIZE];
					unsafe { Marshal.Copy((IntPtr)e.text.text, rawBytes, 0, SDL.SDL_TEXTINPUTEVENT_TEXT_SIZE); }
					int length = Array.IndexOf<byte>(rawBytes, 0);
					string text = Encoding.UTF8.GetString(rawBytes, 0, length);
					//add text to all stringbuilders in TextInputReceivers
					TextInputReceivers.ForEach(builder => builder.Append(text));
					break;
				}

				case SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED:
				{
					//get first free gamepad slot
					int newGamepadId = -1;
					while (GamepadHandles[++newGamepadId] != IntPtr.Zero) { }
					GamepadHandles[newGamepadId] = SDL.SDL_GameControllerOpen(e.cdevice.which);

					IntPtr joystick = SDL.SDL_GameControllerGetJoystick(GamepadHandles[newGamepadId]);
					GamepadInstanceIDs.Add(SDL.SDL_JoystickInstanceID(joystick), newGamepadId);
					GamepadCurrentStates[newGamepadId] = new List<Button>();
					GamepadPreviousStates[newGamepadId] = new List<Button>();

					//init rumble if supported
					if (SDL.SDL_JoystickIsHaptic(joystick) == 1)
					{
						IntPtr hapticHandle = SDL.SDL_HapticOpenFromJoystick(joystick);
						if (SDL.SDL_HapticRumbleSupported(hapticHandle) == 1)
						{
							SDL.SDL_HapticRumbleInit(hapticHandle);
							GamepadHapticHandles[newGamepadId] = hapticHandle;
						}
						else
							SDL.SDL_HapticClose(hapticHandle);
					}
					break;
				}

				case SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMOVED:
				{
					int gamepadId = GamepadInstanceIDs[e.cdevice.which];
					SDL.SDL_GameControllerClose(GamepadHandles[gamepadId]);
					GamepadHandles[gamepadId] = IntPtr.Zero;
					GamepadInstanceIDs.Remove(e.cdevice.which);

					if (GamepadHapticHandles[gamepadId] != IntPtr.Zero)
					{
						SDL.SDL_HapticClose(GamepadHapticHandles[gamepadId]);
						GamepadHapticHandles[gamepadId] = IntPtr.Zero;
					}
					break;
				}

				case SDL.SDL_EventType.SDL_CONTROLLERBUTTONDOWN:
				{
					GamepadCurrentStates[GamepadInstanceIDs[e.cbutton.which]].Add((Button)(3000 + e.cbutton.button));
					break;
				}

				case SDL.SDL_EventType.SDL_CONTROLLERBUTTONUP:
				{
					GamepadCurrentStates[GamepadInstanceIDs[e.cbutton.which]].Remove((Button)(3000 + e.cbutton.button));
					break;
				}

				case SDL.SDL_EventType.SDL_CONTROLLERAXISMOTION:
				{
					int gamepad = GamepadInstanceIDs[e.caxis.which];
					byte axis = e.caxis.axis;
					float value = (float)e.caxis.axisValue / short.MaxValue;

					GamepadAxisValues[gamepad, axis] = value;

					if (value <= -GamepadDeadZone)
					{
						//add button if it's not added yet
						Button button = (Button)(3015 + e.caxis.axis * 2);
						if (!GamepadCurrentStates[gamepad].Contains(button))
							GamepadCurrentStates[gamepad].Add(button);
					}
					else if (value >= GamepadDeadZone)
					{
						Button button = (Button)(3016 + e.caxis.axis * 2);
						if (!GamepadCurrentStates[gamepad].Contains(button))
							GamepadCurrentStates[gamepad].Add(button);

					}
					else
					{
						//remove both buttons for this axis if it's not in range
						if (e.caxis.axis < 4)
							GamepadCurrentStates[gamepad].Remove((Button)(3015 + e.caxis.axis * 2));
						GamepadCurrentStates[gamepad].Remove((Button)(3016 + e.caxis.axis * 2));
					}
					break;
				}
			}
		}

		/// <summary>
		/// SDL.SDL_s eventloop only sends a mouse event when it moves, and MousePosition might change because of changes in a view or the window.
		/// </summary>
		internal static void UpdateMousePosition()
		{
			List<View> views = Resources.Get<View>();

			//decide on which viewport the mouse currently is. backwards because the last view created will be in front of the one before it (if they overlap)
			for (int i = views.Count - 1; i >= 0; i--)
			{
				View view = views.ElementAt(i);
				Rectangle absoluteGameArea = view.GetAbsoluteGameArea();
				Rectangle absoluteViewport = view.GetAbsoluteViewport();

				if (!absoluteViewport.IntersectsWith(UntranslatedMousePosition)) 
					continue;

				//found it! let's calculate and ditch this place
				MousePosition = absoluteGameArea.Position + (UntranslatedMousePosition - absoluteViewport.Position) / absoluteViewport.Size * absoluteGameArea.Size;
				break;
			}
		}

		/// <summary>
		/// Adds all mappings to the currentstates.
		/// </summary>
		internal static void ApplyButtonMaps()
		{
			foreach (KeyValuePair<Button, Button> buttonPair in ButtonMaps)
			{
				if ((int)buttonPair.Key < 3000 && (int)buttonPair.Value < 3000) //non-gamepad to non-gamepad map: add value when currentstate has key and no value
				{
					if (IsPressed(buttonPair.Key))
					{
						if (!CurrentState.Contains(buttonPair.Value))
							CurrentState.Add(buttonPair.Value);
					}
					else if (IsReleased(buttonPair.Key))
						CurrentState.Remove(buttonPair.Value);
				}
				else if ((int)buttonPair.Key >= 3000 && (int)buttonPair.Value >= 3000) //gamepad to gamepad map: add value for all connected gamepads that have key and no value
				{
					for (int i = 0; i < 8; i++)
					{
						if (GamepadHandles[i] != IntPtr.Zero)
						{
							if (IsPressed(buttonPair.Key, i + 1))
							{
								if (!GamepadCurrentStates[i].Contains(buttonPair.Value))
									GamepadCurrentStates[i].Add(buttonPair.Value);
							}
							else if (IsReleased(buttonPair.Key, i + 1))
								GamepadCurrentStates[i].Remove(buttonPair.Value);
						}
					}
				}
				else if ((int)buttonPair.Key < 3000 && (int)buttonPair.Value >= 3000) //non-gamepad to gamepad map: add value to all connected gamepads that don't have value
				{
					if (IsPressed(buttonPair.Key))
					{
						for (int i = 0; i < 8; i++)
						{
							if (GamepadHandles[i] != IntPtr.Zero)
							{
								if (!GamepadCurrentStates[i].Contains(buttonPair.Value))
									GamepadCurrentStates[i].Add(buttonPair.Value);
							}
						}
					}
					else if (IsReleased(buttonPair.Key))
					{
						for (int i = 0; i < 8; i++)
						{
							if (GamepadHandles[i] != IntPtr.Zero)
							{
								GamepadCurrentStates[i].Remove(buttonPair.Value);
							}
						}
					}
				}
				else //gamepad to non-gamepad map: add value to currentstate when any of the connected gamepads has key pressed
				{
					for (int i = 0; i < 8; i++)
					{
						if (GamepadHandles[i] != IntPtr.Zero)
						{
							if (IsPressed(buttonPair.Key, i + 1))
							{
								if (!CurrentState.Contains(buttonPair.Value))
								{
									CurrentState.Add(buttonPair.Value);
									break;
								}
							}
							else if (IsReleased(buttonPair.Key, i + 1))
							{
								CurrentState.Remove(buttonPair.Value);
								break;
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Returns a string with all pressed buttons, for debugging purposes only.
		/// </summary>
		public static string GetStateInformation()
		{
			//mouse/keyboard info
			string str = "Pressed buttons: ";
			if (CurrentState.Count > 0)
			{
				str = CurrentState.Aggregate(str, 
					(current, button) => current + button + ", ");

				str = str.Substring(0, str.Length - 2);
			}

			//gamepad info
			for(byte i = 0; i < 8; i++)
			{
				if (GamepadHandles[i] == IntPtr.Zero) 
					continue;

				str += "\nGamepad " + (i + 1) + ": ";

				if (GamepadCurrentStates[i].Count <= 0) 
					continue;

				str = GamepadCurrentStates[i].Aggregate(str, 
					(current, button) => current + button + ", ");

				str = str.Substring(0, str.Length - 2);
			}
				
			return str;
		}

		/// <summary>
		/// Cleanup.
		/// </summary>
		internal static void CloseGamepads()
		{
			for (int i = 0; i < 8; i++)
			{
				if (GamepadHandles[i] == IntPtr.Zero) 
					continue;

				SDL.SDL_GameControllerClose(GamepadHandles[i]);
				GamepadHandles[i] = IntPtr.Zero;

				if (GamepadHapticHandles[i] == IntPtr.Zero) 
					continue;

				SDL.SDL_HapticClose(GamepadHapticHandles[i]);
				GamepadHapticHandles[i] = IntPtr.Zero;
			}
			GamepadInstanceIDs.Clear();
		}
	}
}