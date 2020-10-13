using System;
using System.Collections.Generic;
using System.Text;

namespace LunaServer.Addons
{
    /// <summary>
    /// Represents a message sent between client and server.
    ///
    /// <para> A message consists of a string type, and a payload of zero or more typed parameters. </para>
    /// </summary>
    public class AddonMessage : IEnumerable<object>
    {
        /// <summary>
        /// The type of the message
        /// </summary>
        public string Type { get; internal set; }

        /// <summary>
        /// The object values within the message, excluding the type
        /// </summary>
        public List<object> Values { get; internal set; } = new List<object>();

        /// <summary>
        /// The number of entries within the message, excluding the type
        /// </summary>
        public int Count => this.Values.Count;

        public AddonMessage(string type)
        {
            if (string.IsNullOrEmpty(type))
                throw new ArgumentNullException(nameof(type));

            this.Type = type;
        }

        public AddonMessage(string type, params object[] parameters)
        {
            if (string.IsNullOrEmpty(type))
                throw new ArgumentNullException(nameof(type));

            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            this.Type = type;
            this.Add(parameters);
        }

        public static AddonMessage Create(string type, params object[] parameters)
        {
            if (string.IsNullOrEmpty(type))
                throw new ArgumentNullException("You must specify a type for the PlayerIO message.");

            return new AddonMessage(type, parameters);
        }

        public IEnumerator<object> GetEnumerator() => this.Values.GetEnumerator();

        public override string ToString()
        {
            var sb = new StringBuilder("");

            sb.AppendLine(string.Concat("  msg.Type= ", this.Type, ", ", this.Values.Count, " entries"));

            for (var i = 0; i < this.Values.Count; i++)
                sb.AppendLine(string.Concat("  msg[", i, "] = ", this.Values[i], "  (", this.Values[i].GetType().Name, ")"));

            return sb.ToString();
        }

        /// <summary> Retrieve the object stored in the message at the given index. </summary>
        public object this[uint index] => this.Values[(int)index];

        /// <summary> Retrieve an object at the given index and return it as type T </summary> <typeparam name="T">The type to cast to and return</typeparam> <param name="index"> The index to find the entry in </param>
        public T Get<T>(uint index) => (T)Convert.ChangeType(this[index], typeof(T));

        /// <summary> Retrieve the string at the given index </summary> <param name="index"> The index to find the entry in </param>
        public string GetString(uint index) => (string)this[index];

        /// <summary> Retrieve the byte[] at the given index </summary> <param name="index"> The index to find the entry in </param>
        public byte[] GetByteArray(uint index) => (byte[])this[index];

        /// <summary> Retrieve the bool at the given index </summary> <param name="index"> The index to find the entry in </param>
        public bool GetBoolean(uint index) => (bool)this[index];

        /// <summary> Retrieve the double at the given index </summary> <param name="index"> The index to find the entry in </param>
        public double GetDouble(uint index) => (double)this[index];

        /// <summary> Retrieve the float at the given index </summary> <param name="index"> The index to find the entry in </param>
        public float GetFloat(uint index) => (float)this[index];

        /// <summary> Retrieve the int at the given index </summary> <param name="index"> The index to find the entry in </param>
        public int GetInteger(uint index) => (int)this[index];

        /// <summary> Retrieve the int at the given index </summary> <param name="index"> The index to find the entry in </param>
        public int GetInt(uint index) => (int)this[index];

        /// <summary> Retrieve the uint at the given index </summary> <param name="index"> The index to find the entry in </param>
        public uint GetUInt(uint index) => (uint)this[index];

        /// <summary> Retrieve the uint at the given index </summary> <param name="index"> The index to find the entry in </param>
        public uint GetUnsignedInteger(uint index) => (uint)this[index];

        /// <summary> Retrieve the long at the given index </summary> <param name="index"> The index to find the entry in </param>
        public long GetLong(uint index) => (long)this[index];

        /// <summary> Retrieve the ulong at the given index </summary> <param name="index"> The index to find the entry in </param>
        public ulong GetULong(uint index) => (ulong)this[index];

        /// <summary> Retrieve the ulong at the given index </summary> <param name="index"> The index to find the entry in </param>
        public ulong GetUnsignedLong(uint index) => (ulong)this[index];

        /// <summary> Add a string to the message payload </summary><param name="value"> The value to add </param>
        public void Add(string value) => _add(value);

        /// <summary> Add a string to the message payload </summary><param name="value"> The value to add </param>
        public void Add(int value) => _add(value);

        /// <summary> Add a uint to the message payload </summary><param name="value"> The value to add </param>
        public void Add(uint value) => _add(value);

        /// <summary> Add a long to the message payload </summary><param name="value"> The value to add </param>
        public void Add(long value) => _add(value);

        /// <summary> Add a ulong to the message payload </summary><param name="value"> The value to add </param>
        public void Add(ulong value) => _add(value);

        /// <summary> Add a byte[] to the message payload </summary><param name="value"> The value to add </param>
        public void Add(byte[] value) => _add(value);

        /// <summary> Add a float to the message payload </summary><param name="value"> The value to add </param>
        public void Add(float value) => _add(value);

        /// <summary> Add a double to the message payload </summary><param name="value"> The value to add </param>
        public void Add(double value) => _add(value);

        /// <summary> Add a bool to the message payload </summary><param name="value"> The value to add </param>
        public void Add(bool value) => _add(value);

        /// <summary>
        /// Add multiple objects to the message in one go.
        /// </summary>
        /// <example>
        /// Adding a string, a number and two boolean values to the message.
        /// <code> msg.Add("a string", 1234, true, false) </code>
        /// </example>
        /// <param name="parameters"> The objects to add to the message. </param>
        public void Add(params object[] parameters)
        {
            _add(parameters);
        }

        private void _add(params object[] parameters)
        {
            if (parameters.Length == 0)
                return;

            var allowedTypes = new List<Type>() {
                typeof(string), typeof(int),    typeof(uint),
                typeof(long),   typeof(ulong),  typeof(float),
                typeof(double), typeof(bool),   typeof(byte[])
            };

            foreach (var value in parameters)
            {
                if (value == null)
                    throw new Exception("PlayerIO messages do not support null objects.");

                if (!allowedTypes.Contains(value.GetType()))
                    throw new Exception($"PlayerIO messages do not support objects of type '{value.GetType()}'");

                this.Values.Add(value);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable)this.Values).GetEnumerator();
        }
    }
}
