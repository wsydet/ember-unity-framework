// JsonException.cs — Exception type for LitJSON parsing errors.
// Part of LitJSON library. The authors disclaim copyright to this source code.

using System;

namespace Ember.Basic.LitJson
{
    public class JsonException : ApplicationException
    {
        public JsonException() : base() { }

        internal JsonException(ParserToken token) :
            base($"Invalid token '{token}' in input string") { }

        internal JsonException(ParserToken token, Exception innerException) :
            base($"Invalid token '{token}' in input string", innerException) { }

        internal JsonException(int c) :
            base($"Invalid character '{(char)c}' in input string") { }

        internal JsonException(int c, Exception innerException) :
            base($"Invalid character '{(char)c}' in input string", innerException) { }

        public JsonException(string message) : base(message) { }

        public JsonException(string message, Exception innerException) :
            base(message, innerException) { }
    }
}
