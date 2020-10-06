namespace MoonScript
{
    public class Variable
    {
        public VariableType Type { get; set; }

        public string Key { get; set; }
        public object Value { get; set; }

        public Variable(VariableType type, string key, object value)
        {
            this.Type = type;

            this.Key = key;
            this.Value = value;
        }
    }

    public class IntVariable
    {
        public IntVariable(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public int X { get; }
        public int Y { get; }
    }
}