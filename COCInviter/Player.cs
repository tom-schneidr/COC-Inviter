using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COCInviter
{
    [Serializable]
    public class Player
    {
      public string Tag { get; set; }
      public int? Townhall { get; set; }
      public int? Level { get; set; }
      public int? Queen { get; set; }
      public int? King { get; set; }
      public int? Warden { get; set; }
      public int? Champion { get; set; }
      public int? Attacks { get; set; }
      public int? Trophies { get; set; }
      public int? Donations { get; set; }
  }
}
