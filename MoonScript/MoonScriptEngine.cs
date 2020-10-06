using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MoonScript
{
    using Interfaces;
    using Lexical;

    [Serializable]
    public sealed class MoonScriptException : Exception
    {
        public MoonScriptException()
        {
        }

        public MoonScriptException(string message) : base(message)
        {
        }

        public MoonScriptException(string format, params object[] message) : base(string.Format(format, message))
        {
        }

        public MoonScriptException(string message, Exception inner) : base(message, inner)
        {
        }
    }

    public class MoonScriptEngine
    {
        private ILexer _lexer;
        internal List<Page> _pages;
        public List<Page> Pages => _pages;
        public Options Options { get; }
        internal Parser Parser { get; }
        internal Lexer Lexer => (Lexer)_lexer;

        public MoonScriptEngine(Options options = null)
        {
            if (options == null)
                this.Options = new Options();

            _lexer = new Lexer(definitions: new List<TokenDefinition>() {
                new TokenDefinition(TokenType.Trigger,         new Regex(@"\([0-9]{1}\:[0-9]{1,99999}\)",                            RegexOptions.Compiled)),
                new TokenDefinition(TokenType.MessageVariable,  new Regex($@"\{this.Options.MessageDeclarationSymbol}[\ba-zA-Z\d\D][\ba-zA-Z\d_]*",  RegexOptions.Compiled)),
                new TokenDefinition(TokenType.Variable, new Regex("\\%(\\w+(\\.[x|y])?)", RegexOptions.Compiled)),
                new TokenDefinition(TokenType.String,          new Regex(@"\" + this.Options.StringBeginSymbol + @"(.*?)\" + this.Options.StringEndSymbol,     RegexOptions.Compiled)),
                new TokenDefinition(TokenType.Number,          new Regex(@"[-+]?([0-9]*\.[0-9]+|[0-9]+)",                                              RegexOptions.Compiled)),
                new TokenDefinition(TokenType.Comment,         new Regex(@"\" + this.Options.CommentSymbol + @".*[\r|\n]",                                 RegexOptions.Compiled), ignored: true),
                new TokenDefinition(TokenType.Word,            new Regex(@"\w+",                                                                       RegexOptions.Compiled), ignored: true),
                new TokenDefinition(TokenType.Symbol,          new Regex(@"\W",                                                                        RegexOptions.Compiled), ignored: true),
                new TokenDefinition(TokenType.Whitespace,      new Regex(@"\s+",                                                                       RegexOptions.Compiled), ignored: true)
            });

            this.Parser = new Parser(lexer: _lexer);
            _pages = new List<Page>();
        }

        public Page LoadFromString(string source, PageExecuteTriggerHandler pageExecuteTrigger, CauseTriggerDiscoveryHandler pageDiscoverCauseTrigger)
        {
            var page = new Page(this, pageExecuteTrigger, pageDiscoverCauseTrigger)
                .InsertBlocks(this.Parser.Parse(source));
            
            _pages.Add(page);
            return page;
        }
    }

    public class Options
    {
        public Options()
        {
        }

        /// <summary>
        /// Allow an existing TriggerHandler to be overridden by newer TriggerHandler
        /// <para>Default: false</para>
        /// </summary>
        public bool CanOverrideTriggerHandlers { get; set; } = false;

        /// <summary>
        /// Beginning string literal symbol
        /// <para>Default: {</para>
        /// </summary>
        public char StringBeginSymbol { get; set; } = '{';

        /// <summary>
        /// Ending string literal symbol
        /// <para>Default: }</para>
        /// </summary>
        public char StringEndSymbol { get; set; } = '}';

        /// <summary>
        /// Message literal symbol
        /// <para>Default: ~</para>
        /// </summary>
        public char MessageDeclarationSymbol { get; set; } = '~';

        /// <summary>
        /// Variable literal symbol
        /// <para>Default: %</para>
        /// </summary>
        public char VariableDeclarationSymbol { get; set; } = '%';

        /// <summary>
        /// Comment literal symbol
        /// <para>Default: *</para>
        /// </summary>
        public string CommentSymbol { get; set; } = "*";
    }
}