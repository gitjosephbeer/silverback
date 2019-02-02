﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Silverback.Messaging.LargeMessages;

namespace Silverback.Tests.TestTypes
{
    public class InMemoryChunkStore : IChunkStore
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, byte[]>> _store = new ConcurrentDictionary<string, ConcurrentDictionary<int, byte[]>>();

        public void StoreChunk(string messageId, int chunkId, byte[] content) =>
            _store
                .GetOrAdd(messageId, _ => new ConcurrentDictionary<int, byte[]>())
                .AddOrUpdate(chunkId, content, (_, __) => content);

        public int CountChunks(string messageId) =>
            _store.Where(x => x.Key == messageId).Sum(x => x.Value.Count);

        public Dictionary<int, byte[]> GetChunks(string messageId) =>
            _store[messageId].ToDictionary(p => p.Key, p => p.Value);

        public void Cleanup(string messageId) =>
            _store.Remove(messageId, out _);
    }
}
