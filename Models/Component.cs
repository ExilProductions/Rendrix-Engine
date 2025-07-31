using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RendrixEngine.Models
{
    public abstract class Component
    {
        public virtual void OnEnable()
        {
            // Default implementation does nothing
        }

        public virtual void OnDisable()
        {
            // Default implementation does nothing
        }

        public virtual void Update(float deltaTime)
        {
            // Default implementation does nothing
        }
    }
}
