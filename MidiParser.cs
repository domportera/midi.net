using System.Diagnostics.CodeAnalysis;
using Midi.Net.MidiUtilityStructs;

namespace Midi.Net;

public static class MidiParser
{
    public static ushort Value14Bit(byte msb, byte lsb) => (ushort)((msb << 7) | lsb);

    public static bool TryInterpret(ref MidiStatus? latest, ReadOnlySpan<byte> bytes,
        [NotNullWhen(true)] out MidiEvent? midi, [NotNullWhen(false)] out string? reason)
    {
        if (bytes.Length is not 2 && bytes.Length is not 3)
        {
            reason = "MIDI message must 2 or 3 bytes long. Length: " + bytes.Length;
            midi = null;
            return false;
        }

        var evt = new RawMidiEvent(bytes);
        if (evt.IsRealTime)
        {
            midi = null;
            reason = "Ignoring real-time message - not yet supported";
            return false;
        }

        if (evt.HasStatus)
        {
            var status = evt.Status;
            latest = status;
            var count = (byte)(bytes.Length - 1);
            var span = count == 0 ? ReadOnlySpan<byte>.Empty : bytes[1..count];
            midi = new MidiEvent(status, new RawMidiData(span));
            reason = null;
            return true;
        }

        if (latest != null)
        {
            // inherit the previous status (running status)
            midi = new MidiEvent(latest.Value, new RawMidiData(bytes));
            reason = null;
            return true;
        }

        // No status available - spec says to ignore the message
        midi = null;
        reason = "No status available";
        return false;
    }
}