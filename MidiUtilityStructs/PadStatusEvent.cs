
namespace Midi.Net.MidiUtilityStructs;

public enum PadAxis: int { Velocity = -1, X, Y, Z }
public readonly record struct PadStatusEvent
{
    public PadStatusEvent(int colX, int rowY, PadAxis axis, float rawValueAbsolute)
    {
        ColX = colX;
        RowY = rowY;
        Axis = axis;
        RawValueAbsolute = rawValueAbsolute; 
    }

    public PadStatusEvent(int colX, int rowY, PadAxis axis, byte value7Bit)
    {
        #if DEBUG
        if (value7Bit > 127)
            throw new ArgumentOutOfRangeException(nameof(value7Bit));
        #endif
        ColX = colX;
        RowY = rowY;
        Axis = axis;
        RawValueAbsolute = value7Bit * InverseVelocity;
    }
        
    private const float InverseVelocity = 1f / 127f;

    public int ColX { get; }
    public int RowY { get; }
    public PadAxis Axis { get; }
    public float RawValueAbsolute { get; }
}