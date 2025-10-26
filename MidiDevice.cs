using System.Runtime.CompilerServices;
using Commons.Music.Midi;
using Midi.Net.MidiUtilityStructs;
using Midi.Net.MidiUtilityStructs.Enums;
using MidiEvent = Midi.Net.MidiUtilityStructs.MidiEvent;

namespace Midi.Net;

public partial class MidiDevice : IMidiInput, IMidiOutput
{
    public string Name => Input.Details.Name;
    public string Manufacturer => Input.Details.Manufacturer;
    public string Version => Input.Details.Version;
    public string Id => Input.Details.Id;
    public bool IsDisposed { get; private set; }
    public event EventHandler<ReadOnlyMemory<MidiEvent>>? MidiReceived;
    public ConnectionState ConnectionState => (ConnectionState)Input.Connection;
    private readonly AutoResetEvent _midiSendEvent = new(false);
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    protected MidiDevice()
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

            PushMidiImmediately();
        }
    }

    protected virtual Task<(bool Success, string? Error)> OnClose() => Task.FromResult<(bool, string?)>((true, null));

    protected internal virtual Task OnConnect() => Task.CompletedTask;

    public async Task CloseAsync()
    {
        try
        {
            var closeResult = await OnClose();
            if (!closeResult.Success)
            {
                await Console.Error.WriteLineAsync($"Error closing MIDI device: {closeResult.Error}");
            }
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Error closing MIDI device: {ex.Message}");
        }

        ObjectDisposedException.ThrowIf(IsDisposed, this);
        GC.SuppressFinalize(this);
        await Dispose(true);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _ = Dispose(true);
    }

    public void CommitNrpn(int nrpn, int value, int channel)
    {
        var nrpnLsb = (byte)(nrpn & 0x7F);
        var nrpnMsb = (byte)((nrpn >> 7) & 0x7F);

        var valueLsb = (byte)(value & 0x7F);
        var valueMsb = (byte)((value >> 7) & 0x7F);
        CommitCC(channel,
            new ControlChangeMessage(ControlChange.NrpnMsb, nrpnMsb),
            new ControlChangeMessage(ControlChange.NrpnLsb, nrpnLsb),
            new ControlChangeMessage(ControlChange.DataEntryMsb, valueMsb),
            new ControlChangeMessage(ControlChange.DataEntryLsb, valueLsb)
        );
    }

    public void PushMidiImmediately()
    {
        Buffer buffer;
        lock (_sendQueueLock)
        {
            if (!_sendQueue.TryDequeue(out buffer))
                return;
        }

        try
        {
            Output.Send(buffer.Data, 0, buffer.Position, 0);
        }
        catch (Exception ex)
        {
            try
            {
                Console.Error.WriteLine(ex.ToString());
            }
            catch
            {
                // ignore
            }
        }

        ReturnBufferToPool(ref buffer);
    }

    protected void AppendMidiEvent(in MidiEvent evt)
    {
        lock (_bufferLock)
        {
            var buffer = GetBuffer();
            AppendMidiEvent(evt, ref buffer);
        }
    }

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
            ;
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


    internal IMidiInput Input
    {
        get => _input!;
        init
        {
            _input = value;
            _input.MessageReceived += OnMessageReceived;
        }
    }


    internal IMidiOutput Output
    {
        get => _output!;
        init => _output = value;
    }


    private MidiStatus? _inputStatus;
    private readonly IMidiInput? _input;
    private readonly IMidiOutput? _output;

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