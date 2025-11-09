using System.Runtime.CompilerServices;
using Commons.Music.Midi;
using Midi.Net.MidiUtilityStructs;
using MidiEvent = Midi.Net.MidiUtilityStructs.MidiEvent;

namespace Midi.Net;

public delegate ValueTask AsyncEventHandler<in T>(object? sender, T e);

// todo: seal this class, have a separate interface for device-specific functionality

public sealed partial class MidiDevice : IMidiInput, IMidiOutput, IAsyncDisposable
{
    private readonly AutoResetEvent _midiSendEvent = new(false);
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _inputConnected, _outputConnected;
    public event AsyncEventHandler<bool>? ConnectionStateChanged;

    private readonly MidiInputSlot _input;
    private readonly MidiOutputSlot _output;
    private readonly Lock _connectionStateLock = new();

    public MidiDevice(IMidiAccess2 access, DeviceHandler.DeviceSearchTerm inputTerm,
        DeviceHandler.DeviceSearchTerm outputTerm)
    {
        var input = new MidiInputSlot(inputTerm, access);
        var output = new MidiOutputSlot(outputTerm, access);
        _input = input;
        _output = output;

        input.ConnectionStateChanged += OnSlotStateChanged;
        output.ConnectionStateChanged += OnSlotStateChanged;

        var args = new MidiDevice.SendThreadArgs
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

    public async Task<Result> CloseAsync()
    {
        var inputClose = _input.CloseAsync();
        var outputClose = _output.CloseAsync();
        await Task.WhenAll(inputClose, outputClose);
        return ResultFactory.From(success: Input.Connection == MidiPortConnectionState.Closed &&
                                           Output.Connection == MidiPortConnectionState.Closed,
            messageIfFailed: "Failed to close MIDI device");
    }

    private async ValueTask OnSlotStateChanged(object? sender, bool e)
    {
        _connectionStateLock.Enter();
        var wasConnected = _inputConnected && _outputConnected;
        if (sender == Input)
        {
            _inputConnected = e;
        }
        else
        {
            _outputConnected = e;
        }

        var isConnected = _inputConnected && _outputConnected;
        _connectionStateLock.Exit();
        if (wasConnected != isConnected)
        {
            Console.WriteLine("Connection state changed: " + isConnected);
            try
            {
                if (ConnectionStateChanged != null)
                {
                    await ConnectionStateChanged.Invoke(this, isConnected);
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error in {nameof(OnSlotStateChanged)}: {ex}");
            }
        }
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

            if (!PushMidiImmediately())
            {
            }
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AppendMidiEvent(in MidiEvent evt, ref MidiDevice.Buffer buffer)
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

    private ref MidiDevice.Buffer GetBuffer()
    {
        if (_midiSendBuffer != default)
        {
            return ref _midiSendBuffer;
        }

        MidiDevice.Buffer buffer;
        lock (_bufferPoolLock)
        {
            _midiBufferPool.TryPop(out buffer);
        }

        if (buffer == default)
        {
            buffer = new MidiDevice.Buffer
            {
                Data = new byte[BufferSize],
                Position = 0
            };
        }

        _midiSendBuffer = buffer;
        return ref _midiSendBuffer;
    }

    private void ReturnBufferToPool(ref MidiDevice.Buffer buff)
    {
        lock (_bufferPoolLock)
        {
            buff.Position = 0;
            _midiBufferPool.Push(buff); // return to pool
        }
    }

    private readonly Lock _sendLock = new();
    private readonly Lock _bufferLock = new();
    private readonly Lock _bufferPoolLock = new();
    private Buffer _midiSendBuffer;
    private readonly Stack<Buffer> _midiBufferPool = new();
    private const int BufferSize = 4096;
    private readonly Lock _sendQueueLock = new();
    private readonly Queue<Buffer> _sendQueue = new();

    private struct Buffer : IEquatable<MidiDevice.Buffer>
    {
        public required byte[] Data { get; init; }
        public int Position;

        // equality ops
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(MidiDevice.Buffer left, MidiDevice.Buffer right) =>
            ReferenceEquals(left.Data, right.Data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(MidiDevice.Buffer left, MidiDevice.Buffer right) =>
            !ReferenceEquals(left.Data, right.Data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(MidiDevice.Buffer other) => ReferenceEquals(Data, other.Data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => obj is MidiDevice.Buffer other && ReferenceEquals(Data, other.Data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => Data.GetHashCode();
    }
}