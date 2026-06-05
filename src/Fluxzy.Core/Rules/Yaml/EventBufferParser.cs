// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Fluxzy.Rules.Yaml
{
    /// <summary>
    ///     A minimal <see cref="IParser" /> that replays a buffered list of parsing events.
    /// </summary>
    internal sealed class EventBufferParser : IParser
    {
        private readonly IReadOnlyList<ParsingEvent> _events;
        private int _index = -1;

        public EventBufferParser(IReadOnlyList<ParsingEvent> events)
        {
            _events = events;
        }

        public ParsingEvent? Current => _index >= 0 && _index < _events.Count ? _events[_index] : null;

        public bool MoveNext()
        {
            if (_index < _events.Count) {
                _index++;
            }

            return _index < _events.Count;
        }
    }
}
