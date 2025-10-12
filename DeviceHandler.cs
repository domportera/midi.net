using Commons.Music.Midi;
using CoreMidi;

namespace LinnstrumentKeyboard;

public static class DeviceHandler
{
    
#pragma warning disable CS0618 // Type or member is obsolete
    private static readonly IMidiAccess MidiAccess = MidiAccessManager.Default;
#pragma warning restore CS0618 // Type or member is obsolete
    
    public readonly record struct DeviceOpenResult(IMidiInput? Input, IMidiOutput? Output)
    {
        public bool Success => Input != null && Output != null;
    }

    public static async Task<DeviceOpenResult> TryOpen(string deviceSearchTerm)
    {
        IMidiInput? midiInput = null;
        IMidiOutput? midiOutput = null;
        foreach (var input in MidiAccess.Inputs)
        {
            if (input == null)
                continue;

            if (input.Name.ToLowerInvariant().Contains(deviceSearchTerm))
            {
                midiInput = await MidiAccess.OpenInputAsync(input.Id);
                break;
            }
        }
        
        if (midiInput == null)
        {
            await Console.Error.WriteLineAsync("Failed to open MIDI device input");
            return new DeviceOpenResult(null, null);
        }

        foreach (var output in MidiAccess.Outputs)
        {
            if (output == null)
                continue;
            
            if (output.Name.ToLowerInvariant().Contains(deviceSearchTerm))
            {
                midiOutput = await MidiAccess.OpenOutputAsync(output.Id);
                break;
            }
        }

        if (midiOutput == null)
        {
            await Console.Error.WriteLineAsync("Failed to open MIDI device output");
            midiInput.Dispose();
            return new DeviceOpenResult(null, null);
        }

        return new DeviceOpenResult(midiInput, midiOutput);
    }
    
    public static void SendNrpn(this IMidiOutput device, int nrpn, int value, int channel)
    {
        var nrpnLsb = (byte)(nrpn & 0x7F);
        var nrpnMsb = (byte)((nrpn >> 7) & 0x7F);
        
        var valueLsb = (byte)(value & 0x7F);
        var valueMsb = (byte)((value >> 7) & 0x7F);

        lock(BufferLock)
        {
            var startPos = _bufferPos;
            AppendCC(new ControlChangeMessage(ControlChange.NrpnMsb, nrpnMsb), channel);
            AppendCC(new ControlChangeMessage(ControlChange.NrpnLsb, nrpnLsb), channel);

            AppendCC(new ControlChangeMessage(ControlChange.DataEntryMsb, valueMsb), channel);
            AppendCC(new ControlChangeMessage(ControlChange.DataEntryLsb, valueLsb), channel);

            var len = _bufferPos;
            _bufferPos = 0;
            device.Send(MidiSendBuffer, startPos, len, 0);
        }
    }

    private static void AppendCC(ControlChangeMessage message, int channel)
    {
        var midiEvt = new MidiEvent(new MidiStatus(StatusType.CC, (byte)channel), message);
        AppendMidiEvent(midiEvt);
    }

    private static void AppendMidiEvent(in MidiEvent evt) => _bufferPos += evt.CopyTo(MidiSendBuffer.AsSpan(_bufferPos));

    private static int _bufferPos;
    private static readonly Lock BufferLock = new();
    private static readonly byte[] MidiSendBuffer = new byte[32];
}