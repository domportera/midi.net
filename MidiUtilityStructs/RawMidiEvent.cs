namespace Midi.Net.MidiUtilityStructs;

public readonly ref struct RawMidiEvent
{
    private readonly ReadOnlySpan<byte> _data;

    public RawMidiEvent(ReadOnlySpan<byte> bytes)
    {
        _data = bytes;
    }

    public bool HasStatus => (_data[0] & 0x80) == 0b10000000; // MSBit == 1 means status byte

    public MidiStatus Status => new(_data[0]);

    public bool IsRealTime
    {
        get
        {
            for (int i = 0; i < _data.Length; i++)
            {
                var midiByte = new MidiByte(_data[i]);
                if (!midiByte.IsStatusByte)
                    return false;
            }

            return true;
        }
    }
}