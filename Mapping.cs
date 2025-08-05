using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping581
{
    public class Mapping
    {
        public int AreaId { get; set; }
        public List<MappingObject> Objects { get; set; } = new List<MappingObject>();
    }
}
