namespace DiCor.Net.UpperLayer
{

    public unsafe struct ULMessage
    {
        public Pdu.Type Type { get; }
        public byte B1 { get; set; }
        public byte B2 { get; set; }

        public ULMessage(Pdu.Type type, byte b1 = 0, byte b2 = 0)
        {
            Type = type;
            B1 = b1;
            B2 = b2;
        }
    }
}
