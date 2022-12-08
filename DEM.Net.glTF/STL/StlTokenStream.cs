using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IxMilia.Stl
{
    internal class StlTokenStream : IEnumerator<string>
    {
        private StreamReader _textReader;

        public StlTokenStream(Stream baseStream)
        {
            _textReader = new StreamReader(baseStream);
        }

        public string Current { get; private set; }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            _textReader.Dispose();
        }

        public bool MoveNext()
        {
            SwallowWhitespace();
            if (_textReader.EndOfStream)
            {
                Current = null;
                return false;
            }

            var sb = new StringBuilder();
            while (!_textReader.EndOfStream && !IsWhitespace((char)_textReader.Peek()))
            {
                sb.Append((char)_textReader.Read());
            }

            Current = sb.ToString();
            return true;
        }

        private void SwallowWhitespace()
        {
            bool keepSwallowing = true;
            while (keepSwallowing && !_textReader.EndOfStream)
            {
                if (IsWhitespace((char)_textReader.Peek()))
                {
                    _textReader.Read();
                }
                else
                {
                    break;
                }
            }
        }

        private static bool IsWhitespace(char c)
        {
            return c switch
            {
                ' ' or '\t' or '\r' or '\n' or '\f' or '\v' => true,
                _ => false,
            };
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }
    }
}
