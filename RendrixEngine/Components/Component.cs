using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RendrixEngine
{
    public abstract class Component
    {
        public virtual bool Enabled { get; set; } = true;
        public virtual Transform Transform { get; set; }

        public virtual void OnAwake()
        {
        }

        public virtual void OnDisable()
        {
        }

        public virtual void Update()
        {
        }
    }
}
