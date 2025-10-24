using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Midi.Net.MidiUtilityStructs;

namespace Midi.Net;

public static class MidiParser
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Value7BitNormalized(byte value) => value / 127f;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Value14BitNormalized(ushort value) => value / 16383f;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Value14BitNormalized(byte msb, byte lsb) => Value14BitNormalized(Value14Bit(msb, lsb));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort Value14Bit(byte msb, byte lsb) => (ushort)((msb << 7) | lsb);

    public static int Interpret(ref MidiStatus? latestStatus, ReadOnlySpan<byte> bytes, Span<MidiEvent> midiEvents, StringBuilder sb)
    {
        // find where the message ends - there may be multiple messages in a single call to this message
        // we know the message ends when we have a status byte with the status bit set
        
        int count = 0;
        if (bytes.Length < 2)
        {
            sb.Append("MIDI message must 2 or 3 bytes long. Length: ").Append(bytes.Length);
            return count;
        }
        
        // get the splits characterized by the status bits
        for (int i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            
            var status = new MidiStatus(b);
            int dataStartIndex;
            if (!status.IsStatusByte)
            {
                if (latestStatus != null)
                {
                    status = latestStatus.Value;
                    dataStartIndex = i;
                }
                else
                {
                    sb.AppendLine("No status available");
                    continue;
                }
            }
            else
            {
                dataStartIndex = i + 1;
            }

            var dataEndIndex = dataStartIndex + 1;
            if (dataEndIndex >= bytes.Length)
            {
                sb.AppendLine("MIDI message must be at least 2 bytes long");
                return count;
            }

            if (new MidiStatus(bytes[dataStartIndex]).IsStatusByte)
            {
                // this must be a "realtime" message - ignore for now
                sb.AppendLine("Ignoring real-time message - not yet supported");
                continue;
            }

            latestStatus = status;
            var midiEvent = new MidiEvent(status, bytes[dataStartIndex], bytes[dataEndIndex]);
            midiEvents[count++] = midiEvent;
            
            i = dataEndIndex;
        }

        return count;
    }
}