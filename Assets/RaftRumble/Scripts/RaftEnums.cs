namespace RaftRumble
{
    public enum RaftGameMode
    {
        PracticeSinglePlayer,
        Multiplayer
    }

    public enum RaftGameState
    {
        WaitingToStart,
        Playing,
        GameOver,
        Victory
    }
}
