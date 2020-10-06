using System;
using System.Collections.Generic;
using System.Text;

namespace LunaServer.EndlessOnline.Communication
{
    /// <summary>
    /// Packet family IDs
    /// </summary>
    public enum PacketFamily : byte
    {
        Connection = 1,
        Account = 2,
        Character = 3,
        Login = 4,
        Welcome = 5,
        Walk = 6,
        Face = 7,
        Chair = 8,
        Emote = 9,
        Attack = 11,
        Spell = 12,
        Shop = 13,
        Item = 14,
        StatSkill = 16,
        Global = 17,
        Talk = 18,
        Warp = 19,
        JukeBox = 21,
        Players = 22,
        Avatar = 23,
        Party = 24,
        Refresh = 25,
        NPC = 26,
        AutoRefresh = 27,
        Appear = 29,
        PaperDoll = 30,
        Effect = 31,
        Trade = 32,
        Chest = 33,
        Door = 34,
        Message = 35,
        Bank = 36,
        Locker = 37,
        Barber = 38,
        Guild = 39,
        Sit = 41,
        Recover = 42,
        Board = 43,
        Arena = 45,
        Priest = 46,
        Marriage = 47,
        AdminInteract = 48,
        Citizen = 49,
        Quest = 50,
        Book = 51,

        Init = 255
    }

    /// <summary>
    /// Packet action IDs
    /// </summary>
    public enum PacketAction : byte
    {
        Request = 1,
        Accept = 2,
        Reply = 3,
        Remove = 4,
        Agree = 5,
        Create = 6,
        Add = 7,
        Player = 8,
        Take = 9,
        Use = 10,
        Buy = 11,
        Sell = 12,
        Open = 13,
        Close = 14,
        Message = 15,
        Spec = 16,
        Admin = 17,
        List = 18,
        Tell = 20,
        Report = 21,
        Announce = 22,
        Server = 23,
        Drop = 24,
        Junk = 25,
        Get = 27,
        Exp = 33,

        Ping = 240,
        Pong = 241,
        Net3 = 242,
        Init = 255
    }

    /// <summary>
    /// EO packet reader and writer
    /// </summary>
    public class Packet
    {
        private List<byte> data;

        private int readPos = 2;
        private int writePos = 2;

        /// <summary>
        /// Retrieve the family ID for the packet
        /// </summary>
        public PacketFamily Family
        {
            get { return (PacketFamily)data[1]; }
            set { data[1] = (byte)value; }
        }

        /// <summary>
        /// Retrieve the action ID for the packet
        /// </summary>
        public PacketAction Action
        {
            get { return (PacketAction)data[0]; }
            set { data[0] = (byte)value; }
        }

        /// <summary>
        /// Return the read position.
        /// This is incremented by every read operation.
        /// </summary>
        public int ReadPos
        {
            get { return readPos; }
            set
            {
                if (value < 0 || value > this.Length)
                    throw new IndexOutOfRangeException("Seek out of range of packet");
                readPos = value;
            }
        }

        /// <summary>
        /// Return the write position.
        /// This is incremented by every write operation.
        /// </summary>
        public int WritePos
        {
            get { return writePos; }
            set
            {
                if (value < 0 || value > this.Length)
                    throw new IndexOutOfRangeException("Seek out of range of packet");
                writePos = value;
            }
        }

        /// <summary>
        /// Returns the total length of the packet including IDs
        /// </summary>
        public int Length
        {
            get { return data.Count; }
        }

        /// <summary>
        /// Byte written by AddBreak
        /// </summary>
        public const byte Break = 0xFF;

        /// <summary>
        /// Constants for the maximum number that can be stored in a number of bytes in "EO Format"
        /// </summary>
        public readonly static int[] Max = { 253, 64009, 16194277 };

        /// <summary>
        /// Encodes a number in to "EO Format"
        /// </summary>
        /// <param name="number">The number to be encoded</param>
        /// <param name="size">The size of the bte array returned</param>
        /// <returns>A byte array as large as size containing the encoded number</returns>
        public static byte[] EncodeNumber(int number, int size)
        {
            var b = new byte[size];

            for (var i = 3; i >= 1; --i)
            {
                if (i >= b.Length)
                {
                    if (number >= Max[i - 1])
                        number = number % Max[i - 1];
                }
                else if (number >= Max[i - 1])
                {
                    b[i] = (byte)(number / Max[i - 1] + 1);
                    number = number % Max[i - 1];
                }
                else
                {
                    b[i] = 254;
                }
            }

            b[0] = (byte)(number + 1);

            return b;
        }

        /// <summary>
        /// Decodes an "EO Format" number
        /// </summary>
        /// <param name="b">The byte array to decode</param>
        /// <returns>Returns the decoded number</returns>
        public static int DecodeNumber(byte[] b)
        {
            for (var i = 0; i < b.Length; ++i)
            {
                if (b[i] == 0 || b[i] == 254)
                    b[i] = 0;
                else
                    --b[i];
            }

            var a = 0;

            for (var i = b.Length - 1; i >= 1; --i)
            {
                a += b[i] * Max[i - 1];
            }

            return a + b[0];
        }

        /// <summary>
        /// Creates a blank packet with the specified IDs
        /// </summary>
        public Packet(PacketFamily family, PacketAction action)
        {
            data = new List<byte>(2)
            {
                (byte)action,
                (byte)family
            };
        }

        /// <summary>
        /// Creates a packet from the specified byte array
        /// </summary>
        public Packet(byte[] data)
        {
            this.data = new List<byte>(data);
        }

        /// <summary>
        /// Clears all the data in the packet (ID is preserved)
        /// </summary>
        public void Clear()
        {
            data = new List<byte>(new byte[] { data[0], data[1] });
            readPos = 2;
            writePos = 2;
        }

        /// <summary>
        /// Sets the packet IDs
        /// </summary>
        public void SetID(PacketFamily family, PacketAction action)
        {
            data[0] = (byte)action;
            data[1] = (byte)family;
        }

        /// <summary>
        /// Adds a byte to the data stream (raw data).
        /// Uses 1 byte.
        /// </summary>
        public void AddByte(byte b) { data.Insert(writePos, b); writePos += 1; }

        internal void AddInt(object usage)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds a "break" byte to the stream.
        /// Uses 1 byte.
        /// </summary>
        /// <see>Break</see>
        public void AddBreak()
        {
            this.AddByte(Break);
        }

        /// <summary>
        /// Adds a byte to the stream in "EO format".
        /// Uses 1 byte.
        /// </summary>
        public void AddChar(byte c)
        {
            data.InsertRange(writePos, EncodeNumber((int)c, 1));
            writePos += 1;
        }

        /// <summary>
        /// Adds a short to the stream in "EO format".
        /// Uses 2 bytes.
        /// </summary>
        public void AddShort(ushort s)
        {
            data.InsertRange(writePos, EncodeNumber((int)s, 2));
            writePos += 2;
        }

        /// <summary>
        /// Adds an integer to the stream in "EO format".
        /// Uses 3 bytes.
        /// </summary>
        public void AddThree(int t)
        {
            data.InsertRange(writePos, EncodeNumber(t, 3));
            writePos += 3;
        }

        /// <summary>
        /// Adds an integer to the stream in "EO format".
        /// Uses 4 bytes.
        /// </summary>
        public void AddInt(int i)
        {
            data.InsertRange(writePos, EncodeNumber(i, 4));
            writePos += 4;
        }

        /// <summary>
        /// Adds an array of bytes to the stream
        /// </summary>
        public void AddBytes(byte[] b)
        {
            data.InsertRange(writePos, b);
            writePos += b.Length;
        }

        /// <summary>
        /// Adds a range of bytes to the stream
        /// </summary>
        public void AddBytes(byte[] b, int offset, int count)
        {
            var b2 = new byte[count];
            Array.Copy(b, offset, b2, 0, count);
            data.InsertRange(writePos, b2);
            writePos += count;
        }

        /// <summary>
        /// Adds a string encoded as ASCII to the stream
        /// </summary>
        public void AddString(string s)
        {
            var b = ASCIIEncoding.ASCII.GetBytes(s);

            this.AddBytes(b);
        }

        /// <summary>
        /// Adds a string encoded as ASCII to the stream then adds a Break character
        /// </summary>
        public void AddBreakString(string s)
        {
            var b = ASCIIEncoding.ASCII.GetBytes(s);

            for (var i = 0; i < b.Length; ++i)
            {
                if (b[i] == Break)
                    b[i] = (byte)'y';
            }

            this.AddBytes(b);
            this.AddBreak();
        }

        /// <summary>
        /// Reads a byte from the data stream (raw data).
        /// Reads 1 byte
        /// </summary>
        public byte PeekByte() { return data[readPos]; }

        /// <summary>
        /// Reads an "EO format" byte from the stream.
        /// Reads 1 byte.
        /// </summary>
        public byte PeekChar() { return (byte)DecodeNumber(data.GetRange(readPos, 1).ToArray()); }

        /// <summary>
        /// Reads an "EO format" short from the stream.
        /// Reads 2 bytes.
        /// </summary>
        public ushort PeekShort() { return (ushort)DecodeNumber(data.GetRange(readPos, 2).ToArray()); }

        /// <summary>
        /// Reads an "EO format" integer from the stream.
        /// Reads 3 bytes.
        /// </summary>
        public int PeekThree() { return DecodeNumber(data.GetRange(readPos, 3).ToArray()); }

        /// <summary>
        /// Reads an "EO format" integer from the stream.
        /// Reads 4 bytes.
        /// </summary>
        public int PeekInt() { return DecodeNumber(data.GetRange(readPos, 4).ToArray()); }

        /// <summary>
        /// Reads a fixed amount of bytes from the stream
        /// </summary>
        /// <param name="length">Number of bytes to read</param>
        public byte[] PeekBytes(int length) { return data.GetRange(readPos, length).ToArray(); }

        /// <summary>
        /// Reads a fixed amount of bytes from the stream and returns it as a string
        /// </summary>
        /// <param name="length">Number of bytes to read</param>
        public string PeekFixedString(int length) { return ASCIIEncoding.ASCII.GetString(this.PeekBytes(length)); }

        /// <summary>
        /// Reads all of the data from the current read position to the end of the string
        /// </summary>
        public string PeekEndString() { return this.PeekFixedString(this.Length - readPos); }

        /// <summary>
        /// Reads a string up until a Break character or the end of the string
        /// </summary>
        /// <see>Break</see>
        public string PeekBreakString()
        {
            var breakpos = readPos;

            while (breakpos < this.Length && data[breakpos] != Break)
                ++breakpos;

            return this.PeekFixedString(breakpos - readPos);
        }

        /// <summary>
        /// Reads a byte from the data stream (raw data).
        /// Reads 1 byte
        /// </summary>
        public byte GetByte()
        {
            var temp = this.PeekByte();
            readPos += 1;
            return temp;
        }

        /// <summary>
        /// Reads an "EO format" byte from the stream.
        /// Reads 1 byte.
        /// </summary>
        public byte GetChar()
        {
            var temp = this.PeekChar();
            readPos += 1;
            return temp;
        }

        /// <summary>
        /// Reads an "EO format" short from the stream.
        /// Reads 2 bytes.
        /// </summary>
        public ushort GetShort()
        {
            var temp = this.PeekShort();
            readPos += 2;
            return temp;
        }

        /// <summary>
        /// Reads an "EO format" integer from the stream.
        /// Reads 3 bytes.
        /// </summary>
        public int GetThree()
        {
            var temp = this.PeekThree();
            readPos += 3;
            return temp;
        }

        /// <summary>
        /// Reads an "EO format" integer from the stream.
        /// Reads 4 bytes.
        /// </summary>
        public int GetInt()
        {
            var temp = this.PeekInt();
            readPos += 4;
            return temp;
        }

        /// <summary>
        /// Reads a fixed amount of bytes from the stream
        /// </summary>
        /// <param name="length">Number of bytes to read</param>
        public byte[] GetBytes(int length)
        {
            var b = this.PeekBytes(length);
            readPos += length;
            return b;
        }

        /// <summary>
        /// Reads a fixed amount of bytes from the stream and returns it as a string
        /// </summary>
        /// <param name="length">Number of bytes to read</param>
        public string GetFixedString(int length)
        {
            return ASCIIEncoding.ASCII.GetString(this.GetBytes(length));
        }

        /// <summary>
        /// Reads all of the data from the current read position to the end of the string
        /// </summary>
        public string GetEndString()
        {
            return this.GetFixedString(this.Length - readPos);
        }

        /// <summary>
        /// Reads a string up until a Break character or the end of the string
        /// </summary>
        /// <see>Break</see>
        public string GetBreakString()
        {
            var temp = this.PeekBreakString();
            readPos += temp.Length + 1;
            return temp;
        }

        /// <summary>
        /// Skips a fixed number of bytes without returning any data
        /// </summary>
        /// <param name="bytes">Number of bytes to skip</param>
        public void Skip(int bytes)
        {
            readPos += bytes;
        }

        /// <summary>
        /// Returns the whole packet as a byte array
        /// </summary>
        public byte[] Get()
        {
            return data.ToArray();
        }
    }
}