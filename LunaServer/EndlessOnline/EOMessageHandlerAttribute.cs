using System;

namespace LunaServer.EndlessOnline
{
    using EndlessOnline.Communication;

    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    internal sealed class EOMessageHandlerAttribute : Attribute
    {
        public EOMessageHandlerAttribute(PacketFamily family, PacketAction action, ClientState state)
        {
            this.Family = family;
            this.Action = action;
            this.State = state;
        }

        public PacketFamily Family { get; }
        public PacketAction Action { get; }
        public ClientState State { get; }
    }
}