using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ExtensionUtilityExportInformationNet
{
    /// <summary>
    /// A lightweight JSON builder class that constructs JSON strings manually without external dependencies.
    /// Supports nested objects, arrays, key-value pairs, and primitive values.
    /// </summary>
    public class JsonBuilder
    {
        /// <summary>
        /// Internal StringBuilder to accumulate JSON content.
        /// </summary>
        private readonly StringBuilder _sb = new();

        /// <summary>
        /// Defines the type of context (object or array) on the stack.
        /// </summary>
        private enum ContextType
        {
            /// <summary>
            /// JSON object context "{...}".
            /// </summary>
            Object,
            /// <summary>
            /// JSON array context "[...]".
            /// </summary>
            Array
        }

        /// <summary>
        /// Represents the current context state (type and whether first element was written).
        /// </summary>
        private class Context
        {
            /// <summary>
            /// Gets the context type (Object or Array).
            /// </summary>
            public ContextType Type { get; }

            /// <summary>
            /// Gets or sets a flag indicating if the first element in this context was already written.
            /// </summary>
            public bool FirstElementWritten { get; set; }

            /// <summary>
            /// Initializes a new Context with the specified type.
            /// </summary>
            /// <param name="type">The context type.</param>
            public Context(ContextType type)
            {
                Type = type;
                FirstElementWritten = false;
            }
        }

        /// <summary>
        /// Stack of open contexts to track nesting and commas.
        /// </summary>
        private readonly Stack<Context> _stack = new();

        /// <summary>
        /// Writes a comma before the next element if needed (not first in context).
        /// </summary>
        private void WriteCommaIfNeeded()
        {
            if (_stack.Count == 0) return;

            var ctx = _stack.Peek();
            if (ctx.FirstElementWritten)
            {
                _sb.Append(',');
            }
            else
            {
                ctx.FirstElementWritten = true;
            }
        }

        /// <summary>
        /// Escapes a string value for JSON (handles quotes, backslashes, control chars).
        /// </summary>
        /// <param name="value">The string to escape.</param>
        /// <returns>The escaped string.</returns>
        private static string EscapeString(string value)
        {
            if (value == null)
                return "";

            var sb = new StringBuilder(value.Length + 10);
            foreach (var ch in value)
            {
                switch (ch)
                {
                    case '\\': sb.Append(@"\\"); break;
                    case '\"': sb.Append("\\\""); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (ch < 32)
                        {
                            sb.AppendFormat("\\u{0:X4}", (int)ch);
                        }
                        else
                        {
                            sb.Append(ch);
                        }
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Begins a new JSON object. If id is provided, writes it as "id":{ ... } inside current object.
        /// </summary>
        /// <param name="id">Optional key name for this object (empty for root or array element).</param>
        public void BeginObject(string id = "")
        {
            if (!string.IsNullOrEmpty(id))
            {
                if (_stack.Count == 0 || _stack.Peek().Type != ContextType.Object)
                    throw new InvalidOperationException("Named object can only be started inside an object.");

                WriteCommaIfNeeded();
                _sb.Append('\"').Append(EscapeString(id)).Append("\":");
            }
            else
            {
                if (_stack.Count > 0)
                {
                    WriteCommaIfNeeded();
                }
            }

            _sb.Append('{');
            _stack.Push(new Context(ContextType.Object));
        }

        /// <summary>
        /// Ends the current JSON object (writes closing brace).
        /// </summary>
        public void EndObject()
        {
            if (_stack.Count == 0 || _stack.Peek().Type != ContextType.Object)
                throw new InvalidOperationException("No open object to close.");

            _stack.Pop();
            _sb.Append('}');
        }

        /// <summary>
        /// Begins a new JSON array. If arrayId is provided, writes it as "arrayId":[ ... ] inside current object.
        /// </summary>
        /// <param name="arrayId">Optional key name for this array (empty for root or object element).</param>
        public void BeginArray(string arrayId = "")
        {
            if (!string.IsNullOrEmpty(arrayId))
            {
                if (_stack.Count == 0 || _stack.Peek().Type != ContextType.Object)
                    throw new InvalidOperationException("Named array can only be started inside an object.");

                WriteCommaIfNeeded();
                _sb.Append('\"').Append(EscapeString(arrayId)).Append("\":[");
            }
            else
            {
                if (_stack.Count > 0)
                {
                    WriteCommaIfNeeded();
                }
                _sb.Append('[');
            }

            _stack.Push(new Context(ContextType.Array));
        }

        /// <summary>
        /// Ends the current JSON array (writes closing bracket).
        /// </summary>
        public void EndArray()
        {
            if (_stack.Count == 0 || _stack.Peek().Type != ContextType.Array)
                throw new InvalidOperationException("No open array to close.");

            _stack.Pop();
            _sb.Append(']');
        }

        /// <summary>
        /// Adds a string key-value pair to the current object.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The string value.</param>
        public void AddStrPair(string key, string value)
        {
            EnsureObjectContext();
            WriteCommaIfNeeded();
            _sb.Append('\"').Append(EscapeString(key)).Append("\":");
            WriteStringValue(value);
        }

        /// <summary>
        /// Adds an int key-value pair to the current object.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The int value.</param>
        public void AddIntPair(string key, int value)
        {
            EnsureObjectContext();
            WriteCommaIfNeeded();
            _sb.Append('\"').Append(EscapeString(key)).Append("\":");
            _sb.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Adds a double key-value pair to the current object.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The double value.</param>
        public void AddFltPair(string key, double value)
        {
            EnsureObjectContext();
            WriteCommaIfNeeded();
            _sb.Append('\"').Append(EscapeString(key)).Append("\":");
            _sb.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Adds a bool key-value pair to the current object.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The bool value.</param>
        public void AddBoolPair(string key, bool value)
        {
            EnsureObjectContext();
            WriteCommaIfNeeded();
            _sb.Append('\"').Append(EscapeString(key)).Append("\":");
            _sb.Append(value ? "true" : "false");
        }

        /// <summary>
        /// Adds a string value to the current array.
        /// </summary>
        /// <param name="value">The string value.</param>
        public void AddStrValue(string value)
        {
            EnsureArrayContext();
            WriteCommaIfNeeded();
            WriteStringValue(value);
        }

        /// <summary>
        /// Adds an int value to the current array.
        /// </summary>
        /// <param name="value">The int value.</param>
        public void AddIntValue(int value)
        {
            EnsureArrayContext();
            WriteCommaIfNeeded();
            _sb.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Adds a double value to the current array.
        /// </summary>
        /// <param name="value">The double value.</param>
        public void AddFltValue(double value)
        {
            EnsureArrayContext();
            WriteCommaIfNeeded();
            _sb.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Adds a bool value to the current array.
        /// </summary>
        /// <param name="value">The bool value.</param>
        public void AddBoolValue(bool value)
        {
            EnsureArrayContext();
            WriteCommaIfNeeded();
            _sb.Append(value ? "true" : "false");
        }

        /// <summary>
        /// Writes a quoted escaped string value (internal).
        /// </summary>
        /// <param name="value">The string value.</param>
        private void WriteStringValue(string value)
        {
            _sb.Append('\"').Append(EscapeString(value ?? "")).Append('\"');
        }

        /// <summary>
        /// Ensures current context is an object (throws if not).
        /// </summary>
        private void EnsureObjectContext()
        {
            if (_stack.Count == 0 || _stack.Peek().Type != ContextType.Object)
                throw new InvalidOperationException("Key-value pairs can only be added inside an object.");
        }

        /// <summary>
        /// Ensures current context is an array (throws if not).
        /// </summary>
        private void EnsureArrayContext()
        {
            if (_stack.Count == 0 || _stack.Peek().Type != ContextType.Array)
                throw new InvalidOperationException("Values without key can only be added inside an array.");
        }

        /// <summary>
        /// Returns the complete JSON string. Throws if contexts are unbalanced.
        /// </summary>
        /// <param name="pretty">If true, formats JSON with indentation for readability.</param>
        /// <returns>The built JSON string.</returns>
        public string GetJsonString(bool pretty = false)
        {
            if (_stack.Count != 0)
                throw new InvalidOperationException("JSON is not complete: there are unclosed objects or arrays.");

            if (!pretty)
                return _sb.ToString();

           
            string raw = _sb.ToString();
            var result = new StringBuilder();
            int indent = 0;
            bool inString = false;
            bool escape = false;

            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];

                
                if (c == '\"')
                {
                    if (!escape)
                        inString = !inString;
                    result.Append(c);
                }
                else if (!inString)
                {
                    if (c == '{' || c == '[')
                    {
                        result.Append(c);
                        result.AppendLine();
                        indent += 4;
                        AppendIndent(result, indent);
                    }
                    else if (c == '}' || c == ']')
                    {
                        result.AppendLine();
                        indent -= 4;
                        AppendIndent(result, indent);
                        result.Append(c);
                    }
                    else if (c == ',' && i + 1 < raw.Length && raw[i + 1] != ' ')
                    {
                        result.Append(c);
                        result.AppendLine();
                        AppendIndent(result, indent);
                    }
                    else
                    {
                        result.Append(c);
                    }
                }
                else
                {
                    result.Append(c);
                }

                escape = (c == '\\') && !escape;
            }

            return result.ToString();
        }

        /// <summary>
        /// Appends indentation spaces to StringBuilder.
        /// </summary>
        /// <param name="sb">Target StringBuilder.</param>
        /// <param name="indent">Number of spaces.</param>
        private static void AppendIndent(StringBuilder sb, int indent)
        {
            sb.Append(' ', indent);
        }
    }
}
