
using Commons.Music.Midi;
using MidiEvent = Midi.Net.MidiUtilityStructs.MidiEvent;

namespace Midi.Net;

public sealed class MidiInputSlot : MidiSlot, IMidiInput
{
    public event EventHandler<ReadOnlyMemory<MidiEvent>>? MidiReceived
    {
        add
        {
            if(value != null)
                _midiReceivedHandlers.Add(value);
        }
        remove
        {
            if(value != null) 
                _midiReceivedHandlers.Remove(value);
        }
    }

    
    public MidiInputSlot(DeviceHandler.DeviceSearchTerm searchTerm, IMidiAccess2 access) : base(access, searchTerm)
    {
        _onMessageReceived = OnMessageReceivedInternal;
    }

    public event EventHandler<MidiReceivedEventArgs>? MessageReceived
    {
        add
        {
            if (value != null)
            {
                _messageReceivedHandlers.Add(value);
            }
        }
        remove
        {
            if (value != null)
            {
                _messageReceivedHandlers.Remove(value);
            }
        }
    }

    protected override Task<Result<IMidiPort>> BeginConnectTask(DeviceHandler.DeviceSearchTerm searchTerm)
    {
        return DeviceHandler.TryOpenInput(searchTerm).Cast<IMidiInput, IMidiPort>();
    }


    protected override void OnConnectionStateChanged(IMidiPort? port)
    {
        if (Input != null)
        {
            Input.MessageReceived -= _onMessageReceived;
        }
        
        if (port == null)
        {
            Input = null;
        }
        else
        {
            var input = (IMidiInput)port;
            input.MessageReceived += _onMessageReceived;
            
            Input = input;
        }
    }
    
    void OnMessageReceivedInternal(object? sender, MidiReceivedEventArgs e)
    {
        ForwardEvents(_messageReceivedHandlers, e);
        
        if(_midiParseEngine.ProcessMessageReceived(e, out var events))
        {
            ForwardEvents(_midiReceivedHandlers, events.Value);
        }
    }

    private static void ForwardEvents<T>(IReadOnlyList<EventHandler<T>> handlers, T e)
    {
        // iterate through message handlers
        // doing it this way allows us to catch exceptions on a per-listener basis,
        // preventing the entire event from being dropped by a single bad listener
        
        // go in reverse order so if anyone removes themselves from the list when invoked,
        // we don't have any issues
        
        for(int i = handlers.Count - 1; i >= 0; i--)
        {
            try
            {
                handlers[i](null, e);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }
    }

    protected override void OnDispose(bool disposing)
    {
    }

    private IMidiInput? Input { get; set; }
    private readonly MidiParseEngine _midiParseEngine = new();
    private readonly List<EventHandler<MidiReceivedEventArgs>> _messageReceivedHandlers = new();
    private readonly List<EventHandler<ReadOnlyMemory<MidiEvent>>> _midiReceivedHandlers = new();
    private readonly EventHandler<MidiReceivedEventArgs> _onMessageReceived;
}