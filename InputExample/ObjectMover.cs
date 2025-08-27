using RendrixEngine;

namespace InputExample
{
    public class ObjectMover : Component
    {
        public float speed = 5;

        public override void Update()
        {
            if(KeyboardInput.GetKey(ConsoleKey.DownArrow))
            {
                Transform.Translate(new Vector3D(0, -speed * Time.DeltaTime, 0));
            }
            if(KeyboardInput.GetKey(ConsoleKey.UpArrow))
            {
                Transform.Translate(new Vector3D(0, speed * Time.DeltaTime, 0));
            }
            if(KeyboardInput.GetKey(ConsoleKey.LeftArrow))
            {
                Transform.Translate(new Vector3D(-speed * Time.DeltaTime, 0, 0));
            }
            if(KeyboardInput.GetKey(ConsoleKey.RightArrow))
            {
                Transform.Translate(new Vector3D(speed * Time.DeltaTime, 0, 0));
            }
            if(KeyboardInput.GetKey(ConsoleKey.Q))
            {
                Transform.Translate(new Vector3D(0, 0, speed * Time.DeltaTime));
            }
            if(KeyboardInput.GetKey(ConsoleKey.E))
            {
                Transform.Translate(new Vector3D(0, 0, -speed * Time.DeltaTime));
            }
        }
    }
}
