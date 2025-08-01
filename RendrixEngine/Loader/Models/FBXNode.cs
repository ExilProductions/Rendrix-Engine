using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RendrixEngine.Loader.Models
{
    public class FBXNode
    {
        public string Name { get; set; }
        public List<object> Properties { get; } = new List<object>();
        public List<FBXNode> Children { get; } = new List<FBXNode>();
    }
}
