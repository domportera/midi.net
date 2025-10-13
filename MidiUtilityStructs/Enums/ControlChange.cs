namespace Midi.Net.MidiUtilityStructs.Enums;

public enum ControlChange : byte
{
    // 0 - 31 - MSB of most continuous Controller Data. they obtain information from pedals, levers, wheels, etc

    // 32 - 63 - LSB for controllers 0-31. reserved for optional use for the above controllers. for example,
    // controller 7 (volume) could have a controller 39 (7 + 32) for fine volume control. when an MSB
    // is received on 0-31, the LSB is set to 0 until an LSB is received.

    // 64 - 95 - additional single-byte controllers 64-69 have been defined for switched functions (hold pedal, etc)
    // while 91-95 are for controlling the depth of certain external audio effects

    // 96 - 101 - Increment/Decrement and Parameter numbers
    // 102 - 119 - undefined single-byte controllers


    // 16-19 and 80-83 are defined as General Purpose Controllers. 16-19 are two-byte controllers (with 48-51 as their
    // optional LSBs). 80-83 are single-byte controllers.

    // virtually all controllers are defined as 0 being no effect, 127 being maximum effect. Three defined controllers 
    // are notably different: Balance, Pan, and Expression
    BankSelectMsb = 0,

    ModulationWheel = 1,
    BreathController = 2,
    FootController = 4,
    PortamentoTime = 5,
    DataEntryMsb = 6,
    ChannelVolume = 7,

    /// <summary>
    /// BALANCE: CC8 (08H) - 0 is full left/lower, 64 is equal balance, 127 is full right/upper
    /// </summary>
    Balance = 8,


    /// <summary>
    /// PAN: CC10 (0AH) - 0 is full left, 64 is center, 127 is full right
    /// </summary>
    Pan = 10,

    /// <summary>
    /// EXPRESSION: CC11 (0BH) - A form of volume accent above the programmed or main volume
    /// </summary>
    Expression = 11,

    EffectControl1 = 12,
    EffectControl2 = 13,

    GeneralPurpose1 = 16,
    GeneralPurpose2 = 17,
    GeneralPurpose3 = 18,
    GeneralPurpose4 = 19,

    LSB0 = 32,
    LSB1 = 33,
    LSB2 = 34,
    LSB3 = 35,
    LSB4 = 36,
    LSB5 = 37,
    DataEntryLsb = 38,
    LSB7 = 39,
    LSB8 = 40,
    LSB9 = 41,
    LSB10 = 42,
    LSB11 = 43,
    LSB12 = 44,
    LSB13 = 45,
    LSB14 = 46,
    LSB15 = 47,
    LSB16 = 48,
    LSB17 = 49,
    LSB18 = 50,
    LSB19 = 51,
    LSB20 = 52,
    LSB21 = 53,
    LSB22 = 54,
    LSB23 = 55,
    LSB24 = 56,
    LSB25 = 57,
    LSB26 = 58,
    LSB27 = 59,
    LSB28 = 60,
    LSB29 = 61,
    LSB30 = 62,
    LSB31 = 63,

    DamperSustainPedal = 64,
    PortamentoOnOff = 65,
    Sostenuto = 66,
    SoftPedal = 67,

    /// <summary>
    /// 00-3F: Normal (legato off); 40-7F: Legato (on)
    /// </summary>
    LegatoFootswitch = 68,

    Hold2 = 69,


    /// <summary>
    /// CC 00H and 20H - Bank Select, with 00H as MSB and 20H as LSB. must be sent as a pair
    /// </summary>
    BankSelectLsb = 32,

    SC1SoundVariation = 70,
    SC2TimbralHarmonicIntensity = 71,
    SC3ReleaseTime = 72,
    SC4AttackTime = 73,
    SC5Brightness = 74,
    SoundController6 = 75,
    SoundController7 = 76,
    SoundController8 = 77,
    SoundController9 = 78,
    SoundController10 = 79,
    GeneralPurpose5 = 80,
    GeneralPurpose6 = 81,
    GeneralPurpose7 = 82,
    GeneralPurpose8 = 83,
    PortamentoControl = 84,
    Effects1Depth = 91,
    Effects2Depth = 92,
    Effects3Depth = 93,
    Effects4Depth = 94,
    Effects5Depth = 95,
    DataIncrement = 96,
    DataDecrement = 97,

    /// <summary>
    /// Non-Registered Parameter Number (NRPN) - LSB
    /// Used in conjunction with controller 99 (NRPN MSB) to specify a parameter
    /// to be modified by controllers 6 (Data Entry MSB), 38 (Data Entry LSB), 96 (Data Increment)
    /// and 97 (Data Decrement).
    /// The parameter number is a 14-bit value, with controller 99 as the MSB and controller 98 as the LSB.
    /// If a NRPN is selected, it remains selected until another NRPN is selected or
    /// the controllers are reset. NRPN parameters are device-specific.
    /// </summary>
    NrpnLsb = 98,

    /// <summary>
    /// Non-Registered Parameter Number (NRPN) - MSB
    /// Used in conjunction with controller 98 (NRPN LSB) to specify a parameter
    /// to be modified by controllers 6 (Data Entry MSB), 38 (Data Entry LSB), 96 (Data Increment)
    /// and 97 (Data Decrement).
    /// The parameter number is a 14-bit value, with controller 99 as the MSB and controller 98 as the LSB.
    /// If a NRPN is selected, it remains selected until another NRPN is selected or
    /// the controllers are reset. NRPN parameters are device-specific.
    /// </summary>
    NrpnMsb = 99,

    /// <summary>
    /// Registered Parameter Number (RPN) - LSB
    /// Used in conjunction with controller 101 (RPN MSB) to specify a parameter
    /// to be modified by controllers 6 (Data Entry MSB), 38 (Data Entry LSB), 96 (Data Increment)
    /// and 97 (Data Decrement).
    /// The parameter number is a 14-bit value, with controller 101 as the MSB and controller 100 as the LSB.
    /// If an RPN is selected, it remains selected until another RPN is selected or
    /// the controllers are reset. RPN parameters are defined by the MIDI Manufacturers Association.
    ///
    /// 0 = Pitch Bend Sensitivity
    /// 1 = Fine Tuning
    /// 2 = Coarse Tuning
    /// 3 = Tuning Program Select
    /// 4 = Tuning Bank Select
    /// </summary>
    RpnLsb = 100,

    /// <summary>
    /// Registered Parameter Number (RPN) - MSB
    /// Used in conjunction with controller 100 (RPN LSB) to specify a parameter
    /// to be modified by controllers 6 (Data Entry MSB), 38 (Data Entry LSB), 96 (Data Increment)
    /// and 97 (Data Decrement).
    /// The parameter number is a 14-bit value, with controller 101 as the MSB and controller 100 as the LSB.
    /// If an RPN is selected, it remains selected until another RPN is selected or
    /// the controllers are reset. RPN parameters are defined by the MIDI Manufacturers Association.
    /// </summary
    RpnMsb = 101,


    AllSoundOff = 120,
    ResetAllControllers = 121,
    LocalControl = 122,


    /// <summary>
    /// Only recognized on the Basic Channel to which the receiver is assigned
    /// </summary>
    AllNotesOff = 123,

    /// <summary>
    /// Omni mode off, all notes off. Only recognized on the Basic Channel to which the receiver is assigned
    /// </summary>
    OmniModeOff = 124,

    /// <summary>
    /// Omni mode on, all notes off. Only recognized on the Basic Channel to which the receiver is assigned
    /// </summary>
    OmniModeOn = 125,

    /// <summary>
    /// Mono mode on, Poly mode off, all notes off. Only recognized on the Basic Channel to which the receiver is assigned
    /// </summary>
    MonoOnPolyOff = 126,

    /// <summary>
    /// Poly mode on, Mono mode off, all notes off. Only recognized on the Basic Channel to which the receiver is assigned
    /// </summary>
    PolyOnMonoOff = 127
}