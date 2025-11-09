using System.Text;
using Commons.Music.Midi;
using MidiEvent = Midi.Net.MidiUtilityStructs.MidiEvent;

namespace Midi.Net;

public partial class MidiDevice
{
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

        Input.Dispose();
        Output.Dispose();
    }
    
    event EventHandler<MidiReceivedEventArgs>? IMidiInput.MessageReceived
    {
        add => _input.MessageReceived += value;
        remove => _input.MessageReceived -= value;
    }
    
    public event EventHandler<ReadOnlyMemory<MidiEvent>>? MidiReceived
    {
        add => _input.MidiReceived += value;
        remove => _input.MidiReceived -= value;
    }

    ~MidiDevice()
    {
        Dispose(false).Wait();
    }
}