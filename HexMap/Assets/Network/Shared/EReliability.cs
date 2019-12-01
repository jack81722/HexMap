namespace Network.Shared
{
    public enum EReliability
    {
        ReliableOrdered = 3,
        ReliableSequenced = 4,
        ReliableUnordered = 1,
        Unreliable = 0,
        Sequenced = 2,
    }
}