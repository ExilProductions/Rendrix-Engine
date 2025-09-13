using RendrixEngine;
using Avalonia.Input;
using System.Numerics;

namespace InputExample
{
    public class ObjectMover : Component
    {
        public float speed = 5;

        public override void Update()
        {
            
            if (InputManager.GetKey(Key.Down))
            {
                Transform.Translate(new Vector3(0, -speed * Time.DeltaTime, 0));
            }

            
            if (InputManager.GetKey(Key.Up))
            {
                Transform.Translate(new Vector3(0, speed * Time.DeltaTime, 0));
            }

            
            if (InputManager.GetKey(Key.Left))
            {
                Transform.Translate(new Vector3(-speed * Time.DeltaTime, 0, 0));
            }

            
            if (InputManager.GetKey(Key.Right))
            {
                Transform.Translate(new Vector3(speed * Time.DeltaTime, 0, 0));
            }

            
            if (InputManager.GetKey(Key.Q))
            {
                Transform.Translate(new Vector3(0, 0, speed * Time.DeltaTime));
            }

            
            if (InputManager.GetKey(Key.E))
            {
                Transform.Translate(new Vector3(0, 0, -speed * Time.DeltaTime));
            }
        }
    }
}
