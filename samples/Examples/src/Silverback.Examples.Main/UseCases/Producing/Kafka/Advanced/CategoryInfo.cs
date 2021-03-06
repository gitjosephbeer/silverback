﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Examples.Main.Menu;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    public class CategoryInfo : ICategory
    {
        public string Title => "Advanced";

        public string Description => "Some more advanced use cases to get the most out of " +
                                     "Silverback and Apache Kafka.";

        public IEnumerable<Type> Children => new List<Type>
        {
            typeof(PartitioningUseCase),
            typeof(BatchProcessingUseCase),
            typeof(ChunkingUseCase),
            typeof(InteroperableMessageUseCase),
            typeof(HeadersUseCase),
            typeof(MultipleOutboundConnectorsUseCase),
            typeof(SameProcessUseCase),
            typeof(MultipleConsumerGroupsUseCase),
            typeof(BinaryFileUseCase),
            typeof(ProduceByteArrayUseCase),
            typeof(ProduceEmptyMessageUseCase),
            typeof(EncryptionUseCase),
            typeof(EncryptionAvroUseCase),
            typeof(CustomRoutingUseCase),
            typeof(BroadcastUseCase)
        };
    }
}
