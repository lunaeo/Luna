namespace LunaServer.EndlessOnline.Replies
{
    public enum AccountReply : byte
    {
        Exists = 1,
        NotApproved = 2,
        Created = 3,
        ChangeFailed = 5,
        Changed = 6,
        Continue = 64
    }
}