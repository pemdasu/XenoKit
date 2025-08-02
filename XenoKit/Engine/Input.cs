using System;
using Microsoft.Xna.Framework.Input;
using MonoGame.Framework.WpfInterop.Input;
using Xv2CoreLib.Resource.App;
using SimdVector2 = System.Numerics.Vector2;

namespace XenoKit.Engine
{
    public enum MouseButtons { Left, Right, Middle, X1, X2 };

    public class Input
    {
        private static readonly bool[] ExclusiveKeys = new bool[255];

        public MouseState MouseState { get; private set; }
        public MouseState PreviousMouseState { get; private set; }
        public KeyboardState KeyboardState { get; private set; }

        private SimdVector2 _prevMousePos;
        private SimdVector2 _mousePos;
        private SimdVector2 _scaledMousePos;
        private SimdVector2 _axisCorrectMousePos;
        public SimdVector2 MousePosition => _mousePos;
        public SimdVector2 ScaledMousePosition => _scaledMousePos;
        public SimdVector2 AxisCorrectMousePosition => _axisCorrectMousePos;
        public SimdVector2 MouseDelta { get; private set; }

        //public Vector2 MousePosition {  get { return MouseState.Position.ToVector2(); } }
        //public Vector2 AltMousePosition => new Vector2(MouseState.Position.X, MouseState.Position.Y);

        //Scrolling
        /// <summary>
        /// Current MouseWheelValue, as of the previous frame. 
        /// </summary>
        private int CurrentMouseWheelValue = 0;
        public int MouseScrollThisFrame = 0;

        private object leftClickHeldDownContext;
        private object rightClickHeldDownContext;
        private ulong dragFinishedAtTick = 0;

        private bool isLeftClickDown = false;
        private bool isRightClickDown = false;
        private ulong leftClickTick = 0;
        private ulong rightClickTick = 0;
        private int numLeftClicks = 0;
        private int numRightClicks = 0;
        private SimdVector2 _mouseLocationAtLeftDoubleClickStart;
        private SimdVector2 _mouseLocationAtRightDoubleClickStart;

        /// <summary>
        /// Returns whether the left mouse button was pressed (single press). Will not return true during drag events.
        /// </summary>
        public bool IsMouseLeftClickDown {  get; private set; }
        /// <summary>
        /// Returns whether the right mouse button was pressed (single press). Will not return true during drag events.
        /// </summary>
        public bool IsMouseRightClickDown { get; private set; }
        /// <summary>
        /// Returns whether the left mouse button was pressed (double press). Will not return true during drag events.
        /// </summary>
        public bool IsMouseDoubleLeftClickDown { get; private set; }
        /// <summary>
        /// Returns whether the right mouse button was pressed (double press). Will not return true during drag events.
        /// </summary>
        public bool IsMouseDoubleRightClickDown { get; private set; }

        //Const
        private const int ClickEventThreshold = 45;
        private const int DragEndClickDelay = 20;

        public void Update(WpfMouse mouse, WpfKeyboard keyboard)
        {
            //Clear previous state
            for (int i = 0; i < ExclusiveKeys.Length; i++)
                ExclusiveKeys[i] = false;

            IsMouseLeftClickDown = false;
            IsMouseRightClickDown = false;
            IsMouseDoubleLeftClickDown = false;
            IsMouseDoubleRightClickDown = false;

            PreviousMouseState = MouseState;
            _prevMousePos = _mousePos;
            MouseState = mouse.GetState();
            KeyboardState = keyboard.GetState();

            //_mousePos = new Vector2((game.GraphicsDevice.Viewport.Width - MouseState.X) * game.SuperSamplingFactor, MouseState.Y * game.SuperSamplingFactor);
            _mousePos = MouseState.Position.ToNumeric();
            _scaledMousePos = _mousePos * SettingsManager.settings.XenoKit_SuperSamplingFactor;
            _axisCorrectMousePos = new SimdVector2(Viewport.Instance.GraphicsDevice.Viewport.Width - _mousePos.X, _mousePos.Y);
            MouseDelta = _mousePos - _prevMousePos;

            //Update scroll
            MouseScrollThisFrame = MouseState.ScrollWheelValue - CurrentMouseWheelValue;
            CurrentMouseWheelValue = MouseState.ScrollWheelValue;

            //Events
            HandleMouseClicks();
        }

        private void HandleMouseClicks()
        {
            //handle left clicks
            if((Viewport.Instance.Tick - leftClickTick) > ClickEventThreshold || numLeftClicks >= 2 || HasDragEvent(MouseButtons.Left))
            {
                leftClickTick = Viewport.Instance.Tick;
                isLeftClickDown = false;
                numLeftClicks = 0;
            }

            if(MouseState.LeftButton == ButtonState.Pressed && !isLeftClickDown)
            {
                isLeftClickDown = true;
            }
            else if(MouseState.LeftButton == ButtonState.Released && isLeftClickDown)
            {
                numLeftClicks++;
                isLeftClickDown = false;

                if (numLeftClicks == 1)
                {
                    IsMouseLeftClickDown = true;
                    _mouseLocationAtLeftDoubleClickStart = MousePosition;
                }
                else if (numLeftClicks > 1)
                {
                    if (!CheckMousePositionForDrag(_mouseLocationAtLeftDoubleClickStart))
                        IsMouseDoubleLeftClickDown = true;
                }
            }

            //handle right clicks
            if ((Viewport.Instance.Tick - rightClickTick) > ClickEventThreshold || numRightClicks >= 2 || HasDragEvent(MouseButtons.Right))
            {
                rightClickTick = Viewport.Instance.Tick;
                isRightClickDown = false;
                numRightClicks = 0;
            }

            if (MouseState.RightButton == ButtonState.Pressed && !isRightClickDown)
            {
                isRightClickDown = true;
            }
            else if (MouseState.RightButton == ButtonState.Released && isRightClickDown)
            {
                numRightClicks++;
                isRightClickDown = false;

                if (numRightClicks == 1)
                {
                    IsMouseRightClickDown = true;
                    _mouseLocationAtRightDoubleClickStart = MousePosition;
                }
                else if (numRightClicks > 1)
                {
                    if (!CheckMousePositionForDrag(_mouseLocationAtRightDoubleClickStart))
                        IsMouseDoubleRightClickDown = true;
                }
            }
        }

        public bool CheckMousePositionForDrag(SimdVector2 initialPos)
        {
            return SimdVector2.Distance(initialPos, MousePosition) > 5;
        }

        #region Mouse

        public bool WasButtonHeld(MouseButtons button)
        {
            return (GetButtonState(button, MouseState) == ButtonState.Pressed
                    && GetButtonState(button, PreviousMouseState) == ButtonState.Pressed);
        }

        public ButtonState GetButtonState(MouseButtons button, MouseState state)
        {
            if (button == MouseButtons.Left)
                return state.LeftButton;
            if (button == MouseButtons.Middle)
                return state.MiddleButton;
            if (button == MouseButtons.Right)
                return state.RightButton;
            if (button == MouseButtons.X1)
                return state.XButton1;
            if (button == MouseButtons.X2)
                return state.XButton2;

            return ButtonState.Released;
        }

        public void RegisterDragEvent(MouseButtons button, object context)
        {
            if (button == MouseButtons.Left)
            {
                leftClickHeldDownContext = context;
            }
            else if (button == MouseButtons.Right)
            {
                rightClickHeldDownContext = context;
            }
            else
            {
                throw new ArgumentException("Input.RegisterDragEvent: invalid mouse button, this method only accepts the Left and Right mouse buttons");
            }
        }

        public bool HasDragEvent(MouseButtons button)
        {
            if (button == MouseButtons.Left)
            {
                return leftClickHeldDownContext != null;
            }
            else if (button == MouseButtons.Right)
            {
                return rightClickHeldDownContext != null;
            }
            else
            {
                throw new ArgumentException("Input.HasDragEvent: invalid mouse button, this method only accepts the Left and Right mouse buttons");
            }
        }

        public bool HasDragEvent(MouseButtons button, object context)
        {
            if (button == MouseButtons.Left)
            {
                return leftClickHeldDownContext == context;
            }
            else if (button == MouseButtons.Right)
            {
                return rightClickHeldDownContext == context;
            }
            else
            {
                throw new ArgumentException("Input.HasDragEventFor: invalid mouse button, this method only accepts the Left and Right mouse buttons");
            }
        }

        public void ClearDragEvent(MouseButtons button)
        {
            dragFinishedAtTick = Viewport.Instance.Tick;

            if (button == MouseButtons.Left)
            {
                leftClickHeldDownContext = null;
            }
            else if (button == MouseButtons.Right)
            {
                rightClickHeldDownContext = null;
            }
            else
            {
                throw new ArgumentException("Input.ClearDragEvent: invalid mouse button, this method only accepts the Left and Right mouse buttons");
            }
        }

        public void ClearDragEvent(MouseButtons button, object context)
        {
            if (button == MouseButtons.Left)
            {
                if(leftClickHeldDownContext == context)
                {
                    leftClickHeldDownContext = null;
                    dragFinishedAtTick = Viewport.Instance.Tick;
                }
            }
            else if (button == MouseButtons.Right)
            {
                if(rightClickHeldDownContext == context)
                {
                    rightClickHeldDownContext = null;
                    dragFinishedAtTick = Viewport.Instance.Tick;
                }
            }
            else
            {
                throw new ArgumentException("Input.ClearDragEvent: invalid mouse button, this method only accepts the Left and Right mouse buttons");
            }
        }

        #endregion

        #region Keyboard
        public bool IsKeyDown(Keys key)
        {
            if (ExclusiveKeys[(int)key])
               return false;

            return KeyboardState.IsKeyDown(key);
        }

        public bool IsKeyUp(Keys key)
        {
            return KeyboardState.IsKeyUp(key);
        }

        /// <summary>
        /// Register a key for "exclusive use" during this frame. Any further calls to <see cref="IsKeyDown(Keys)"/> with this key will return false during this frame.
        /// </summary>
        /// <param name="key"></param>
        public void ExclusiveKeyDown(Keys key)
        {
            ExclusiveKeys[(int)key] = true;
        }
        #endregion
    }

}
