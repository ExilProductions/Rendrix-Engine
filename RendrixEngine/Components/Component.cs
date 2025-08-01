using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RendrixEngine.Components
{
    public abstract class Component
    {
        public virtual Transform Transform { get; set; }

        public virtual void OnEnable()
        {
        }

        public virtual void OnDisable()
        {
        }

        public virtual void Update(float deltaTime)
        {
        }
    }
}
