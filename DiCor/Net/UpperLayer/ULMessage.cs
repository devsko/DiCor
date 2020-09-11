namespace DiCor.Net.UpperLayer
{
    public struct ULMessage
    {
        public Pdu.Type Type { get; }
        public byte B1 { get; set; }
        public byte B2 { get; set; }
        public byte B3 { get; set; }

        public ULMessage(Pdu.Type type, byte b1 = 0, byte b2 = 0, byte b3 = 0)
        {
            Type = type;
            B1 = b1;
            B2 = b2;
            B3 = b3;
        }
    }
}
