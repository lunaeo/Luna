namespace LunaServer
{
    using EndlessOnline.Domain.Map;

    public class MapItem : Item
    {
        public MapItem(ushort uid, short itemID, byte x, byte y) : base(uid, itemID, x, y)
        {
        }
    }
}