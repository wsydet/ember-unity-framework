// ParserToken.cs — Internal token representation for the lexer and parser.
// Part of LitJSON library. The authors disclaim copyright to this source code.

namespace Ember.Basic.LitJson
{
    internal enum ParserToken
    {
        // Lexer tokens
        None = System.Char.MaxValue + 1,
        Number,
        True,
        False,
        Null,
        CharSeq,
        // Single char
        Char,

        // Parser Rules
        Text,
        Object,
        ObjectPrime,
        Pair,
        PairRest,
        Array,
        ArrayPrime,
        Value,
        ValueRest,
        String,

        // End of input
        End,

        // The empty rule
        Epsilon
    }
}
