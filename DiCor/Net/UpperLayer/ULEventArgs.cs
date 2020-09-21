namespace DiCor.Net.UpperLayer
{
    public class AAssociateArgs
    {
    }

    public class AAbortArgs
    {
        Pdu.AbortSource Source { get; }
    }

    public class APAbortArgs
    {
        Pdu.AbortReason Reason { get; }
    }
}
