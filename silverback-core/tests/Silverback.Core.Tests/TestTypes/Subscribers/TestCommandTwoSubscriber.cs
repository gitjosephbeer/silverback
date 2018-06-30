﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.TestTypes.Subscribers
{
    public class TestCommandTwoSubscriber : Subscriber<TestCommandTwo>
    {
        public int Handled { get; set; }

        public TestCommandTwoSubscriber()
            : base(NullLoggerFactory.Instance)
        {
        }

        public override void Handle(TestCommandTwo message)
        {
            Handled++;
        }
    }
}