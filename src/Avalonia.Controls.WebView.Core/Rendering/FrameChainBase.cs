using System;
using System.Collections.Generic;

namespace Avalonia.Controls.Rendering;

internal interface IFrameChainBase { }

internal abstract class FrameChainBase<TFrame, TSize> : IFrameChainBase
    where TSize : struct, IEquatable<TSize>
    where TFrame : class
{
    internal class FrameWrapper
    {
        public TFrame? Frame;
        public TSize Size;
        public long Version;
        public bool LockedByConsumer;
        public bool LockedByProducer;
        public bool Consumed;
    }

    private List<FrameWrapper> _frames;
    private object _lock = new();
    private long _nextVersion = 1;

    public FrameChainBase()
    {
        _frames = new();
        for (var c = 0; c < 3; c++)
            _frames.Add(new FrameWrapper());
        Producer = new ProducerImpl(this);
        Consumer = new ConsumerImpl(this);
    }

    protected abstract TFrame CreateFrame(TSize size);
    protected abstract void FreeFrame(TFrame frame);

    public IProducer Producer { get; }
    public IConsumer Consumer { get; }
    
    public interface IProducer : IDisposable
    {
        NextFrameDisposable GetNextFrame(TSize size, out TFrame frame);
    }

    public interface IConsumer : IDisposable
    {
        TFrame? CurrentFrame { get; }
        bool NextFrame();
    }

    class ProducerImpl(FrameChainBase<TFrame, TSize> chain) : IProducer
    {
        private FrameChainBase<TFrame, TSize>? _chain = chain;

        public NextFrameDisposable GetNextFrame(TSize size, out TFrame frame)
        {
            if (_chain == null)
                throw new InvalidOperationException("Frame Chain is disposed");
            FrameWrapper nextFrame;
            lock (_chain._lock)
            {
                // Check if there is a consumed frame that we can render to
                // or just use the oldest one if consumer can't keep up with producer
                nextFrame = _chain._frames.Find(
                                f => f is { Consumed: true, LockedByConsumer: false })
                            ?? _chain._frames
                                .Find(f => f is { LockedByConsumer: false }) ?? throw new InvalidOperationException();

                nextFrame.LockedByProducer = true;
            }

            if (nextFrame.Frame == null || !nextFrame.Size.Equals(size))
            {
                bool success = false;
                try
                {

                    if (nextFrame.Frame != null)
                        _chain.FreeFrame(nextFrame.Frame);
                    // Zero the value in case of an exception in CreateFrame
                    nextFrame.Frame = null;
                    nextFrame.Frame = _chain.CreateFrame(size);
                    nextFrame.Size = size;
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        lock (_chain._lock)
                        {
                            nextFrame.LockedByProducer = false;
                        }
                    }
                }
            }

            frame = nextFrame.Frame;
            return new NextFrameDisposable(_chain, nextFrame);
        }

        public void Dispose()
        {
            if (_chain == null)
                return;
            lock (_chain._lock)
            {
                foreach(var f in _chain._frames)
                    if (!f.LockedByConsumer)
                        f.LockedByProducer = true;
            }
            foreach(var f in _chain._frames)
                if (f is { LockedByProducer: true, Frame: not null })
                {
                    _chain.FreeFrame(f.Frame);
                    f.Frame = null;
                }

            _chain = null;
        }
    }
    
    class ConsumerImpl(FrameChainBase<TFrame, TSize> chain) : IConsumer
    {
        private FrameWrapper? _currentFrame;
        public TFrame? CurrentFrame => _currentFrame?.Frame;
        public bool NextFrame()
        {
            lock (chain._lock)
            {
                // We assume that frames are version-sorted at this point
                FrameWrapper? nextFrame = chain._frames
                    .Find(f => f is { Consumed: false, LockedByProducer: false });
                
                // There are no new frames to render, keep the current one
                if (nextFrame == null)
                    return false;

                if (_currentFrame != null) 
                    _currentFrame.LockedByConsumer = false;

                _currentFrame = nextFrame;
                nextFrame.Consumed = true;
                nextFrame.LockedByConsumer = true;
                return true;
            }
        }

        public void Dispose()
        {
            if (_currentFrame is { Frame: not null })
            {
                chain.FreeFrame(_currentFrame.Frame);
                _currentFrame.Frame = null;
            }
        }
    }

    internal ref struct NextFrameDisposable(FrameChainBase<TFrame, TSize> _chain, FrameWrapper nextFrame) : IDisposable
    {
        public void Dispose()
        {
            lock (_chain._lock)
            {
                nextFrame.Version = ++_chain._nextVersion;
                nextFrame.LockedByProducer = false;
                nextFrame.Consumed = false;
                _chain._frames.Sort(static (l, r) => l.Version.CompareTo(r.Version));
            }
        }
    }
}

