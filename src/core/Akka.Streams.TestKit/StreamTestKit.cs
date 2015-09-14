﻿using System;
using System.Reactive.Streams;
using Akka.TestKit;
using Akka.Actor;
using Akka.Streams.Implementation;

namespace Akka.Streams.TestKit
{
    public class StreamTestKit
    {
        internal sealed class CompletedSubscription<T> : ISubscription
        {
            public ISubscriber<T> Subscriber { get; private set; }

            public CompletedSubscription(ISubscriber<T> subscriber)
            {
                Subscriber = subscriber;
            }

            public void Request(long n)
            {
                Subscriber.OnComplete();
            }

            public void Cancel()
            {
            }
        }

        internal sealed class FailedSubscription<T> : ISubscription
        {
            public ISubscriber<T> Subscriber { get; private set; }
            public Exception Cause { get; private set; }

            public FailedSubscription(ISubscriber<T> subscriber, Exception cause)
            {
                Subscriber = subscriber;
                Cause = cause;
            }

            public void Request(long n)
            {
                Subscriber.OnError(Cause);
            }

            public void Cancel()
            {
            }
        }

        internal sealed class PublisherProbeSubscription<T> : ISubscription
        {
            public ISubscriber<T> Subscriber { get; private set; }
            public TestProbe PublisherProbe { get; private set; }

            public PublisherProbeSubscription(ISubscriber<T> subscriber, TestProbe publisherProbe)
            {
                Subscriber = subscriber;
                PublisherProbe = publisherProbe;
            }

            public void Request(long n)
            {
                PublisherProbe.Ref.Tell(new TestPublisher.RequestMore(this, n));
            }

            public void Cancel()
            {
                PublisherProbe.Ref.Tell(new TestPublisher.CancelSubscription(this));
            }

            public void ExpectRequest(long n)
            {
                PublisherProbe.ExpectMsg<TestPublisher.RequestMore>(
                    x => x.NrOfElements == n && Equals(x.Subscription, this));
            }

            public long ExpectRequest()
            {
                return
                    PublisherProbe.ExpectMsg<TestPublisher.RequestMore>(x => Equals(this, x.Subscription)).NrOfElements;
            }

            public void ExpectCancellation()
            {
                PublisherProbe.FishForMessage(msg =>
                {
                    if (msg is TestPublisher.CancelSubscription &&
                        Equals(((TestPublisher.CancelSubscription) msg).Subscription, this)) return true;
                    if (msg is TestPublisher.RequestMore && Equals(((TestPublisher.RequestMore) msg).Subscription, this))
                        return false;
                    return false;
                });
            }

            public void SendNext(T element)
            {
                Subscriber.OnNext(element);
            }

            public void SendComplete()
            {
                Subscriber.OnComplete();
            }

            public void SendError(Exception cause)
            {
                Subscriber.OnError(cause);
            }

            public void SendOnSubscribe()
            {
                Subscriber.OnSubscribe(this);
            }
        }

        internal sealed class ProbeSource<T> : SourceModule<T, TestPublisher.Probe<T>>
        {
            private readonly TestKitBase _testKit;
            private readonly Attributes _attributes;

            public ProbeSource(TestKitBase testKit, Attributes attributes, SourceShape<T> shape) : base(shape)
            {
                _testKit = testKit;
                _attributes = attributes;
            }

            public override Attributes Attributes
            {
                get { return _attributes; }
            }

            public override IModule WithAttributes(Attributes attributes)
            {
                return new ProbeSource<T>(_testKit, attributes, AmendShape(attributes));
            }

            protected override SourceModule<T, TestPublisher.Probe<T>> NewInstance(SourceShape<T> shape)
            {
                return new ProbeSource<T>(_testKit, _attributes, shape);
            }

            public override IPublisher<T> Create(MaterializationContext context, out TestPublisher.Probe<T> materializer)
            {
                materializer = TestPublisher.CreateProbe<T>(_testKit);
                return materializer;
            }
        }

        internal sealed class ProbeSink<T> : SinkModule<T, TestSubscriber.Probe<T>>
        {
            private readonly TestKitBase _testKit;
            private readonly Attributes _attributes;

            public ProbeSink(TestKitBase testKit, Attributes attributes, SinkShape<T> shape) : base(shape)
            {
                _testKit = testKit;
                _attributes = attributes;
            }

            public override Attributes Attributes
            {
                get { return _attributes; }
            }

            public override IModule WithAttributes(Attributes attributes)
            {
                return new ProbeSink<T>(_testKit, attributes, AmendShape(attributes));
            }


            protected override SinkModule<T, TestSubscriber.Probe<T>> NewInstance(SinkShape<T> shape)
            {
                return new ProbeSink<T>(_testKit, _attributes, shape);
            }

            public override ISubscriber<T> Create(MaterializationContext context, out TestSubscriber.Probe<T> materializer)
            {
                materializer = _testKit.CreateProbe<T>();
                return materializer;
            }
        }
    }
}