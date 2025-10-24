using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Midi.Net.MidiUtilityStructs.Enums;

namespace Midi.Net.MidiUtilityStructs;

[StructLayout(LayoutKind.Sequential, Size = 1, Pack = 1)]
public readonly record struct MidiStatus
{
    [FixedAddressValueType] private readonly byte _value;
    public StatusType Type => (StatusType)(_value & 0xF0);
    public byte Channel => (byte)(_value & 0x0F);
    public Channel ChannelName => (Channel)(Channel + 1);
    public MidiStatus(byte value) => _value = value;

    public MidiStatus(StatusType type, byte channel)
    {
        _value = (byte)((byte)type | channel);
    }
    
    public bool IsStatusByte => (_value & 0b10000000) != 0 && _value != MidiConstants.Eox;

    public static explicit operator byte(MidiStatus status) => status._value;
    public static explicit operator MidiStatus(byte value) => new(value);
}