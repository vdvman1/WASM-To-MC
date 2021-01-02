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
        where TSelf: IInteger<TSelf>
    {
        /// <summary>
        /// The actual bit width of the value. Must be less than or equal to the bit width of the underlying storage
        /// </summary>
        public byte Bits { get; }

        /// <summary>
        /// The maximum value that can be stored by an integer of this type, dependent on <see cref="Bits"/>
        /// </summary>
        public TSelf Max { get; }

        /// <summary>
        /// Get a value of this type with a matching bit width using the value of <paramref name="b"/>, truncating any extra bits
        /// </summary>
        /// <param name="b">Byte to convert to this type</param>
        /// <returns>A value of this type with a matching bit width and a value of <paramref name="b"/>, truncating any extra bits</returns>
        public TSelf From(byte b);

        /// <summary>
        /// Calculate the bitwise OR of this and <paramref name="other"/>, keeping the bit width of this and truncating any extra bits
        /// </summary>
        /// <param name="other">Value to bitwise OR with this</param>
        /// <returns>bitwise OR of this and <paramref name="other"/>, keeping the bit width of this and truncating any extra bits</returns>
        public TSelf Or(TSelf other);

        /// <summary>
        /// Shift this to the left by <paramref name="amount"/> bits, dropping any extra bits
        /// </summary>
        /// <param name="amount">Number of bits to shift by</param>
        /// <returns>The value of this shifted to the left by <paramref name="amount"/> bits, dropping any extra bits</returns>
        public TSelf LShift(byte amount);

        /// <summary>
        /// Shift this to the right by <paramref name="amount"/> bits, dropping any extra bits
        /// </summary>
        /// <param name="amount">Number of bits to shift by</param>
        /// <returns>The value of this shifted to the right by <paramref name="amount"/> bits, dropping any extra bits</returns>
        public TSelf RShift(byte amount);

        /// <summary>
        /// Get the byte with bits from <code><paramref name="i"/> * 8</code> to <code><paramref name="i"/> * 8 + 7</code>
        /// </summary>
        /// <param name="i">Which byte to get</param>
        /// <returns>The byte at index <paramref name="i"/>, or 0 if outside the range of this</returns>
        public byte this[byte i] { get; }
    }

    public interface IInteger<TSelf, TInt> : IInteger<TSelf>
        where TSelf : IInteger<TSelf, TInt>
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

        public byte this[byte i] => i == 0 ? (byte)value : 0;

        public int CompareTo(SByte other) => Value.CompareTo(other.Value);

        public SByte From(byte b) => new(Bits, (sbyte)b);

        /// <summary>
        /// Keep only the lower <paramref name="bits"/> bits of <paramref name="val"/> and cast to <see cref="sbyte"/>
        /// Performs sign extension with the highest used bit
        /// </summary>
        /// <param name="val">Value to truncate</param>
        /// <param name="bits">Number of bits to keep</param>
        /// <returns>The truncated value, sign extended</returns>
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

        public byte this[byte i] => i == 0 ? (byte)value : 0;

        public int CompareTo(SShort other) => Value.CompareTo(other.Value);

        public SShort From(byte b) => new(Bits, b);

        /// <summary>
        /// Keep only the lower <paramref name="bits"/> bits of <paramref name="val"/> and cast to <see cref="short"/>
        /// Performs sign extension with the highest used bit
        /// </summary>
        /// <param name="val">Value to truncate</param>
        /// <param name="bits">Number of bits to keep</param>
        /// <returns>The truncated value, sign extended</returns>
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

    public struct UInt : IInteger<UInt, uint>
    {
        public byte Bits { get; }

        private static uint MaxValue(byte bits) => (uint)((1L << bits) - 1);

        public UInt Max => new(Bits, value: MaxValue(Bits));

        private uint value;
        public uint Value
        {
            get => value;
            set => this.value = Math.Min(value, Max.Value);
        }

        public UInt(byte bits, uint value = 0)
        {
            Bits = Math.Min(bits, (byte)32);
            this.value = Math.Min(value, MaxValue(bits));
        }

        public byte this[byte i] => (byte)(value >> (i * 8));

        public int CompareTo(UInt other) => Value.CompareTo(other.Value);

        public UInt From(byte b) => new(Bits, b);

        public UInt LShift(byte amount) => new(Bits, value: (Value << amount) & Max.Value);

        public UInt RShift(byte amount) => new(Bits, value: Value >> amount);

        public UInt Or(UInt other) => new(Bits, value: (Value | other.Value) & Max.Value);
    }
}
