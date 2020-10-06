using System.Collections.Generic;

namespace MoonScript.Interfaces
{
    using Lexical;

    internal interface ILexer
    {
        IEnumerable<Token> Tokenize(string source);
    }
}