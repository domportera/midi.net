using System.Text;
using Commons.Music.Midi;
using MidiEvent = Midi.Net.MidiUtilityStructs.MidiEvent;

namespace Midi.Net;

public partial class MidiDevice
{
    private readonly StringBuilder _midiEventStringBuilder = new();
    private MidiEvent[] _midiEvents = [];
    private byte[] _buffer = [];
    private int _bufferedCount;
    private void OnMessageReceived(object? sender, MidiReceivedEventArgs e)
    {
        var dataSpan = new Span<byte>(e.Data, e.Start, e.Length);
        if (dataSpan.Length == 0)
            return;
        
        EnsureBufferLength(dataSpan);

        if (dataSpan.Length + _bufferedCount < 2)
        {
            // not enough data to interpret
            // append to our buffer and wait for more data
            dataSpan.CopyTo(_buffer.AsSpan(_bufferedCount));
            _bufferedCount += dataSpan.Length;
            return;
        }

        if (_bufferedCount > 0)
        {
            // we have data that has been buffered - we need too append to that
            // and then read from that
            dataSpan.CopyTo(_buffer.AsSpan(_bufferedCount));
            dataSpan = _buffer.AsSpan(0, _bufferedCount + dataSpan.Length);
            _bufferedCount = 0;
        }

        if (_midiEvents.Length < dataSpan.Length)
        {
            // ensure we have ample space for midi events
            _midiEvents = new MidiEvent[dataSpan.Length];
        }
            
        var eventCount = MidiParser.Interpret(ref _inputStatus, dataSpan, _midiEvents, _midiEventStringBuilder, out var bytesUsed);

        // copy any unused data to our buffer
        _bufferedCount = dataSpan.Length - bytesUsed;
        if (_bufferedCount > 0)
        {
            if(_buffer.Length < _bufferedCount)
            {
                Array.Resize(ref _buffer, _bufferedCount);
            }
            
            dataSpan[bytesUsed..].CopyTo(_buffer);
        }
        
        // log any errors from parsing
        if (_midiEventStringBuilder.Length > 0)
        {
            _ = Console.Out.WriteLineAsync(_midiEventStringBuilder.ToString());
            _midiEventStringBuilder.Clear();
        }
        
        // raise midi receive events
        var memory = new ReadOnlyMemory<MidiEvent>(_midiEvents, 0, eventCount);
        try
        {
            // todo - raise multiple with a single invocation - ReadOnlyMemory-style?
            MidiReceived?.Invoke(this, memory);
        }
        catch (Exception ex)
        {
            _ = Console.Error.WriteLineAsync(ex.ToString());
        }

        
        ForwardEvent(sender, e, _messageReceivedHandlers);
    }

    private void EnsureBufferLength(Span<byte> dataSpan)
    {
        if (dataSpan.Length + _bufferedCount >= _buffer.Length)
        {
            // increase buffer size
            Array.Resize(ref _buffer, dataSpan.Length + _bufferedCount);
        }
    }

    private static void ForwardEvent<T>(object? sender, T e, IList<EventHandler<T>> handlers)
    {
        lock (handlers)
        {
            for (var index = handlers.Count - 1; index >= 0; index--)
            {
                try
                {
                    handlers[index](sender, e);
                }
                catch (Exception ex)
                {
                    _ = Console.Error.WriteLineAsync(ex.ToString());
                }
            }
        }
    }

    #region managed-midi interface implementation

    IMidiPortDetails IMidiPort.Details => Input.Details;
    MidiPortConnectionState IMidiPort.Connection => Input.Connection;

    void IMidiOutput.Send(byte[] mevent, int offset, int length, long timestamp) =>
        Output.Send(mevent, offset, length, timestamp);

   

    #endregion


    async Task IMidiPort.CloseAsync() => await Dispose(true);
    void IDisposable.Dispose() => Dispose(true).Wait();
    async ValueTask IAsyncDisposable.DisposeAsync() => await Dispose(true);
    private async Task Dispose(bool disposing)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        
        if (disposing)
        {
            IsDisposed = true;
            GC.SuppressFinalize(this);
        }

        await _cancellationTokenSource.CancelAsync();
        _midiSendEvent.Dispose();
        lock (_onReconnectSubscriptions)
        {
            _onReconnectSubscriptions.Clear();
        }

        lock (_messageReceivedHandlers)
        {
            _messageReceivedHandlers.Clear();
        }

        _input.MessageReceived -= OnMessageReceived;

        try
        {
            if (Input.Connection == MidiPortConnectionState.Open)
            {
                await Input.CloseAsync();
            }
            
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync($"Error during device disposal: {e.Message}");
        }

        try
        {
            if (Output.Connection == MidiPortConnectionState.Open)
            {
                await Output.CloseAsync();
            }
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync($"Error during device disposal: {e.Message}");
        }
    }

    ~MidiDevice()
    {
        Dispose(false).Wait();
    }
}