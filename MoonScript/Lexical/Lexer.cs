using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MoonScript.Lexical
{
    using Interfaces;

    internal class Lexer : ILexer
    {
        public List<TokenDefinition> Definitions { get; set; } = new List<TokenDefinition>();
        public Regex TerminatorPattern = new Regex(@"\r\n|\r|\n", RegexOptions.Compiled);

        public Lexer(List<TokenDefinition> definitions)
        {
            this.Definitions = definitions;
        }

        public IEnumerable<Token> Tokenize(string source)
        {
            var currentIndex = 0;
            var currentLine = 1;
            var currentColumn = 0;

            while (currentIndex < source.Length)
            {
                TokenDefinition tokenDefinition = null;
                Match tokenMatch = null;

                foreach (var definition in this.Definitions)
                {
                    var match = definition.Regex.Match(source, currentIndex);

                    if (match.Success && (match.Index - currentIndex) == 0)
                    {
                        tokenDefinition = definition;
                        tokenMatch = match;

                        break;
                    }
                }

                if (tokenDefinition == null)
                    throw new Exception($"Unrecognized symbol '{source[currentIndex]}' at index {currentIndex} (line {currentLine}, column {currentColumn}).");

                var value = source.Substring(currentIndex, tokenMatch.Length);

                if (!tokenDefinition.Ignored)
                    yield return new Token(tokenDefinition.Type, value, new TokenPosition(currentIndex, currentLine, currentColumn));

                var terminatorMatch = TerminatorPattern.Match(value);

                if (terminatorMatch.Success)
                {
                    currentLine += 1;
                    currentColumn = value.Length - (terminatorMatch.Index + terminatorMatch.Length);
                }
                else
                {
                    currentColumn += tokenMatch.Length;
                }

                currentIndex += tokenMatch.Length;
            }

            yield return new Token(TokenType.EOF, null, new TokenPosition(currentIndex, currentLine, currentColumn));
        }
    }
}