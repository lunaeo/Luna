using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MoonScript
{
    public class Trigger : IEquatable<Trigger>
    {
        internal Page Page { get; set; }

        public TriggerCategory Category { get; set; } = TriggerCategory.Undefined;
        public Queue<object> Contents { get; set; } = new Queue<object>();
        public Area Area { get; set; } = new Area();

        public int Id { get; set; } = -1;

        /// <summary>
        /// An optional parameter to specify any particular entity which had triggered the <see cref="TriggerCategory.Cause"/>.
        /// </summary>
        public object Context { get; set; }

        /// <summary>
        /// A list of successful <see cref="TriggerCategory.Condition"/>(s) evaluated prior.
        /// <para> Note: This property is only set on the last node, typically being the <see cref="TriggerCategory.Effect"/>. </para>
        /// </summary>
        public List<Trigger> Conditions { get; set; } = new List<Trigger>();

        /// <summary>
        /// A list of successful <see cref="TriggerCategory.Area"/>(s) evaluated prior.
        /// <para> Note: This property is only set on the last node, typically being the <see cref="TriggerCategory.Effect"/>. </para>
        /// </summary>
        public List<Trigger> Areas { get; set; } = new List<Trigger>();

        /// <summary>
        /// A list of successful <see cref="TriggerCategory.Filter"/>(s) evaluated prior.
        /// <para> Note: This property is only set on the last node, typically being the <see cref="TriggerCategory.Effect"/>. </para>
        /// </summary>
        public List<Trigger> Filters { get; set; } = new List<Trigger>();

        public string Description { get; internal set; }

        public Trigger(TriggerCategory category, int triggerId)
        {
            this.Category = category;
            this.Id = triggerId;
        }

        public T Get<T>(int index)
        {
            var content = this.Contents.ToArray()[index];

            if (content is Variable variable)
            {
                switch (variable.Type)
                {
                    case VariableType.Message:
                        content = this.Page.OnVariable(this, VariableType.Message, variable.Key);
                        break;

                    case VariableType.Number:
                        content = this.Page.OnVariable(this, VariableType.Number, variable.Key);
                        break;
                }
            }

            // seek and replace variables in string
            if (content is string)
            {
                foreach (var normalVariableRegex in this.Page.Engine.Lexer.Definitions.Where(x => x.Type == TokenType.Variable))
                {
                    foreach (var match in normalVariableRegex.Regex.Matches((string)content).Cast<Match>())
                    {
                        content = ((string)content).Replace(match.Value, this.Page.OnVariable(this, VariableType.Number, match.Value.Remove(0, 1))?.ToString());
                    }
                }

                foreach (var messageVariableRegex in this.Page.Engine.Lexer.Definitions.Where(x => x.Type == TokenType.MessageVariable))
                {
                    foreach (Match match in messageVariableRegex.Regex.Matches((string)content))
                    {
                        content = ((string)content).Replace(match.Value, this.Page.OnVariable(this, VariableType.Message, match.Value.Remove(0, 1))?.ToString());
                    }
                }
            }

            if (content == null)
            {
                content = default(T);
            }

            if (content is IntVariable)
            {
                return (T)content;
            }

            return (T)Convert.ChangeType(content, typeof(T));
        }

        public string GetVariableName(int index)
        {
            var content = this.Contents.ToArray()[index];

            if (content is Variable variable)
                return variable.Key;

            throw new Exception($"Index {index} in ({this.Category}:{this.Id}) is not a Variable.");
        }

        public bool Equals(Trigger other)
        {
            return other.Category == this.Category && other.Id == this.Id;
        }

        public override bool Equals(object obj)
        {
            if (obj is Trigger other)
                return other.Category == this.Category && other.Id == this.Id;

            return false;
        }

        public override int GetHashCode()
        {
            return ((int)this.Category * this.Id);
        }

        public object Get(int index) => this.Get<object>(index);

        public int GetInt(int index) => this.Get<int>(index);

        public uint GetUInt(int index) => this.Get<uint>(index);

        public double GetDouble(int index) => this.Get<double>(index);

        public string GetString(int index) => this.Get<string>(index);
    }
}