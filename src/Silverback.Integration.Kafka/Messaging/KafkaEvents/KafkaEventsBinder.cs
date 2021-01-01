// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Confluent.Kafka;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.KafkaEvents.Statistics;

namespace Silverback.Messaging.KafkaEvents
{
    internal static class KafkaEventsBinder
    {
        public static void SetEventsHandlers(
            this IConfluentProducerBuilder producerBuilder,
            KafkaProducer producer,
            ISilverbackLogger logger) =>
            producerBuilder
                .SetStatisticsHandler((_, statistics) => OnStatistics(statistics, producer, logger))
                .SetLogHandler((_, logMessage) => OnLog(logMessage, producer, logger));

        public static void SetEventsHandlers(
            this IConfluentConsumerBuilder consumerBuilder,
            KafkaConsumer consumer,
            ISilverbackLogger logger) =>
            consumerBuilder
                .SetStatisticsHandler((_, statistics) => OnStatistics(statistics, consumer, logger))
                .SetPartitionsAssignedHandler(
                    (_, partitions) => OnPartitionsAssigned(partitions, consumer, logger))
                .SetPartitionsRevokedHandler(
                    (_, partitions) => OnPartitionsRevoked(partitions, consumer, logger))
                .SetOffsetsCommittedHandler((_, offsets) => OnOffsetsCommitted(offsets, consumer, logger))
                .SetErrorHandler((_, error) => OnError(error, consumer, logger))
                .SetLogHandler((_, logMessage) => OnLog(logMessage, consumer, logger));

        private static void OnStatistics(string statistics, KafkaProducer producer, ISilverbackLogger logger)
        {
            logger.LogProducerStatisticsReceived(statistics, producer);

            producer.Endpoint.Events.StatisticsHandler?.Invoke(
                KafkaStatisticsDeserializer.TryDeserialize(statistics, logger),
                statistics,
                producer);
        }

        private static void OnStatistics(string statistics, KafkaConsumer consumer, ISilverbackLogger logger)
        {
            logger.LogConsumerStatisticsReceived(statistics, consumer);

            consumer.Endpoint.Events.StatisticsHandler?.Invoke(
                KafkaStatisticsDeserializer.TryDeserialize(statistics, logger),
                statistics,
                consumer);
        }

        private static IEnumerable<TopicPartitionOffset> OnPartitionsAssigned(
            List<TopicPartition> partitions,
            KafkaConsumer consumer,
            ISilverbackLogger logger)
        {
            partitions.ForEach(topicPartition => logger.LogPartitionAssigned(topicPartition, consumer));

            var topicPartitionOffsets =
                consumer.Endpoint.Events.PartitionsAssignedHandler?.Invoke(partitions, consumer).ToList() ??
                partitions.Select(partition => new TopicPartitionOffset(partition, Offset.Unset)).ToList();

            foreach (var topicPartitionOffset in topicPartitionOffsets)
            {
                if (topicPartitionOffset.Offset != Offset.Unset)
                    logger.LogPartitionOffsetReset(topicPartitionOffset, consumer);
            }

            consumer.OnPartitionsAssigned(
                topicPartitionOffsets.Select(
                    topicPartitionOffset =>
                        topicPartitionOffset.TopicPartition).ToList());

            return topicPartitionOffsets;
        }

        private static void OnPartitionsRevoked(
            List<TopicPartitionOffset> partitions,
            KafkaConsumer consumer,
            ISilverbackLogger logger)
        {
            consumer.OnPartitionsRevoked();

            partitions.ForEach(
                topicPartitionOffset => logger.LogPartitionRevoked(topicPartitionOffset, consumer));

            consumer.Endpoint.Events.PartitionsRevokedHandler?.Invoke(partitions, consumer);
        }

        private static void OnOffsetsCommitted(
            CommittedOffsets offsets,
            KafkaConsumer consumer,
            ISilverbackLogger logger)
        {
            foreach (var topicPartitionOffsetError in offsets.Offsets)
            {
                if (topicPartitionOffsetError.Offset == Offset.Unset)
                    continue;

                if (topicPartitionOffsetError.Error != null &&
                    topicPartitionOffsetError.Error.Code != ErrorCode.NoError)
                {
                    logger.LogOffsetCommitError(topicPartitionOffsetError, consumer);
                }
                else
                {
                    logger.LogOffsetCommitted(topicPartitionOffsetError.TopicPartitionOffset, consumer);
                }
            }

            consumer.Endpoint.Events.OffsetsCommittedHandler?.Invoke(offsets, consumer);
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private static void OnError(Error error, KafkaConsumer consumer, ISilverbackLogger logger)
        {
            // Ignore errors if not consuming anymore
            // (lidrdkafka randomly throws some "brokers are down" while disconnecting)
            if (!consumer.IsConnected)
                return;

            try
            {
                if (consumer.Endpoint.Events.ErrorHandler != null &&
                    consumer.Endpoint.Events.ErrorHandler.Invoke(error, consumer))
                    return;
            }
            catch (Exception ex)
            {
                logger.LogKafkaErrorHandlerError(consumer, ex);
            }

            if (error.IsFatal)
                logger.LogConfluentConsumerFatalError(error, consumer);
            else
                logger.LogConfluentConsumerError(error, consumer);
        }

        private static void OnLog(LogMessage logMessage, KafkaProducer producer, ISilverbackLogger logger)
        {
            switch (logMessage.Level)
            {
                case SyslogLevel.Emergency:
                case SyslogLevel.Alert:
                case SyslogLevel.Critical:
                    logger.LogConfluentProducerLogCritical(logMessage, producer);
                    break;
                case SyslogLevel.Error:
                    logger.LogConfluentProducerLogError(logMessage, producer);
                    break;
                case SyslogLevel.Warning:
                    logger.LogConfluentProducerLogWarning(logMessage, producer);
                    break;
                case SyslogLevel.Notice:
                case SyslogLevel.Info:
                    logger.LogConfluentProducerLogInformation(logMessage, producer);
                    break;
                default:
                    logger.LogConfluentProducerLogDebug(logMessage, producer);
                    break;
            }
        }

        private static void OnLog(LogMessage logMessage, KafkaConsumer consumer, ISilverbackLogger logger)
        {
            switch (logMessage.Level)
            {
                case SyslogLevel.Emergency:
                case SyslogLevel.Alert:
                case SyslogLevel.Critical:
                    logger.LogConfluentConsumerLogCritical(logMessage, consumer);
                    break;
                case SyslogLevel.Error:
                    logger.LogConfluentConsumerLogError(logMessage, consumer);
                    break;
                case SyslogLevel.Warning:
                    logger.LogConfluentConsumerLogWarning(logMessage, consumer);
                    break;
                case SyslogLevel.Notice:
                case SyslogLevel.Info:
                    logger.LogConfluentConsumerLogInformation(logMessage, consumer);
                    break;
                default:
                    logger.LogConfluentConsumerLogDebug(logMessage, consumer);
                    break;
            }
        }
    }
}
