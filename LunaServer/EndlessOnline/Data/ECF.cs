namespace LunaServer.EndlessOnline.Data
{
    /// <summary>
    /// Endless Class File reader/writer
    /// </summary>
    public class ECF : Pub
    {
        /// <summary>
        /// Type of the class.
        /// Determines the stat calculations used.
        /// </summary>
        public enum Type : byte
        {
            Melee,
            Rogue,
            Magician,
            Archer,
            Peasant
        }

        /// <summary>
        /// Returns the magic file type header
        /// </summary>
        public override string FileType { get { return "ECF"; } }

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
        /// Creates an empty data set
        /// </summary>
        public ECF() : base() { }

        /// <summary>
        /// Load from a pub file
        /// </summary>
        /// <param name="fileName">File to read the data from</param>
        public ECF(string fileName) : base(fileName) { }

        /// <summary>
        /// Class data entry
        /// </summary>
        public class Entry : IPubEntry
        {
            public string name = string.Empty;

            public byte baseClass;
            public Type type;

            public ushort strength;
            public ushort intelligence;
            public ushort wisdom;
            public ushort agility;
            public ushort constitution;
            public ushort charisma;
        }
    }
}