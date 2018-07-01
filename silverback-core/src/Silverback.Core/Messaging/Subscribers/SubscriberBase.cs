using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Subscribers
{
    /// <summary>
    /// Subscribes to the messages published in a bus.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <seealso cref="ISubscriber" />
    public abstract class SubscriberBase<TMessage> : ISubscriber
        where TMessage : IMessage
    {
        private ILogger _logger;

        /// <summary>
        /// Gets or sets an optional filter to be applied to the messages.
        /// </summary>
        public Func<TMessage, bool> Filter { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriberBase{TMessage}" /> class.
        /// </summary>
        /// <param name="filter">An optional filter to be applied to the messages</param>
        protected SubscriberBase(Func<TMessage, bool> filter = null)
        {
            Filter = filter;
        }

        /// <summary>
        /// Initializes the subscriber.
        /// </summary>
        /// <param name="bus">The subscribed bus.</param>
        public virtual void Init(IBus bus)
        {
            _logger = bus.GetLoggerFactory().CreateLogger<SubscriberBase<TMessage>>();
        }

        /// <summary>
        /// Checks whether the message must be handled according to its type and 
        /// applied filters.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="typedMessage">The typed message.</param>
        /// <returns></returns>
        protected bool MustHandle(IMessage message, out TMessage typedMessage)
        {
            if (!(message is TMessage))
            {
                _logger.LogTrace($"Discarding message because it doesn't match type '{typeof(TMessage).Name}'.");
                typedMessage = default;
                return false;
            }

            typedMessage = (TMessage)message;

            if (Filter != null && !Filter.Invoke(typedMessage))
            {
                _logger.LogTrace("Discarding message because it was filtered out by the custom filter function.");
                return false;
            }

            _logger.LogDebug($"Processing message of type '{typeof(TMessage).Name}'.");

            return true;
        }

        /// <summary>
        /// Called when a message is published.
        /// </summary>
        /// <param name="message">The message.</param>
        public void OnNext(IMessage message)
        {
            _logger.LogTrace("Message received (synchronously).");

            if (MustHandle(message, out var typedMessage))
            {
                Handle(typedMessage);
            }
        }

        /// <summary>
        /// Called when a message is published asynchronously.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public Task OnNextAsync(IMessage message)
        {
            _logger.LogTrace("Message received (asynchronously).");

            if (MustHandle(message, out var typedMessage))
            {
                return HandleAsync(typedMessage);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles the <see cref="T:Silverback.Messaging.Messages.IMessage" />.
        /// </summary>
        /// <param name="message">The message to be handled.</param>
        public abstract void Handle(TMessage message);

        /// <summary>
        /// Handles the <see cref="T:Silverback.Messaging.Messages.IMessage" /> asynchronously.
        /// </summary>
        /// <param name="message">The message to be handled.</param>
        /// <returns></returns>
        public abstract Task HandleAsync(TMessage message);
    }
}