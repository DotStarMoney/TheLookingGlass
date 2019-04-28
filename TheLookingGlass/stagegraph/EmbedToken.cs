namespace TheLookingGlass.StageGraph
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
