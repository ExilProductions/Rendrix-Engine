using RendrixEngine.Audio;

namespace RendrixEngine
{
    public sealed class AudioListener : Component
    {
        public static AudioListener Main { get; private set; }

        public override void OnAwake()
        {
            if (Main == null)
                Main = this;
        }

        public override void OnDisable()
        {
            if (Main == this)
                Main = null;
        }
    }
}
