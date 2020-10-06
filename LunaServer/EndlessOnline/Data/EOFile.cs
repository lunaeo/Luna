using System;
using System.IO;
using System.Text;

namespace LunaServer.EndlessOnline.Data
{
    /// <summary>
    /// Handles conversions on file IO
    /// </summary>
    public class EOFile : IDisposable
    {
        /// <summary>
        /// Size of file upon opening it
        /// </summary>
        public long FileSize { get; private set; }

        /// <summary>
        /// Raw FileStream object
        /// </summary>
        private FileStream stream;

        /// <summary>
        /// Keeps track of the CRC32 hash of the written file
        /// </summary>
        private CRC32 hasher;

        private void WriteWrapper(byte[] array, int offset, int count)
        {
            stream.Write(array, offset, count);
            hasher.TransformBlock(array, offset, count, null, 0);
        }

        /// <summary>
        /// Value written by AddBreak
        /// </summary>
        public const byte Break = 0xFF;

        /// <summary>
        /// Constants for the maximum number that can be stored in a number of bytes in "EO Format"
        /// </summary>
        public static int[] Max = { 253, 64009, 16194277 };

        /// <summary>
        /// Encodes a number in to "EO Format"
        /// </summary>
        /// <param name="number">The number to be encoded</param>
        /// <param name="size">The size of the bte array returned</param>
        /// <returns>A byte array as large as size containing the encoded number</returns>
        public static byte[] EncodeNumber(int number, int size)
        {
            var b = new byte[size];
            var unumber = (uint)number;

            for (var i = 3; i >= 1; --i)
            {
                if (i >= b.Length)
                {
                    if (unumber >= Max[i - 1])
                        unumber = unumber % (uint)Max[i - 1];
                }
                else if (number >= Max[i - 1])
                {
                    b[i] = (byte)(unumber / Max[i - 1] + 1);
                    unumber = unumber % (uint)Max[i - 1];
                }
                else
                {
                    b[i] = 254;
                }
            }

            b[0] = (byte)(unumber + 1);

            return b;
        }

        public long GetPos()
        {
            return stream.Position;
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
        /// Creates a wrapper around a FileStream object
        /// </summary>
        /// <param name="stream">FileStream object to wrap</param>
        public EOFile(FileStream stream)
        {
            stream.Seek(0, SeekOrigin.End);
            this.FileSize = stream.Length;
            stream.Seek(0, SeekOrigin.Begin);

            this.stream = stream;
            hasher = new CRC32();
        }

        /// <summary>
        /// Adds a byte to the data stream (raw data).
        /// Uses 1 byte.
        /// </summary>
        public void AddByte(byte b) { this.WriteWrapper(new byte[] { b }, 0, 1); }

        /// <summary>
        /// Adds a "break" byte to the stream.
        /// Uses 1 byte.
        /// </summary>
        /// <see>Break</see>
        public void AddBreak() { this.AddByte(Break); }

        /// <summary>
        /// Adds a byte to the stream in "EO format".
        /// Uses 1 byte.
        /// </summary>
        public void AddChar(byte c) { this.WriteWrapper(EncodeNumber((int)c, 1), 0, 1); }

        /// <summary>
        /// Adds a short to the stream in "EO format".
        /// Uses 2 bytes.
        /// </summary>
        public void AddShort(ushort s) { this.WriteWrapper(EncodeNumber((int)s, 2), 0, 2); }

        /// <summary>
        /// Adds an integer to the stream in "EO format".
        /// Uses 3 bytes.
        /// </summary>
        public void AddThree(int t) { this.WriteWrapper(EncodeNumber((int)t, 3), 0, 3); }

        /// <summary>
        /// Adds an integer to the stream in "EO format".
        /// Uses 4 bytes.
        /// </summary>
        public void AddInt(int i) { this.WriteWrapper(EncodeNumber(i, 4), 0, 4); }

        /// <summary>
        /// Adds an array of bytes to the stream
        /// </summary>
        public void AddBytes(byte[] b) { this.WriteWrapper(b, 0, b.Length); }

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
        public byte GetByte() { return (byte)stream.ReadByte(); }

        /// <summary>
        /// Reads an "EO format" byte from the stream.
        /// Reads 1 byte.
        /// </summary>
        public byte GetChar() { return (byte)DecodeNumber(this.GetBytes(1)); }

        /// <summary>
        /// Reads an "EO format" short from the stream.
        /// Reads 2 bytes.
        /// </summary>
        public ushort GetShort() { return (ushort)DecodeNumber(this.GetBytes(2)); }

        /// <summary>
        /// Reads an "EO format" integer from the stream.
        /// Reads 3 bytes.
        /// </summary>
        public int GetThree() { return DecodeNumber(this.GetBytes(3)); }

        /// <summary>
        /// Reads an "EO format" integer from the stream.
        /// Reads 4 bytes.
        /// </summary>
        public int GetInt() { return DecodeNumber(this.GetBytes(4)); }

        /// <summary>
        /// Reads a fixed amount of bytes from the stream
        /// </summary>
        /// <param name="length">Number of bytes to read</param>
        public byte[] GetBytes(int length)
        {
            var b = new byte[length];
            stream.Read(b, 0, length);
            return b;
        }

        /// <summary>
        /// Reads a fixed amount of bytes from the stream and returns it as a string
        /// </summary>
        /// <param name="length">Number of bytes to read</param>
        public string GetFixedString(int length) { return ASCIIEncoding.ASCII.GetString(this.GetBytes(length)); }

        /// <summary>
        /// Skips a fixed number of bytes without returning any data
        /// </summary>
        /// <param name="bytes">Number of bytes to skip</param>
        public void Skip(int bytes)
        {
            stream.Seek(bytes, SeekOrigin.Current);
        }

        /// <summary>
		/// Writes the CRC32 hash of the file to the specified location in the file
		/// </summary>
		public byte[] WriteHash(long offset, SeekOrigin origin)
        {
            hasher.TransformFinalBlock(new byte[0], 0, 0);
            var hashBytes = hasher.Hash;
            var pos = stream.Position;

            for (var i = 0; i < 4; ++i)
            {
                hashBytes[i] = (byte)(hashBytes[i] | 0x01);
            }

            stream.Seek(offset, origin);
            stream.Write(hashBytes, 0, 4);
            stream.Seek(pos, SeekOrigin.Begin);

            return hashBytes;
        }

        /// <summary>
        /// Closes the file
        /// </summary>
        public void Dispose()
        {
            hasher.Dispose();
            stream.Close();
        }
    }
}