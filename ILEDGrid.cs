using System.Diagnostics.CodeAnalysis;
using Midi.Net.MidiUtilityStructs;

namespace Midi.Net;

public interface ILEDGrid
{
    public void CommitLED(int x, int y, LedColor color);
    public void PushLEDs();
}

public interface IMidiGrid
{
    bool TryParseMidiEvent(MidiEvent midiEvent, out PadStatusEvent? padStatusEvent, [NotNullWhen(false)] out string? reason);
}

// based on Linnstrument colors for now
public enum LedColor : byte
{
    Off = byte.MaxValue,
    Default = 0,
    Red = 1,
    Yellow = 2,
    Green = 3,
    Cyan = 4,
    Blue = 5,
    Magenta = 6,
    White = 8,
    Orange = 9,
    Lime = 10,
    Pink = 11
}