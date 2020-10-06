using System;
using System.Collections.Generic;
using System.IO;

namespace LunaServer.EndlessOnline.Data
{
    /// <summary>
    /// Pub file data entry
    /// </summary>
    public interface IPubEntry { }

    /// <summary>
    /// Base class for "pub" files
    /// </summary>
    public abstract class Pub
    {
        /// <summary>
        /// Marks a field as a three byte integer
        /// </summary>
        [AttributeUsage(AttributeTargets.Field)]
        protected sealed class ThreeByteAttribute : Attribute { }

        /// <summary>
        /// Returns the magic file type header
        /// </summary>
        public abstract string FileType { get; }

        /// <summary>
        /// Dictionary that contains all of the pub entries
        /// </summary>
        internal Dictionary<ushort, IPubEntry> data;

        /// <summary>
        /// Amount of entries inside data
        /// </summary>
        public int Count { get { return data.Count; } }

        /// <summary>
        /// Adds an element with the provided key and value to the IDictionary<TKey, TValue>
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add</param>
        /// <param name="value">The object to use as the value of the element to add</param>
        public void Add(ushort key, IPubEntry value)
        {
            data.Add(key, value);
        }

        /// <summary>
        /// A unique number for the version of the file.
        /// This is set after Write() is called for new files.
        /// </summary>
        /// <remarks>
        /// EOHax defacto standard is to set this to the CRC32 of the entire file output with this field to 0x00000000
        /// 0x00 bytes are represented in 0x01 to avoid problems with file transfer
        /// </remarks>
        public byte[] RevisionId { get; private set; }

        /// <summary>
        /// Return a new class-specific data entry
        /// </summary>
        public abstract IPubEntry EntryFactory();

        /// <summary>
        /// Creates a blank pub file
        /// </summary>
        public Pub()
        {
            data = new Dictionary<ushort, IPubEntry>();
        }

        /// <summary>
        /// Load from a pub file
        /// </summary>
        /// <param name="fileName">File to read the data from</param>
        public Pub(string fileName)
        {
            data = new Dictionary<ushort, IPubEntry>();
            this.Read(fileName);
        }

        /// <summary>
        /// Read a pub file
        /// </summary>
        /// <param name="fileName">File to read the data from</param>
        public void Read(string fileName)
        {
            using (var file = new EOFile(File.Open(fileName, FileMode.Open, FileAccess.Read)))
            {
                if (file.GetFixedString(3) != this.FileType)
                    throw new Exception("Corrupt or not a " + this.FileType + " file");

                var revisionId = (uint)file.GetInt();

                this.RevisionId = new byte[4] {
                    (byte)((revisionId & 0xFF000000) >> 24),
                    (byte)((revisionId & 0xFF0000) >> 16),
                    (byte)((revisionId & 0xFF00) >> 8),
                    (byte)(revisionId & 0xFF)
                };

                var count = file.GetShort();
                file.Skip(1); // TODO: What is this?

                this.Add(0, this.EntryFactory());
                for (ushort i = 1; i <= count; ++i)
                {
                    var entry = this.EntryFactory();
                    this.GetEntry(file, ref entry);
                    this.Add(i, entry);
                }
            }
        }

        /// <summary>
        /// Writes the full pub file to disk
        /// </summary>
        /// <param name="fileName">Name of the file to write to</param>
        public void Write(string fileName)
        {
            using (var file = new EOFile(File.Open(fileName, FileMode.Create, FileAccess.Write)))
            {
                file.AddString(this.FileType);
                file.AddInt(0);
                file.AddShort((ushort)this.Count);
                file.AddChar(0); // TODO: What is this?

                foreach (var entry in data)
                {
                    this.AddEntry(file, entry.Value);
                }

                this.RevisionId = file.WriteHash(3, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Loads a single entry from the pub file in to an IPubEntry object
        /// </summary>
        /// <param name="file">EOFile object to read</param>
        /// <param name="entry">Entry object to read in to</param>
        private void GetEntry(EOFile file, ref IPubEntry entry)
        {
            var stringLengths = new List<byte>();
            var i = 0;

            var fields = entry.GetType().GetFields();

            foreach (var member in fields)
            {
                if (member.FieldType == typeof(string))
                    stringLengths.Add(file.GetChar());
            }

            foreach (var member in fields)
            {
                var memberType = member.FieldType;

                if (memberType.IsEnum)
                    memberType = Enum.GetUnderlyingType(memberType);

                if (memberType == typeof(char))
                {
                    member.SetValue(entry, (char)file.GetChar());
                }
                else if (memberType == typeof(byte))
                {
                    member.SetValue(entry, file.GetChar());
                }
                else if (memberType == typeof(short))
                {
                    member.SetValue(entry, (short)file.GetShort());
                }
                else if (memberType == typeof(ushort))
                {
                    member.SetValue(entry, file.GetShort());
                }
                else if (memberType == typeof(int))
                {
                    if (member.GetCustomAttributes(typeof(ThreeByteAttribute), false).Length == 0)
                        member.SetValue(entry, file.GetInt());
                    else
                        member.SetValue(entry, file.GetThree());
                }
                else if (memberType == typeof(uint))
                {
                    if (member.GetCustomAttributes(typeof(ThreeByteAttribute), false).Length == 0)
                        member.SetValue(entry, (uint)file.GetInt());
                    else
                        member.SetValue(entry, (uint)file.GetThree());
                }
                else if (memberType == typeof(string))
                {
                    member.SetValue(entry, file.GetFixedString(stringLengths[i++]));
                }
                else
                {
                    throw new Exception("Cannot represent " + memberType + " in pub file");
                }
            }
        }

        /// <summary>
        /// Adds an entry to the pub file
        /// </summary>
        /// <param name="file">EOFile object to write to</param>
        /// <param name="entry">Entry to append to the file</param>
        private void AddEntry(EOFile file, IPubEntry entry)
        {
            var fields = entry.GetType().GetFields();

            foreach (var member in fields)
            {
                if (member.FieldType == typeof(string))
                    file.AddChar((byte)((string)member.GetValue(entry)).Length);
            }

            foreach (var member in fields)
            {
                var memberType = member.FieldType;

                if (memberType.IsEnum)
                    memberType = Enum.GetUnderlyingType(memberType.GetType());

                if (memberType == typeof(char))
                {
                    file.AddChar((byte)(char)member.GetValue(entry));
                }
                else if (memberType == typeof(byte))
                {
                    file.AddChar((byte)member.GetValue(entry));
                }
                else if (memberType == typeof(short))
                {
                    file.AddShort((ushort)(short)member.GetValue(entry));
                }
                else if (memberType == typeof(ushort))
                {
                    file.AddShort((ushort)member.GetValue(entry));
                }
                else if (memberType == typeof(int))
                {
                    if (member.GetCustomAttributes(typeof(ThreeByteAttribute), false).Length == 0)
                        file.AddInt((int)member.GetValue(entry));
                    else
                        file.AddThree((int)member.GetValue(entry));
                }
                else if (memberType == typeof(uint))
                {
                    if (member.GetCustomAttributes(typeof(ThreeByteAttribute), false).Length == 0)
                        file.AddInt((int)(uint)member.GetValue(entry));
                    else
                        file.AddThree((int)(uint)member.GetValue(entry));
                }
                else if (memberType == typeof(string))
                {
                    file.AddString((string)member.GetValue(entry));
                }
                else
                {
                    throw new Exception("Cannot represent " + memberType.Name + " in pub file");
                }
            }
        }
    }
}