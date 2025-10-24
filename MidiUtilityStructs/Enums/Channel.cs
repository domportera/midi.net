namespace Midi.Net.MidiUtilityStructs.Enums;

[Flags]
public enum Channel : ushort
{
    None = 0,
    Channel1 = 1,
    Channel2 = 1 << 1,
    Channel3 = 1 << 2,
    Channel4 = 1 << 3,
    Channel5 = 1 << 4,
    Channel6 = 1 << 5,
    Channel7 = 1 << 6,
    Channel8 = 1 << 7,
        
    Channel9 = 1 << 8,
    Channel10 = 1 << 9,
    Channel11 = 1 << 10,
    Channel12 = 1 << 11,
    Channel13 = 1 << 12,
    Channel14 = 1 << 13,
    Channel15 = 1 << 14,
    Channel16 = 1 << 15,
        
    Channel1To8 = Channel1 | Channel2 | Channel3 | Channel4 | Channel5 | Channel6 | Channel7 | Channel8,
    Channel9To16 = Channel8 | Channel9 | Channel10 | Channel11 | Channel12 | Channel13 | Channel14 | Channel15 | Channel16,
    All = Channel1To8 | Channel9To16
}