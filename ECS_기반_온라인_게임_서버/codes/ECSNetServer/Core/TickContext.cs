using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECSNetServer.Core;



public class TickContext
{
    public float DeltaTime { get; set; }
    public long CurrentTick { get; set; }
    public DateTime CurrentTime { get; set; }
}
