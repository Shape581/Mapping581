using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping581
{
    public class MappingObject
    {
        public int ObjectId { get; set; }
        public int ModelId { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public float RotationX { get; set; }
        public float RotationY { get; set; }
        public float RotationZ { get; set; }
        public bool Interior { get; set; }
        public string SteamId { get; set; }
        public string Data { get; set; }
    }
}
