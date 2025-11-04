using System.Runtime.CompilerServices;
using Commons.Music.Midi;
using Midi.Net.MidiUtilityStructs;
using MidiEvent = Midi.Net.MidiUtilityStructs.MidiEvent;

namespace Midi.Net;

// todo: seal this class, have a separate interface for device-specific functionality
public sealed partial class MidiDevice : IMidiInput, IMidiOutput, IAsyncDisposable
{
    private readonly AutoResetEvent _midiSendEvent = new(false);
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    public MidiDevice()
    {
        var args = new SendThreadArgs
        {
            AutoResetEvent = _midiSendEvent,
            CancellationToken = _cancellationTokenSource.Token
        };

        var thread = new Thread(MidiSendThread)
        {
            IsBackground = true,
            Name = "MIDI Send Thread"
        };
        thread.Start(args);
    }

    private class SendThreadArgs
    {
        public required AutoResetEvent AutoResetEvent { get; init; }
        public required CancellationToken CancellationToken { get; init; }
    }

    private void MidiSendThread(object? argsObj)
    {
        if (argsObj is not SendThreadArgs args)
        {
            const string msg = "Invalid argument for MIDI send thread: expected SendThreadArgs, got ";
            var errorMsg = argsObj == null ? msg + "null" : msg + argsObj.GetType().FullName;
            Console.Error.WriteLine(errorMsg);
            throw new ArgumentException(errorMsg);
        }

        var token = args.CancellationToken;
        var autoResetEvent = args.AutoResetEvent;
        var waitHandles = new[] { autoResetEvent, token.WaitHandle };
        while (true)
        {
            var signaledIndex = WaitHandle.WaitAny(waitHandles);
            if (signaledIndex == 1) // cancellation requested
                break;

            if(!PushMidiImmediately())
            {
                Reconnect();
            }
        }
    }

    private void Reconnect()
    {
        var wasAlreadyReconnecting = Interlocked.Exchange(ref _isReconnecting, true);
        if (wasAlreadyReconnecting)
            return;
        
        var token = _cancellationTokenSource.Token;

        // strategy #1 - let's see if this works
        // all we do is wait for the backend to mark their connection state as open again
        // if this doesn't work, we'll need to actually reconnect manually by disposing our input and output objects
        // and creating new ones
        // ReSharper disable once MethodSupportsCancellation
        Task.Run(async () =>
        {
            try
            {
                while (!token.IsCancellationRequested &&
                       _input.Connection != MidiPortConnectionState.Open &&
                       _output.Connection != MidiPortConnectionState.Open)
                {
                    // wait for the device to be ready
                    await Task.Delay(1000, token).ConfigureAwait(false);
                }

                if (!token.IsCancellationRequested &&
                    _input.Connection == MidiPortConnectionState.Open &&
                    _output.Connection == MidiPortConnectionState.Open)
                {
                    ForwardEvent(this, this, _onReconnectSubscriptions);
                }

            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync(ex.ToString());
            }
            
            _isReconnecting = false;
        });
    }

    private bool _isReconnecting;
    private readonly object _sendLock = new();
   

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AppendMidiEvent(in MidiEvent evt, ref Buffer buffer)
    {
        try
        {
            buffer.Position += evt.CopyTo(buffer.Data.AsSpan(buffer.Position..));
        }
        catch (IndexOutOfRangeException)
        {
            // buffer overflow - just ignore the rest of the data
            buffer.Position = BufferSize; // mark as full
        }
    }

    public void CommitCC(int channel, params Span<ControlChangeMessage> messages)
    {
        var channelByte = (byte)(channel & 0x0F);
        lock (_bufferLock)
        {
            ref var buffer = ref GetBuffer();

            foreach (var msg in messages)
            {
                var midiEvt = new MidiEvent(channelByte, msg);
                AppendMidiEvent(midiEvt, ref buffer);
            }
        }
    }

    private ref Buffer GetBuffer()
    {
        if (_midiSendBuffer != default)
        {
            return ref _midiSendBuffer;
        }

        Buffer buffer;
        lock (_bufferPoolLock)
        {
            _midiBufferPool.TryPop(out buffer);
        }

        if (buffer == default)
        {
            buffer = new Buffer
            {
                Data = new byte[BufferSize],
                Position = 0
            };
        }

        _midiSendBuffer = buffer;
        return ref _midiSendBuffer;
    }

    public void PushMidi()
    {
        Buffer buffer;
        lock (_bufferLock)
        {
            buffer = _midiSendBuffer;
            _midiSendBuffer = default; // consume the buffer
        }

        if (buffer == default)
            return; // nothing to send

        if (buffer.Position == 0)
        {
            ReturnBufferToPool(ref buffer);
            return;
        }

        lock (_sendQueueLock)
        {
            _sendQueue.Enqueue(buffer);
        }

        _midiSendEvent.Set();
    }

    private void ReturnBufferToPool(ref Buffer buff)
    {
        lock (_bufferPoolLock)
        {
            buff.Position = 0;
            _midiBufferPool.Push(buff); // return to pool
        }
    }
    

    private MidiStatus? _inputStatus;
    private readonly IMidiInput _input;
    private readonly IMidiOutput _output;

    private readonly Lock _bufferLock = new();
    private readonly Lock _bufferPoolLock = new();
    private Buffer _midiSendBuffer;
    private readonly Stack<Buffer> _midiBufferPool = new();
    private const int BufferSize = 4096;
    private readonly Lock _sendQueueLock = new();
    private readonly Queue<Buffer> _sendQueue = new();

    private struct Buffer : IEquatable<Buffer>
    {
        public required byte[] Data { get; init; }
        public int Position;

        // equality ops
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Buffer left, Buffer right) => ReferenceEquals(left.Data, right.Data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Buffer left, Buffer right) => !ReferenceEquals(left.Data, right.Data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Buffer other) => ReferenceEquals(Data, other.Data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => obj is Buffer other && ReferenceEquals(Data, other.Data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => Data.GetHashCode();
    }
}