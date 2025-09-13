namespace RendrixEngine
{
    // Basic scene interface: implement to build node hierarchy.
    public interface IScene
    {
        public string Name { get; set; }
        public bool AutoLoad { get; }
        void Setup(SceneNode root);
    }

    // Abstract base class for scenes: auto-registers via constructor
    public abstract class SceneBase : IScene
    {
        public string Name { get; set; }
        public bool AutoLoad { get; protected set; }

        protected SceneBase(string name, bool autoLoad = false)
        {
            Name = name;
            AutoLoad = autoLoad;
            SceneManager.AutoRegisterSceneType(this);
        }

        public abstract void Setup(SceneNode root);
    }

    // Optional lifecycle hooks
    public interface ISceneAwake
    {
        void Awake();
    }

    public interface ISceneStart
    {
        void Start();
    }

    public interface ISceneUnload
    {
        void Unload();
    }
}
