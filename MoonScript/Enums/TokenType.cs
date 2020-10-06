namespace MoonScript
{
    internal enum TokenType : byte
    {
        Trigger,
        String,
        Number,
        MessageVariable,
        Variable,
        Comment,
        Whitespace,
        Word,
        Symbol,
        EOF
    }
}