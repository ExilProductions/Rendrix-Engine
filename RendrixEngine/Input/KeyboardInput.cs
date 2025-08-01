using System.Collections.Concurrent;

namespace RendrixEngine.Input
{
    public class KeyboardInput
    {
        private static readonly HashSet<ConsoleKey> keysDown = new();
        private static readonly HashSet<ConsoleKey> keysHeld = new();
        private static readonly HashSet<ConsoleKey> keysUp = new();

        private static readonly ConcurrentQueue<ConsoleKey> pressedQueue = new();
        private static readonly ConcurrentQueue<ConsoleKey> releasedQueue = new();

        private static readonly Timer updateTimer;
        private static readonly Thread inputThread;

        static KeyboardInput()
        {
            inputThread = new Thread(InputLoop) { IsBackground = true };
            inputThread.Start();

            updateTimer = new Timer(_ => ProcessInput(), null, 0, 16);
        }
        public static bool GetKeyDown(ConsoleKey key) => keysDown.Contains(key);
        public static bool GetKey(ConsoleKey key) => keysHeld.Contains(key);
        public static bool GetKeyUp(ConsoleKey key) => keysUp.Contains(key);
        private static void InputLoop()
        {
            var keyState = new Dictionary<ConsoleKey, bool>();

            while (true)
            {
                if (!Console.KeyAvailable)
                {
                    Thread.Sleep(1);
                    continue;
                }

                var keyInfo = Console.ReadKey(true);
                var key = keyInfo.Key;

                lock (keyState)
                {
                    if (!keyState.ContainsKey(key) || !keyState[key])
                    {
                        pressedQueue.Enqueue(key);
                        keyState[key] = true;
                    }
                }
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    Thread.Sleep(100);
                    lock (keyState)
                    {
                        if (keyState.TryGetValue(key, out bool isDown) && isDown)
                        {
                            keyState[key] = false;
                            releasedQueue.Enqueue(key);
                        }
                    }
                });
            }
        }
        private static void ProcessInput()
        {
            keysDown.Clear();
            keysUp.Clear();

            while (pressedQueue.TryDequeue(out var key))
            {
                if (!keysHeld.Contains(key))
                {
                    keysDown.Add(key);
                    keysHeld.Add(key);
                }
            }

            while (releasedQueue.TryDequeue(out var key))
            {
                if (keysHeld.Contains(key))
                {
                    keysHeld.Remove(key);
                    keysUp.Add(key);
                }
            }
        }
    }
}
