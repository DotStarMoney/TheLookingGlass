namespace TheLookingGlass.StageGraph
{
    public class EmbedToken
    {
        internal EmbedToken(in int lookupId)
        {
            LookupId = lookupId;
        }

        internal int LookupId { get; }
    }
}
