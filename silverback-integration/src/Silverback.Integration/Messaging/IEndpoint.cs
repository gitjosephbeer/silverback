﻿// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging
{
    public interface IEndpoint
    {
        string Name { get; }

        IMessageSerializer Serializer { get; }
    }

    public interface IConsumerEndpoint : IEndpoint
    {
    }

    public interface IProducerEndpoint : IEndpoint
    {
        ChunkSettings Chunk { get; }
    }
}