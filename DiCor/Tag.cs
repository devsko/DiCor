namespace DiCor
{
    public record struct Tag
    {
        public ushort Group { get; private init; }

        public ushort Element { get; private init; }

        public Tag(ushort group, ushort element)
        {
            Group = group;
            Element = element;
        }
    }
}
