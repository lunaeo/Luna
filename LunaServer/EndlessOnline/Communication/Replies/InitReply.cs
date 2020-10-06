namespace LunaServer.EndlessOnline.Replies
{
    public enum InitReply : byte
    {
        OutOfDate = 1,
        OK = 2,
        Banned = 3,
        FileMap = 4,
        FileEIF = 5,
        FileENF = 6,
        FileESF = 7,
        Players = 8,
        MapMutation = 9,
        FriendListPlayers = 10,
        FileECF = 11
    }
}