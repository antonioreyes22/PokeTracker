using System;
using System.Collections.Generic;
using System.Text;

namespace PokeTracker.Models
{
    public class Pokemon
    {
        public string name { get; set; }
        public ushort speciesId { get; set; }
        public ushort numberPokedex { get; set; }
        public string type1 { get; set; }
        public string? type2 { get; set; }
    }
}
