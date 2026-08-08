using System;
using System.Collections.Generic;
using System.Text;

namespace PokeTracker.Models
{
    public class Pokemon
    {
        public string nickname {  get; set; }
        public string name { get; set; }
        public ushort speciesId { get; set; }
        public ushort numberPokedex { get; set; }
        public string type1 { get; set; }
        public string? type2 { get; set; }
        public ushort hp { get; set; }
        public ushort maxHp { get; set; }
        public ushort atq { get; set; }
        public ushort spAtq { get; set; }
        public ushort def {  get; set; }
        public ushort spDef { get; set; }
        public ushort spe { get; set; }
        public string? ImagePath { get; set; }
        public byte Level { get; set; }
        public string ability { get; set; }
        public string? ab1 { get; set; }
        public string? ab2 { get; set; }
        public string nature { get; set; }
        public byte evHp { get; set; }
        public byte evAtq { get; set; }
        public byte evDef { get; set; }
        public byte evSpAtq { get; set; }
        public byte evSpDef { get; set; }
        public byte evSpe { get; set; }
        public byte ivHp { get; set; }
        public byte ivAtq { get; set; }
        public byte ivDef { get; set; }
        public byte ivSpAtq { get; set; }
        public byte ivSpDef { get; set; }
        public byte ivSpe { get; set; }
    }
}
