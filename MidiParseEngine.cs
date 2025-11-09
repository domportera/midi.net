using System.Diagnostics.CodeAnalysis;
using System.Text;
using Commons.Music.Midi;
using Midi.Net.MidiUtilityStructs;
using MidiEvent = Midi.Net.MidiUtilityStructs.MidiEvent;

namespace Midi.Net;

public sealed class MidiParseEngine
{
    private readonly StringBuilder _midiEventStringBuilder = new();
    private MidiEvent[] _midiEvents = [];
    private byte[] _buffer = [];
    private int _bufferedCount;
    private MidiStatus? _inputStatus;

    public bool ProcessMessageReceived(MidiReceivedEventArgs e, [NotNullWhen(true)] out ReadOnlyMemory<MidiEvent>? events)
    {
        var dataSpan = new Span<byte>(e.Data, e.Start, e.Length);
        if (dataSpan.Length == 0)
        {
            events = null;
            return false;
        }
        
        EnsureBufferLength(dataSpan);

        if (dataSpan.Length + _bufferedCount < 2)
        {
            // not enough data to interpret
            // append to our buffer and wait for more data
            dataSpan.CopyTo(_buffer.AsSpan(_bufferedCount));
            _bufferedCount += dataSpan.Length;
            events = null;
            return false;
        }

        if (_bufferedCount > 0)
        {
            // we have data that has been buffered - we need too append to that
            // and then read from that
            dataSpan.CopyTo(_buffer.AsSpan(_bufferedCount));
            dataSpan = _buffer.AsSpan(0, _bufferedCount + dataSpan.Length);
            _bufferedCount = 0;
        }

        if (_midiEvents.Length < dataSpan.Length)
        {
            // ensure we have ample space for midi events
            _midiEvents = new MidiEvent[dataSpan.Length];
        }
            
        var eventCount = MidiParser.Interpret(ref _inputStatus, dataSpan, _midiEvents, _midiEventStringBuilder, out var bytesUsed);

        // copy any unused data to our buffer
        _bufferedCount = dataSpan.Length - bytesUsed;
        if (_bufferedCount > 0)
        {
            if(_buffer.Length < _bufferedCount)
            {
                Array.Resize(ref _buffer, _bufferedCount);
            }
            
            dataSpan[bytesUsed..].CopyTo(_buffer);
        }
        
        // log any errors from parsing
        if (_midiEventStringBuilder.Length > 0)
        {
            _ = Console.Out.WriteLineAsync(_midiEventStringBuilder.ToString());
            _midiEventStringBuilder.Clear();
        }

        if (eventCount == 0)
        {
            events = null;
            return false;
        }
        
        // raise midi receive events
        events = new ReadOnlyMemory<MidiEvent>(_midiEvents, 0, eventCount);
        return true;
    }

    private void EnsureBufferLength(Span<byte> dataSpan)
    {
        if (dataSpan.Length + _bufferedCount >= _buffer.Length)
        {
            // increase buffer size
            Array.Resize(ref _buffer, dataSpan.Length + _bufferedCount);
        }
    }
}