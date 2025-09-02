using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using System;
using System.Collections.Generic;

namespace RendrixEngine
{
    public class InputManager
    {
        private readonly Window _window;

        private HashSet<Key> _keysDown = new();
        private HashSet<Key> _keysPressedThisFrame = new();
        private HashSet<Key> _keysReleasedThisFrame = new();

        private HashSet<MouseButton> _mouseDown = new();
        private HashSet<MouseButton> _mousePressedThisFrame = new();
        private HashSet<MouseButton> _mouseReleasedThisFrame = new();

        private Point _lastMousePosition;
        private Vector _mouseDelta;

        private static InputManager _instance;

        public static InputManager Instance => _instance ?? throw new Exception("InputManager not initialized");

        public InputManager(Window window)
        {
            if (_instance != null)
                throw new Exception("InputManager already initialized");

            _window = window;

            
            _window.KeyDown += OnKeyDown;
            _window.KeyUp += OnKeyUp;
            _window.PointerMoved += OnPointerMoved;
            _window.PointerPressed += OnPointerPressed;
            _window.PointerReleased += OnPointerReleased;

            _lastMousePosition = new Point(0, 0);
            _mouseDelta = new Vector(0, 0);

            
            Dispatcher.UIThread.Post(DispatcherUpdate, DispatcherPriority.Render);

            _instance = this;
        }

        
        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (!_keysDown.Contains(e.Key))
            {
                _keysDown.Add(e.Key);
                _keysPressedThisFrame.Add(e.Key);
            }
        }

        private void OnKeyUp(object? sender, KeyEventArgs e)
        {
            _keysDown.Remove(e.Key);
            _keysReleasedThisFrame.Add(e.Key);
        }

        
        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            var pos = e.GetPosition(_window);
            _mouseDelta = pos - _lastMousePosition;
            _lastMousePosition = pos;
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var button = e.GetCurrentPoint(_window).Properties.PointerUpdateKind switch
            {
                PointerUpdateKind.LeftButtonPressed => MouseButton.Left,
                PointerUpdateKind.RightButtonPressed => MouseButton.Right,
                PointerUpdateKind.MiddleButtonPressed => MouseButton.Middle,
                _ => MouseButton.None
            };

            if (button != MouseButton.None && !_mouseDown.Contains(button))
            {
                _mouseDown.Add(button);
                _mousePressedThisFrame.Add(button);
            }
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            var button = e.GetCurrentPoint(_window).Properties.PointerUpdateKind switch
            {
                PointerUpdateKind.LeftButtonReleased => MouseButton.Left,
                PointerUpdateKind.RightButtonReleased => MouseButton.Right,
                PointerUpdateKind.MiddleButtonReleased => MouseButton.Middle,
                _ => MouseButton.None
            };

            if (button != MouseButton.None)
            {
                _mouseDown.Remove(button);
                _mouseReleasedThisFrame.Add(button);
            }
        }

        
        
        
        private void DispatcherUpdate()
        {
            Update(); 

            
            Dispatcher.UIThread.Post(DispatcherUpdate, DispatcherPriority.Render);
        }

        private void Update()
        {
            _keysPressedThisFrame.Clear();
            _keysReleasedThisFrame.Clear();
            _mousePressedThisFrame.Clear();
            _mouseReleasedThisFrame.Clear();
            _mouseDelta = new Vector(0, 0);
        }

        
        public static bool GetKey(Key key) => Instance._keysDown.Contains(key);
        public static bool GetKeyDown(Key key) => Instance._keysPressedThisFrame.Contains(key);
        public static bool GetKeyUp(Key key) => Instance._keysReleasedThisFrame.Contains(key);

        
        public static bool GetMouseButton(MouseButton button) => Instance._mouseDown.Contains(button);
        public static bool GetMouseButtonDown(MouseButton button) => Instance._mousePressedThisFrame.Contains(button);
        public static bool GetMouseButtonUp(MouseButton button) => Instance._mouseReleasedThisFrame.Contains(button);

        public static Point MousePosition => Instance._lastMousePosition;
        public static Vector MouseDelta => Instance._mouseDelta;
    }
}
