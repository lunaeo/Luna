namespace LunaServer.EndlessOnline.Domain.Character
{
    public class InventoryItem : IInventoryItem
    {
        public short ItemID { get; }

        public int Amount { get; }

        public InventoryItem(short itemID, int amount)
        {
            this.ItemID = itemID;
            this.Amount = amount;
        }

        public IInventoryItem WithAmount(int newAmount)
        {
            return new InventoryItem(this.ItemID, newAmount);
        }
    }

    public interface IInventoryItem
    {
        short ItemID { get; }

        int Amount { get; }

        IInventoryItem WithAmount(int newAmount);
    }
}