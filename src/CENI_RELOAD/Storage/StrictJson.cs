using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Jaosnake.CeniReload.Storage
{
    // Syntax and duplicate-key validation before Unity's permissive deserializer.
    internal sealed class StrictJson
    {
        private readonly string _text;
        private int _position;
        private StrictJson(string text) { _text = text ?? ""; }
        public static void Validate(string text)
        {
            var parser = new StrictJson(text);
            parser.Space();
            if (parser.Peek != '{') throw new FormatException("Checkpoint must be a JSON object.");
            parser.Value(0);
            parser.Space();
            if (parser._position != parser._text.Length) throw new FormatException("Trailing JSON content.");
        }
        private char Peek => _position < _text.Length ? _text[_position] : '\0';
        private void Space() { while (Peek == ' ' || Peek == '\r' || Peek == '\n' || Peek == '\t') _position++; }
        private void Expect(char value) { Space(); if (Peek != value) throw new FormatException("Invalid JSON delimiter."); _position++; }
        private void Value(int depth)
        {
            if (depth > 64) throw new FormatException("JSON nesting limit exceeded.");
            Space();
            if (Peek == '{')
            {
                _position++; Space(); var keys = new HashSet<string>(StringComparer.Ordinal);
                if (Peek == '}') { _position++; return; }
                while (true)
                {
                    Space(); string key = String();
                    if (!keys.Add(key)) throw new FormatException("Duplicate JSON key: " + key);
                    Expect(':'); Value(depth + 1); Space();
                    if (Peek == '}') { _position++; return; }
                    Expect(',');
                }
            }
            if (Peek == '[')
            {
                _position++; Space();
                if (Peek == ']') { _position++; return; }
                while (true)
                {
                    Value(depth + 1); Space();
                    if (Peek == ']') { _position++; return; }
                    Expect(',');
                }
            }
            if (Peek == '"') { String(); return; }
            foreach (string literal in new[] { "true", "false", "null" })
            {
                if (_position + literal.Length <= _text.Length && string.CompareOrdinal(_text, _position, literal, 0, literal.Length) == 0)
                { _position += literal.Length; return; }
            }
            if (Peek == '-') _position++;
            if (Peek == '0') _position++;
            else { if (Peek < '1' || Peek > '9') throw new FormatException("Invalid JSON number."); Digits(); }
            if (Peek == '.') { _position++; RequiredDigits(); }
            if (Peek == 'e' || Peek == 'E') { _position++; if (Peek == '+' || Peek == '-') _position++; RequiredDigits(); }
        }
        private void Digits() { while (Peek >= '0' && Peek <= '9') _position++; }
        private void RequiredDigits() { if (Peek < '0' || Peek > '9') throw new FormatException("Incomplete JSON number."); Digits(); }
        private string String()
        {
            Expect('"'); var result = new StringBuilder();
            while (true)
            {
                if (_position >= _text.Length || Peek < ' ') throw new FormatException("Invalid JSON string.");
                char value = _text[_position++];
                if (value == '"') return result.ToString();
                if (value != '\\') { result.Append(value); continue; }
                if (_position >= _text.Length) throw new FormatException("Incomplete escape.");
                value = _text[_position++];
                switch (value)
                {
                    case '"': case '\\': case '/': result.Append(value); break;
                    case 'b': result.Append('\b'); break;
                    case 'f': result.Append('\f'); break;
                    case 'n': result.Append('\n'); break;
                    case 'r': result.Append('\r'); break;
                    case 't': result.Append('\t'); break;
                    case 'u':
                        if (_position + 4 > _text.Length || !ushort.TryParse(_text.Substring(_position, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ushort code))
                            throw new FormatException("Invalid Unicode escape.");
                        result.Append((char)code); _position += 4; break;
                    default: throw new FormatException("Invalid JSON escape.");
                }
            }
        }
    }
}
