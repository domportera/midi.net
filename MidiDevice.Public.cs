using Commons.Music.Midi;
using Midi.Net.MidiUtilityStructs;
using Midi.Net.MidiUtilityStructs.Enums;
using MidiEvent = Midi.Net.MidiUtilityStructs.MidiEvent;

namespace Midi.Net;

public partial class MidiDevice
{
    public bool IsConnected => Input.Connection == MidiPortConnectionState.Open &&
                               Output.Connection == MidiPortConnectionState.Open;
    
    public string Name => Input.Details.Name;
    
    public bool IsDisposed { get; private set; }
    public event EventHandler<ReadOnlyMemory<MidiEvent>>? MidiReceived;
    public ConnectionState ConnectionState => (ConnectionState)_input.Connection;
    

    public event EventHandler<MidiDevice>? Reconnected
    {
        add
        {
            if (value != null) _onReconnectSubscriptions.Add(value);
        }

        remove
        {
            if (value != null) _onReconnectSubscriptions.Remove(value);
        }
    }
    
    public required IMidiInput Input
    {
        get => _input!;
        init
        {
            _input = value;
            _input.MessageReceived += OnMessageReceived;
        }
    }

    public required IMidiOutput Output
    {
        get => _output!;
        init => _output = value;
    }

    public bool PushMidiImmediately()
    {
        Buffer buffer;
        lock (_sendQueueLock)
        {
            if (!_sendQueue.TryDequeue(out buffer))
                return true;
        }

        if (_output.Connection != MidiPortConnectionState.Open)
        {
            // todo - do we need options for discarding vs preserving?
            ReturnBufferToPool(ref buffer);
            return false;
        }

        try
        {
            lock (_sendLock) // one at a time pls
            {
                _output.Send(buffer.Data, 0, buffer.Position, 0);
            }

            ReturnBufferToPool(ref buffer);
            return true;
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

            ReturnBufferToPool(ref buffer);
            return false;
        }
    }

    public void CommitMidiEvent(in MidiEvent evt)
    {
        lock (_bufferLock)
        {
            var buffer = GetBuffer();
            AppendMidiEvent(evt, ref buffer);
        }
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
}