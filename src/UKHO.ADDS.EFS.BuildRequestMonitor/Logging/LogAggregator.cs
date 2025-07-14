using System.Text;

namespace UKHO.ADDS.EFS.BuildRequestMonitor.Logging
{
    internal class JsonObjectAggregator
    {
        private readonly StringBuilder _buffer = new();
        private int _depth;
        private bool _escapeNext;
        private bool _insideString;

        public IEnumerable<string> Append(ReadOnlySpan<char> chunk)
        {
            List<string> completedObjects = new();

            for (var i = 0; i < chunk.Length; i++)
            {
                var c = chunk[i];
                _buffer.Append(c);

                if (_escapeNext)
                {
                    _escapeNext = false;
                    continue;
                }

                if (c == '\\')
                {
                    _escapeNext = true;
                    continue;
                }

                if (c == '"')
                {
                    _insideString = !_insideString;
                    continue;
                }

                if (_insideString)
                {
                    continue;
                }

                if (c == '{')
                {
                    _depth++;
                }
                else if (c == '}')
                {
                    _depth--;
                    if (_depth == 0)
                    {
                        // Complete JSON object detected
                        completedObjects.Add(_buffer.ToString());
                        _buffer.Clear();
                    }
                }
            }

            return completedObjects;
        }

        public void Reset()
        {
            _buffer.Clear();
            _depth = 0;
            _insideString = false;
            _escapeNext = false;
        }
    }
}
