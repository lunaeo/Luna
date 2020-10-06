using System;

namespace LunaServer.EndlessOnline.Communication
{
    /// <summary>
    /// Packet processor for the client side
    /// </summary>
    public class ClientPacketProcessor : PacketProcessor
    {
        public byte SequenceStart { get; set; }
        public byte SequenceValue { get; set; }

        public byte GetSequence()
        {
            var val = this.SequenceValue;

            if (++this.SequenceValue == 10)
                this.SequenceValue = 0;

            return (byte)(this.SequenceStart + val);
        }

        public void AddSequenceByte(ref byte[] original)
        {
            var newPacket = new byte[original.Length + 1];
            Array.Copy(original, 0, newPacket, 0, 2);
            newPacket[2] = Packet.EncodeNumber(this.GetSequence(), 1)[2];
            Array.Copy(original, 2, newPacket, 3, original.Length - 2);
            original = newPacket;
        }

        public override void Encode(ref byte[] original)
        {
            if (this.SendMulti == 0 || original[1] == (byte)PacketFamily.Init)
                return;

            this.AddSequenceByte(ref original);
            SwapMultiples(ref original, this.SendMulti);
            Interleave(ref original);
            FlipMSB(ref original);
        }

        public override void Decode(ref byte[] original)
        {
            if (this.RecvMulti == 0 || original[1] == (byte)PacketFamily.Init)
                return;

            FlipMSB(ref original);
            Deinterleave(ref original);
            SwapMultiples(ref original, this.RecvMulti);
        }
    }
}