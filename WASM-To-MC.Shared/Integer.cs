using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace WASM_To_MC.Shared
{
    public interface IInteger<TSelf> : IComparable<TSelf>
        where TSelf: struct, IInteger<TSelf>
    {
        public byte Bits { get; }
        public TSelf Max { get; }
        public byte UsedBits { get; }

        public TSelf From(byte b);

        public TSelf Or(TSelf other);

        public TSelf LShift(byte amount);

        public TSelf RShift(byte amount);

        public byte this[byte i] { get; }
    }

    public interface IInteger<TSelf, TInt> : IInteger<TSelf>
        where TSelf : struct, IInteger<TSelf, TInt>
        where TInt : struct
    {
        public TInt Value { get; }
    }

    public struct UByte : IInteger<UByte, byte>
    {
        public byte Bits { get; }

        private static byte MaxValue(byte bits) => (byte)((1 << bits) - 1);

        public UByte Max => new(Bits, value: MaxValue(Bits));

        private byte value;
        public byte Value
        {
            get => value;
            set => this.value = Math.Min(value, Max.Value);
        }

        public UByte(byte bits, byte value = 0)
        {
            Bits = Math.Min(bits, (byte)8);
            this.value = Math.Min(value, MaxValue(bits));
        }

        public byte UsedBits => (byte)(8 - BitOperations.LeadingZeroCount(Value));

        public byte this[byte i] => i == 0 ? value : 0;

        public int CompareTo(UByte other) => Value.CompareTo(other.Value);

        public UByte From(byte b) => new(Bits, b);

        public UByte LShift(byte amount) => new(Bits, value: (byte)((Value << amount) & Max.Value));

        public UByte RShift(byte amount) => new(Bits, value: (byte)(Value >> amount));

        public UByte Or(UByte other) => new(Bits, value: (byte)((Value | other.Value) & Max.Value));
    }

    public struct SByte : IInteger<SByte, sbyte>
    {
        public byte Bits { get; }

        private static sbyte MaxValue(byte bits) => (sbyte)((1 << (bits - 1)) - 1);

        public SByte Max => new(Bits, value: MaxValue(Bits));

        private sbyte value;
        public sbyte Value
        {
            get => value;
            set => this.value = DiscardExcessBits(value, Bits);
        }

        public SByte(byte bits, sbyte value = 0)
        {
            Bits = Math.Min(bits, (byte)8);
            this.value = DiscardExcessBits(value, bits);
        }

        public byte UsedBits => (byte)(8 - BitOperations.LeadingZeroCount((uint)Value));

        public byte this[byte i] => i == 0 ? (byte)value : 0;

        public int CompareTo(SByte other) => Value.CompareTo(other.Value);

        public SByte From(byte b) => new(Bits, (sbyte)b);

        private static sbyte DiscardExcessBits(int val, byte bits)
        {
            var max = MaxValue(bits);
            var newVal = val & max;
            // If highest bit set, sign extend
            return (val & (1 << bits)) == 0 ? (sbyte)newVal : (sbyte)(newVal | ~max);
        }

        public SByte LShift(byte amount) => new(Bits, value: DiscardExcessBits(Value << amount, Bits));

        public SByte RShift(byte amount) => new(Bits, value: (sbyte)(Value >> amount));

        public SByte Or(SByte other) => new(Bits, value: DiscardExcessBits(Value | other.Value, Bits));
    }

    public struct UShort : IInteger<UShort, ushort>
    {
        public byte Bits { get; }

        private static ushort MaxValue(byte bits) => (ushort)((1 << bits) - 1);

        public UShort Max => new(Bits, value: MaxValue(Bits));

        private ushort value;
        public ushort Value
        {
            get => value;
            set => this.value = Math.Min(value, Max.Value);
        }

        public UShort(byte bits, ushort value = 0)
        {
            Bits = Math.Min(bits, (byte)16);
            this.value = Math.Min(value, MaxValue(bits));
        }

        public byte UsedBits => (byte)(16 - BitOperations.LeadingZeroCount(Value));

        public byte this[byte i] => (byte)(value >> (i * 8));

        public int CompareTo(UShort other) => Value.CompareTo(other.Value);

        public UShort From(byte b) => new(Bits, b);

        public UShort LShift(byte amount) => new(Bits, value: (ushort)((Value << amount) & Max.Value));

        public UShort RShift(byte amount) => new(Bits, value: (ushort)(Value >> amount));

        public UShort Or(UShort other) => new(Bits, value: (ushort)((Value | other.Value) & Max.Value));
    }

    public struct SShort : IInteger<SShort, short>
    {
        public byte Bits { get; }

        private static short MaxValue(byte bits) => (short)((1 << (bits - 1)) - 1);

        public SShort Max => new(Bits, value: MaxValue(Bits));

        private short value;
        public short Value
        {
            get => value;
            set => this.value = DiscardExcessBits(value, Bits);
        }

        public SShort(byte bits, short value = 0)
        {
            Bits = Math.Min(bits, (byte)16);
            this.value = DiscardExcessBits(value, bits);
        }

        public byte UsedBits => (byte)(16 - BitOperations.LeadingZeroCount((uint)Value));

        public byte this[byte i] => i == 0 ? (byte)value : 0;

        public int CompareTo(SShort other) => Value.CompareTo(other.Value);

        public SShort From(byte b) => new(Bits, b);

        private static short DiscardExcessBits(int val, byte bits)
        {
            var max = MaxValue(bits);
            var newVal = val & max;
            // If highest bit set, sign extend
            return (val & (1 << (bits - 1))) == 0 ? (short)newVal : (short)(newVal | ~max);
        }

        public SShort LShift(byte amount) => new(Bits, value: DiscardExcessBits(Value << amount, Bits));

        public SShort RShift(byte amount) => new(Bits, value: (short)(Value >> amount));

        public SShort Or(SShort other) => new(Bits, value: DiscardExcessBits(Value | other.Value, Bits));
    }
}
