namespace DiCor.IO
{
    public readonly partial struct CharacterEncoding
    {
        private readonly AsciiString _name;

        public CharacterEncoding(AsciiString name)
        {
            // TODO validate CS
            _name = name;
        }

    }
}
