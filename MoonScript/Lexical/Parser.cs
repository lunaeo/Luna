using System.Collections.Generic;

namespace MoonScript.Lexical
{
    using System.Linq;
    using Helpers;
    using Interfaces;

    internal class Parser
    {
        public ILexer Lexer { get; set; }

        public Parser(ILexer lexer)
        {
            this.Lexer = lexer;
        }

        public List<List<Trigger>> Parse(string source)
        {
            var triggerBlocks = new List<List<Trigger>>();
            var block = new List<Trigger>();

            Trigger currentTrigger = null,
                    previousTrigger = null;

            var tokens = this.Lexer.Tokenize(source).ToList();

            using (var iterator = tokens.GetEnumerator())
            {
                while (iterator.MoveNext())
                {
                    var token = iterator.Current;

                    switch (token.Type)
                    {
                        case TokenType.Trigger:
                            if (currentTrigger != null)
                            {
                                if (previousTrigger != null)
                                {
                                    if (previousTrigger.Category == TriggerCategory.Effect && currentTrigger.Category == TriggerCategory.Cause)
                                    {
                                        triggerBlocks.Add(block);
                                        block = new List<Trigger>();
                                    }
                                }

                                block.Add(currentTrigger);
                                previousTrigger = currentTrigger;
                            }

                            var category = (TriggerCategory)Helpers.IntParse(token.Value[1..token.Value.IndexOf(':')]);
                            var triggerId = token.Value.Substring(token.Value.IndexOf(':') + 1);
                            triggerId = triggerId[0..^1];

                            currentTrigger = new Trigger(category, Helpers.IntParse(triggerId));
                            break;

                        case TokenType.String:
                            token.Value = token.Value[1..^1];

                            currentTrigger.Contents.Enqueue(token.Value);
                            break;

                        case TokenType.MessageVariable:
                            var messageVariable = new Variable(VariableType.Message, token.Value[1..], null);

                            currentTrigger.Contents.Enqueue(messageVariable);
                            break;

                        case TokenType.Variable:
                            var normalVariable = new Variable(VariableType.Number, token.Value[1..], null);

                            currentTrigger.Contents.Enqueue(normalVariable);
                            break;

                        case TokenType.Number:
                            var value = double.Parse(token.Value, System.Globalization.NumberStyles.AllowDecimalPoint);

                            currentTrigger.Contents.Enqueue(value);
                            break;

                        case TokenType.EOF:
                            if (currentTrigger != null)
                            {
                                if (currentTrigger.Category != TriggerCategory.Undefined)
                                {
                                    block.Add(currentTrigger);
                                    triggerBlocks.Add(block);
                                }
                            }
                            break;
                    }
                }
            }

            return triggerBlocks;
        }
    }
}