namespace DiCor.Net.UpperLayer
{
    public enum ULConnectionState
    {
        Sta1_Idle,
        Sta2_TransportConnectionOpen,
        Sta3_AwaitingLocalAssociateResponse,
        Sta4_AwaitingTransportConnectionOpen,
        Sta5_AwaitingAssociateResponse,
        Sta6_Ready,
        Sta7_AwaitingReleaseResponse,
        Sta8_AwaitingLocalReleaseResponse,
        Sta9_AwaitingLocalReleaseResponseCollisionRequestor,
        Sta10_AwaitingReleaseResponseCollisionAcceptor,
        Sta11_AwaitingReleaseResponseCollisionRequestor,
        Sta12_AwaitingLocalReleaseResponseCollisionAcceptor,
        Sta13_AwaitingTransportConnectionClose,

    }
}
