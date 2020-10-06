namespace LunaServer.EndlessOnline.Domain.Character
{
    public class InventorySpell : IInventorySpell
    {
        public short ID { get; }

        public short Level { get; }

        public InventorySpell(short id, short level)
        {
            this.ID = id;
            this.Level = level;
        }

        public IInventorySpell WithLevel(short newLevel)
        {
            return new InventorySpell(this.ID, newLevel);
        }
    }

    public interface IInventorySpell
    {
        short ID { get; }

        short Level { get; }

        IInventorySpell WithLevel(short newLevel);
    }
}