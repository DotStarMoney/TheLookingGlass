using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace experimental.StageGraph
{
    public class EmbedToken
    {
        internal int LookupId { get; }

        internal EmbedToken(in int lookupId)
        {
            this.LookupId = lookupId;
        }
    }
}
