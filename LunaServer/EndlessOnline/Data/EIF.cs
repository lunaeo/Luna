namespace LunaServer.EndlessOnline.Data
{
    /// <summary>
    /// Endless Item File reader/writer
    /// </summary>
    public class EIF : Pub
    {
        /// <summary>
        /// Type of the item.
        /// Determines it's generic behaviour.
        /// </summary>
        public enum Type : byte
        {
            Static,
            Unknown,
            Money,
            Heal,
            Teleport,
            Spell,
            EXPReward,
            StatReward,
            SkillReward,
            Key,
            Weapon,
            Shield,
            Armor,
            Hat,
            Boots,
            Gloves,
            Charm,
            Belt,
            Necklace,
            Ring,
            Armlet,
            Bracer,
            Beer,
            EffectPotion,
            HairDye,
            CureCurse
        }

        /// <summary>
        /// Sub-type of the item.
        /// Determines type-specific behaviour of the item.
        /// </summary>
        public enum SubType : byte
        {
            None,
            Ranged,
            Arrows,
            Wings
        }

        /// <summary>
        /// Special property the item carries.
        /// Effects the color of the item name in the inventory.
        /// </summary>
        public enum Special : byte
        {
            Normal,
            Rare,
            Unknown,
            Unique,
            Lore,
            Cursed
        }

        /// <summary>
        /// Fixed item size constants
        /// </summary>
        public enum Size : byte
        {
            Size1x1,
            Size1x2,
            Size1x3,
            Size1x4,
            Size2x1,
            Size2x2,
            Size2x3,
            Size2x4
        }

        /// <summary>
        /// Returns the width component of the Size passed
        /// </summary>
        /// <param name="size">Size member of the item</param>
        public static int SizeWidth(Size size)
        {
            return size switch
            {
                Size.Size1x1 => 1,
                Size.Size1x2 => 1,
                Size.Size1x3 => 1,
                Size.Size1x4 => 1,
                Size.Size2x1 => 2,
                Size.Size2x2 => 2,
                Size.Size2x3 => 2,
                Size.Size2x4 => 2,
                _ => 0,
            };
        }

        /// <summary>
        /// Returns the height component of the Size passed
        /// </summary>
        /// <param name="size">Size member of the item</param>
        public static int SizeHeight(Size size)
        {
            return size switch
            {
                Size.Size1x1 => 1,
                Size.Size1x2 => 2,
                Size.Size1x3 => 3,
                Size.Size1x4 => 4,
                Size.Size2x1 => 1,
                Size.Size2x2 => 2,
                Size.Size2x3 => 3,
                Size.Size2x4 => 4,
                _ => 0,
            };
        }

        /// <summary>
        /// Returns the total number of tiles in the inventory taken
        /// </summary>
        /// <param name="size">Size member of the item</param>
        public static int SizeTiles(Size size)
        {
            return size switch
            {
                Size.Size1x1 => 1,
                Size.Size1x2 => 2,
                Size.Size1x3 => 3,
                Size.Size1x4 => 4,
                Size.Size2x1 => 2,
                Size.Size2x2 => 4,
                Size.Size2x3 => 6,
                Size.Size2x4 => 8,
                _ => 0,
            };
        }

        /// <summary>
        /// Returns the magic file type header
        /// </summary>
        public override string FileType { get { return "EIF"; } }

        /// <summary>
        /// Returns a element of a key
        /// </summary>
        /// <param name="key">The key that you want the element of</param>
        /// <returns>Entry of that key</returns>
        public Entry this[ushort key]
        {
            get
            {
                if (data.ContainsKey(key)) return (Entry)data[key];
                else return null;
            }
        }

        /// <summary>
        /// Return a new class-specific data entry
        /// </summary>
        public override IPubEntry EntryFactory()
        {
            return new Entry();
        }

        /// <summary>
        /// Returns the item ID of a certain key
        /// </summary>
        /// <param name="keyNum">Index of key number</param>
        /// <returns>Item ID</returns>
        public ushort GetKey(ushort keyNum)
        {
            ushort keyCount = 0;

            for (ushort i = 0; i < data.Count; ++i)
            {
                if (this[i].type == Type.Key)
                {
                    if (keyCount == keyNum)
                    {
                        return i;
                    }

                    ++keyCount;
                }
            }

            return 0;
        }

        /// <summary>
        /// Creates an empty data set
        /// </summary>
        public EIF() : base() { }

        /// <summary>
        /// Load from a pub file
        /// </summary>
        /// <param name="fileName">File to read the data from</param>
        public EIF(string fileName) : base(fileName) { }

        /// <summary>
        /// Item data entry
        /// </summary>
        public class Entry : IPubEntry
        {
            public string name = string.Empty;
            public ushort graphic;
            public Type type;
            public SubType subtype;
            public Special special;

            public ushort hp;
            public ushort tp;
            public ushort minDamage;
            public ushort maxDamage;
            public ushort accuracy;
            public ushort evade;
            public ushort armor;

            public byte unknown1;

            public byte strength;
            public byte intelligence;
            public byte wisdom;
            public byte agility;
            public byte constitution;
            public byte charisma;

            public byte light;
            public byte dark;
            public byte earth;
            public byte air;
            public byte water;
            public byte fire;

            [ThreeByte] public int special1;
            public byte special2;
            public byte special3;

            public ushort levelRequirement;
            public ushort classRequirement;

            public ushort strRequirement;
            public ushort intRequirement;
            public ushort wisRequirement;
            public ushort agiRequirement;
            public ushort conRequirement;
            public ushort chaRequirement;

            public byte unknown2;
            public byte unknown3;

            public byte weight;

            public byte unknown4;

            public Size size;
        }
    }
}