using System;

namespace LunaServer.EndlessOnline.Domain.Map
{
    public class Item : IItem
    {
        public ushort UniqueID { get; }

        public short ItemID { get; }

        public byte X { get; }

        public byte Y { get; }

        public int Amount { get; private set; }

        public bool IsNPCDrop { get; private set; }

        public int OwningPlayerID { get; private set; }

        public DateTime DropTime { get; private set; }

        public Item(ushort uid, short itemID, byte x, byte y)
        {
            this.UniqueID = uid;
            this.ItemID = itemID;
            this.X = x;
            this.Y = y;
        }

        public IItem WithAmount(int newAmount)
        {
            var newItem = MakeCopy(this);
            newItem.Amount = newAmount;
            return newItem;
        }

        public IItem WithIsNPCDrop(bool isNPCDrop)
        {
            var newItem = MakeCopy(this);
            newItem.IsNPCDrop = isNPCDrop;
            return newItem;
        }

        public IItem WithOwningPlayerID(int owningPlayerID)
        {
            var newItem = MakeCopy(this);
            newItem.OwningPlayerID = owningPlayerID;
            return newItem;
        }

        public IItem WithDropTime(DateTime dropTime)
        {
            var newItem = MakeCopy(this);
            newItem.DropTime = dropTime;
            return newItem;
        }

        private static Item MakeCopy(IItem input)
        {
            return new Item(input.UniqueID, input.ItemID, input.X, input.Y)
            {
                Amount = input.Amount,
                IsNPCDrop = input.IsNPCDrop,
                OwningPlayerID = input.OwningPlayerID,
                DropTime = input.DropTime
            };
        }
    }

    public interface IItem
    {
        ushort UniqueID { get; }

        short ItemID { get; }

        byte X { get; }

        byte Y { get; }

        int Amount { get; }

        bool IsNPCDrop { get; }

        int OwningPlayerID { get; }

        DateTime DropTime { get; }

        IItem WithAmount(int newAmount);

        IItem WithIsNPCDrop(bool isNPCDrop);

        IItem WithOwningPlayerID(int owningPlayerID);

        IItem WithDropTime(DateTime dropTime);
    }
}