using System.Text;
using Commons.Music.Midi;
using MidiEvent = Midi.Net.MidiUtilityStructs.MidiEvent;

namespace Midi.Net;

public partial class MidiDevice
{
    private readonly StringBuilder _midiEventStringBuilder = new();
    private MidiEvent[] _midiEvents = [];
    private void OnMessageReceived(object? sender, MidiReceivedEventArgs e)
    {
        var dataSpan = new ReadOnlySpan<byte>(e.Data, e.Start, e.Length);
        if (dataSpan.Length == 0)
            return;

        if (_midiEvents.Length < dataSpan.Length)
        {
            // intentionally keep this array as small as possible
            _midiEvents = new MidiEvent[dataSpan.Length];
        }
            
            
        var eventCount = MidiParser.Interpret(ref _inputStatus, dataSpan, _midiEvents, _midiEventStringBuilder);
        if (_midiEventStringBuilder.Length > 0)
        {
            _ = Console.Out.WriteLineAsync(_midiEventStringBuilder.ToString());
            _midiEventStringBuilder.Clear();
        }

        if (eventCount == 0)
            return;
        
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