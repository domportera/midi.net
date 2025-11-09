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
    public ConnectionState ConnectionState => (ConnectionState)Math.Max((int)Input.Connection, (int)Output.Connection);

    public IMidiInput Input => _input;

    public IMidiOutput Output => _output;
    
    public void PushMidi()
    {
        MidiDevice.Buffer buffer;
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


    public bool PushMidiImmediately()
    {
        MidiDevice.Buffer buffer;
        lock (_sendQueueLock)
        {
            if (!_sendQueue.TryDequeue(out buffer))
                return true;
        }

        if (Output.Connection != MidiPortConnectionState.Open)
        {
            // todo - do we need options for discarding vs preserving?
            ReturnBufferToPool(ref buffer);
            return false;
        }

        try
        {
            lock (_sendLock) // one at a time pls
            {
                Output.Send(buffer.Data, 0, buffer.Position, 0);
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

    public void BeginConnect()
    {
        _input.BeginConnect();
        _output.BeginConnect();
    }
}