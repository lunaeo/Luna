namespace MoonScript.Lexical
{
    internal class Token
    {
        public TokenPosition Position { get; set; }
        public TokenType Type { get; set; }
        public string Value { get; set; }

        public Token(TokenType type, string value, TokenPosition position)
        {
            this.Type = type;
            this.Value = value;
            this.Position = position;
        }

        public override string ToString()
        {
            return $"Token: {{ Type: \"{this.Type}\", Value: \"{this.Value}\", Position: {{ Index: \"{this.Position.Index}\", Line: \"{this.Position.Line}\", Column: \"{this.Position.Column}\" }} }}";
        }
    }
}