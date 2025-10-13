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
    public event EventHandler<MidiEvent>? MidiReceived;
    public ConnectionState ConnectionState => (ConnectionState)Input.Connection;

    public async Task CloseAsync()
    {
        try
        {
            await Input.CloseAsync();
            await Output.CloseAsync();
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync(e.ToString());
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
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

    private void AppendMidiEvent(in MidiEvent evt) => _bufferPos += evt.CopyTo(_midiSendBuffer.AsSpan(_bufferPos..));

    private MidiStatus? _inputStatus;
    private int _bufferPos;

    internal IMidiInput Input
    {
        get => _input!;
        init
        {
            _input = value;
            _input.MessageReceived += OnMessageReceived;
        }
    }

    private IMidiInput? _input;
    private IMidiOutput? _output;

    internal IMidiOutput Output
    {
        get => _output!;
        init => _output = value;
    }

    private readonly Lock _bufferLock = new();
    private readonly byte[] _midiSendBuffer = new byte[4096];

    public void CommitCC(int channel, params Span<ControlChangeMessage> messages)
    {
        var channelByte = (byte)(channel & 0x0F);
        lock (_bufferLock)
        {
            foreach (var msg in messages)
            {
                var midiEvt = new MidiEvent(new MidiStatus(StatusType.CC, channelByte), msg);
                AppendMidiEvent(midiEvt);
            }
        }
    }

    public void PushMidi()
    {
        lock (_bufferLock)
        {
            var len = _bufferPos;
            if (len == 0)
            {
                return;
            }
            
            _bufferPos = 0;
            Output.Send(_midiSendBuffer, 0, len, 0);
        }
    }
}