using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace RendrixEngine.Input
{
    public enum MouseKey
    {
        Left,
        Right,
        Middle
    }

    public static class MouseInput
    {
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out CursorPos lpPoint);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        public struct CursorPos
        {
            public int X;
            public int Y;
        }

        private static readonly Dictionary<MouseKey, int> vkMap = new()
        {
            { MouseKey.Left, 0x01 },   // VK_LBUTTON
            { MouseKey.Right, 0x02 },  // VK_RBUTTON
            { MouseKey.Middle, 0x04 }  // VK_MBUTTON
        };

        private static readonly Dictionary<MouseKey, bool> buttonStates = new()
        {
            { MouseKey.Left, false },
            { MouseKey.Right, false },
            { MouseKey.Middle, false }
        };

        public static CursorPos CursorPosition { get; private set; }

        private static readonly Thread inputThread;

        static MouseInput()
        {
            inputThread = new Thread(InputLoop) { IsBackground = true };
            inputThread.Start();
        }

        private static void InputLoop()
        {
            while (true)
            {
                CursorPos pos;
                GetCursorPos(out pos);
                CursorPosition = pos;

                foreach (var kv in vkMap)
                {
                    bool isDown = (GetAsyncKeyState(kv.Value) & 0x8000) != 0;
                    buttonStates[kv.Key] = isDown;
                }
            }
        }

        public static bool GetButton(MouseKey key)
        {
            return buttonStates.TryGetValue(key, out var pressed) && pressed;
        }

        public static CursorPos GetCursorPosition() => CursorPosition;
    }
}
