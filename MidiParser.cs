using System.Diagnostics.CodeAnalysis;
using Midi.Net.MidiUtilityStructs;

namespace Midi.Net;

public static class MidiParser
{
    public static ushort Value14Bit(byte msb, byte lsb) => (ushort)((msb << 7) | lsb);

    public static bool TryInterpret(ref MidiStatus? latest, ReadOnlySpan<byte> bytes,
        [NotNullWhen(true)] out MidiEvent? midi)
    {
        if (bytes.Length is not 2 && bytes.Length is not 3)
            throw new ArgumentException("MIDI message must 2 or 3 bytes long.", nameof(bytes));

        var evt = new RawMidiEvent(bytes);
        if (evt.IsRealTime)
        {
            midi = null;
#if DEBUG
            _ = Console.Out.WriteLineAsync("Ignoring real-time message - not yet supported");
#endif
            return false;
        }

        if (evt.HasStatus)
        {
            var status = evt.Status;
            latest = status;
            var count = (byte)(bytes.Length - 1);
            var span = count == 0 ? ReadOnlySpan<byte>.Empty : bytes[1..count];
            midi = new MidiEvent(status, new RawMidiData(span));
            return true;
        }

        if (latest != null)
        {
            // inherit the previous status (running status)
            midi = new MidiEvent(latest.Value, new RawMidiData(bytes));
            return true;
        }

        // No status available - spec says to ignore the message
        midi = null;
        return false;
    }
}