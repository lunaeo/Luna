using System.Text.RegularExpressions;

namespace MoonScript.Lexical
{
    internal class TokenDefinition
    {
        public Regex Regex { get; private set; }
        public TokenType Type { get; private set; }
        public bool Ignored { get; private set; }

        public TokenDefinition(TokenType type, Regex regex) : this(type, regex, false)
        {
        }

        public TokenDefinition(TokenType type, Regex regex, bool ignored)
        {
            this.Type = type;
            this.Regex = regex;
            this.Ignored = ignored;
        }
    }
}