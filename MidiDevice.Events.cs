using Commons.Music.Midi;

namespace Midi.Net;

public partial class MidiDevice
{
    private readonly List<EventHandler<MidiDevice>> _onReconnectSubscriptions = new();

    
    private readonly List<EventHandler<MidiReceivedEventArgs>> _messageReceivedHandlers = new();
    event EventHandler<MidiReceivedEventArgs>? IMidiInput.MessageReceived
    {
        add
        {
            if (value == null)
                return;

            lock (_messageReceivedHandlers)
            {
                _messageReceivedHandlers.Add(value);
            }
        }
        remove
        {
            if (value == null)
                return;

            lock (_messageReceivedHandlers)
            {
                _messageReceivedHandlers.Remove(value);
            }
        }
    }
}