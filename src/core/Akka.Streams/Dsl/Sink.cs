﻿using System;
using System.Reactive.Streams;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Dispatch;
using Akka.Streams.Implementation;
using Akka.Streams.Implementation.Stages;

namespace Akka.Streams.Dsl
{
    /**
     * A `Sink` is a set of stream processing steps that has one open input and an attached output.
     * Can be used as a `Subscriber`
     */
    public sealed class Sink<TIn, TMat> : IGraph<SinkShape<TIn>, TMat>
    {
        private readonly IModule _module;

        public Sink(IModule module)
        {
            _module = module;
        }

        public SinkShape<TIn> Shape { get { return (SinkShape<TIn>)_module.Shape; } }
        public IModule Module { get { return _module; } }

        /**
         * Connect this `Sink` to a `Source` and run it. The returned value is the materialized value
         * of the `Source`, e.g. the `Subscriber` of a [[Source#subscriber]].
         */
        public TMat2 RunWith<TMat2>(IGraph<SourceShape<TIn>, TMat2> source, IMaterializer materializer)
        {
            return Source.Wrap(source).To(this).Run();
        }

        public Sink<TIn, TMat2> MapMaterializedValue<TMat2>(Func<TMat, TMat2> fn)
        {
            return new Sink<TIn, TMat2>(_module.TransformMaterializedValue(o => fn((TMat)o)));
        }

        public IGraph<SinkShape<TIn>, TMat> WithAttributes(Attributes attributes)
        {
            return new Sink<TIn, TMat>(_module.WithAttributes(attributes).Nest());
        }

        public IGraph<SinkShape<TIn>, TMat> Named(string name)
        {
            return WithAttributes(Attributes.CreateName(name));
        }
    }

    public static class Sink
    {
        private static SinkShape<T> Shape<T>(string name)
        {
            return new SinkShape<T>(new Inlet<T>(name + ".in"));
        }

        /**
         * A graph with the shape of a sink logically is a sink, this method makes
         * it so also in type.
         */
        public static Sink<TIn, TMat> Wrap<TIn, TMat>(IGraph<SinkShape<TIn>, TMat> graph)
        {
            return graph is Sink<TIn, TMat>
                ? graph as Sink<TIn, TMat>
                : new Sink<TIn, TMat>(graph.Module);
        }

        /**
         * Helper to create [[Sink]] from `Subscriber`.
         */
        public static Sink<TIn, object> Create<TIn>(ISubscriber<TIn> subscriber)
        {
            return new Sink<TIn, object>(new SubscriberSink<TIn>(subscriber, DefaultAttributes.SubscriberSink, Shape<TIn>("SubscriberSink")));
        }

        /**
         * A `Sink` that immediately cancels its upstream after materialization.
         */
        public static Sink<TIn, object> Cancelled<TIn>()
        {
            return new Sink<TIn, object>(new CancelSink(DefaultAttributes.CancelledSink, Shape<object>("CancelledSink")));
        }

        /**
         * A `Sink` that materializes into a `Future` of the first value received.
         */
        public static Sink<TIn, Task<TIn>> Head<TIn>()
        {
            return new Sink<TIn, Task<TIn>>(new HeadSink<TIn>(DefaultAttributes.HeadSink, Shape<TIn>("HeadSink")));
        }

        /**
         * A `Sink` that materializes into a [[org.reactivestreams.Publisher]].
         * that can handle one [[org.reactivestreams.Subscriber]].
         */
        public static Sink<TIn, IPublisher<TIn>> Publisher<TIn>()
        {
            return new Sink<TIn, IPublisher<TIn>>(new PublisherSink<TIn>(DefaultAttributes.PublisherSink, Shape<TIn>("PublisherSink")));
        }

        /**
         * A `Sink` that materializes into a [[org.reactivestreams.Publisher]]
         * that can handle more than one [[org.reactivestreams.Subscriber]].
         */
        public static Sink<TIn, IPublisher<TIn>> FanoutPublisher<TIn>(int initBufferSize, int maxBufferSize)
        {
            return new Sink<TIn, IPublisher<TIn>>(new FanoutPublisherSink<TIn>(initBufferSize, maxBufferSize, DefaultAttributes.FanoutPublisherSink, Shape<TIn>("FanoutPublisherSink")));
        }

        private static readonly Sink<object, Task> _ignore = new Sink<object, Task>(new BlackholeSink(DefaultAttributes.IgnoreSink, Shape<object>("BlackholeSink")));
        /**
         * A `Sink` that will consume the stream and discard the elements.
         */
        public static Sink<object, Task> Ignore { get { return _ignore; } }

        /**
         * A `Sink` that will invoke the given procedure for each received element. The sink is materialized
         * into a [[scala.concurrent.Future]] will be completed with `Success` when reaching the
         * normal end of the stream, or completed with `Failure` if there is a failure signaled in
         * the stream..
         */
        public static Sink<TIn, Task> ForEach<TIn>(Action<TIn> action)
        {
            throw new NotImplementedException();
        }

        /**
         * Combine several sinks with fun-out strategy like `Broadcast` or `Balance` and returns `Sink`.
         */
        public static Sink<TIn, object> Combine<TIn, TOut>(Func<int, IGraph<UniformFanOutShape<TIn, TOut>, object>> strategy, Sink<TOut, object> first, Sink<TOut, object> second, params Sink<TOut, object>[] rest)
        {
            throw new NotImplementedException();
        }

        /**
         * A `Sink` that will invoke the given function to each of the elements
         * as they pass in. The sink is materialized into a [[scala.concurrent.Future]]
         *
         * If `f` throws an exception and the supervision decision is
         * [[akka.stream.Supervision.Stop]] the `Future` will be completed with failure.
         *
         * If `f` throws an exception and the supervision decision is
         * [[akka.stream.Supervision.Resume]] or [[akka.stream.Supervision.Restart]] the
         * element is dropped and the stream continues.
         *
         * @see [[#mapAsyncUnordered]]
         */
        public static Sink<TIn, Task> ForEachParallel<TIn>(int parallelism, Action<TIn> action, MessageDispatcher dispatcher = null)
        {
            throw new NotImplementedException();
        }

        /**
         * A `Sink` that will invoke the given function for every received element, giving it its previous
         * output (or the given `zero` value) and the element as input.
         * The returned [[scala.concurrent.Future]] will be completed with value of the final
         * function evaluation when the input stream ends, or completed with `Failure`
         * if there is a failure signaled in the stream.
         */
        public static Sink<TIn, Task<TOut>> Fold<TIn, TOut>(TOut init, Func<TOut, TIn, TOut> aggregate)
        {
            throw new NotImplementedException();
        }

        /**
         * A `Sink` that when the flow is completed, either through a failure or normal
         * completion, apply the provided function with [[scala.util.Success]]
         * or [[scala.util.Failure]].
         */
        public static Sink<TIn, object> OnComplete<TIn>(Action<Exception> action)
        {
            throw new NotImplementedException();
        }

        /**
         * Sends the elements of the stream to the given `ActorRef`.
         * If the target actor terminates the stream will be canceled.
         * When the stream is completed successfully the given `onCompleteMessage`
         * will be sent to the destination actor.
         * When the stream is completed with failure a [[akka.actor.Status.Failure]]
         * message will be sent to the destination actor.
         *
         * It will request at most `maxInputBufferSize` number of elements from
         * upstream, but there is no back-pressure signal from the destination actor,
         * i.e. if the actor is not consuming the messages fast enough the mailbox
         * of the actor will grow. For potentially slow consumer actors it is recommended
         * to use a bounded mailbox with zero `mailbox-push-timeout-time` or use a rate
         * limiting stage in front of this `Sink`.
         */
        public static Sink<TIn, object> ActorRef<TIn>(IActorRef actorRef, object onCompleteMessage)
        {
            return new Sink<TIn, object>(new ActorRefSink<TIn>(actorRef, onCompleteMessage, DefaultAttributes.ActorRefSink, Shape<TIn>("ActorRefSink")));
        }

        /**
         * Creates a `Sink` that is materialized to an [[akka.actor.ActorRef]] which points to an Actor
         * created according to the passed in [[akka.actor.Props]]. Actor created by the `props` should
         * be [[akka.stream.actor.ActorSubscriber]].
         */
        public static Sink<TIn, IActorRef> ActorSubscriber<TIn>(Props props)
        {
            return new Sink<TIn, IActorRef>(new ActorSubscriberSink<TIn>(props, DefaultAttributes.ActorSubscriberSink, Shape<TIn>("ActorSubscriberSink")));
        }

        /**
         * Creates a `Sink` that is materialized as an [[akka.stream.SinkQueue]].
         * [[akka.stream.SinkQueue.pull]] method is pulling element from the stream and returns ``Future[Option[T]]``.
         * `Future` completes when element is available.
         *
         * `Sink` will request at most `bufferSize` number of elements from
         * upstream and then stop back pressure.
         *
         * @param bufferSize The size of the buffer in element count
         * @param timeout Timeout for ``SinkQueue.pull():Future[Option[T] ]``
         */
        public static Sink<TIn, ISinkQueue<TIn>> Queue<TIn>(int bufferSize, TimeSpan? timeout = null)
        {
            if(bufferSize < 0) throw new ArgumentException("Buffer size must be greater than or equal 0");
            return new Sink<TIn, ISinkQueue<TIn>>(new AcknowledgeSink<TIn>(bufferSize, timeout ?? TimeSpan.FromSeconds(5), DefaultAttributes.AcknowledgeSink, Shape<TIn>("AcknowledgeSink")));
        }
    }
}