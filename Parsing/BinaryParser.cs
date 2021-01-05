using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WASM_To_MC.Shared;

namespace WASM_To_MC.Parsing
{
    public abstract class BinaryParser
    {
        public abstract int Index { get; protected set; }
        public readonly int MaxIndex;
        public abstract byte this[int i] { get; }
        protected abstract string OutOfBoundsMsg { get; }

        protected BinaryParser(int maxIndex) => MaxIndex = maxIndex;

        /// <summary>
        /// Attempt to read the next byte
        /// </summary>
        /// <param name="b">Byte that was read, left unchanged if <code>false</code> is returned</param>
        /// <returns>Whether a byte was read or not</returns>
        public bool TryNextByte(out byte b)
        {
            if (Index >= MaxIndex)
            {
                Index = MaxIndex;
                b = default;
                return false;
            }

            b = this[Index];
            Index++;
            return true;
        }

        /// <summary>
        /// Read the next byte, throws a <see cref="ParseException"/> if no byte could be read
        /// </summary>
        /// <returns>The byte that was read</returns>
        /// <exception cref="ParseException">No byte could be read</exception>
        public byte NextByte() => TryNextByte(out byte b) ? b : throw new ParseException(OutOfBoundsMsg);

        private byte NextByteUnchecked()
        {
            var b = this[Index];
            Index++;
            return b;
        }

        public byte[] NextBytes(int count)
        {
            if(Index + count > MaxIndex)
            {
                throw new ParseException(OutOfBoundsMsg);
            }

            var arr = new byte[count];
            for (int i = 0; i < count; i++)
            {
                arr[i] = NextByteUnchecked();
            }
            return arr;
        }

        /// <summary>
        /// Read a numeric value encoded in LEB128 format
        /// </summary>
        /// <typeparam name="T">The type of numeric value to read</typeparam>
        /// <param name="int">An arbitrary value of the numeric type, used to specify the bitwidth. The actual value is not used</param>
        /// <returns>The read numeric value</returns>
        /// <exception cref="ParseException">The encoded value is invalid for the specified type <typeparamref name="T"/></exception>
        public T LEB128<T>(T @int)
            where T : struct, IInteger<T>
        {
            const byte bit8 = 1 << 7;
            const byte bit7 = 1 << 6;

            T val = @int.From(0);
            byte bits = @int.Bits;
            while (bits > 0)
            {
                byte b = NextByte();
                var magnitude = (byte)(b & ~bit8);
                if (bits >= 7 || (@int.Signed && magnitude >= bit7 ? magnitude >= (bit7 - (1 << (bits - 1))) : magnitude <= new UByte(bits).Max.Value))
                {
                    val = val.Or(@int.From(magnitude).LShift((byte)(@int.Bits - bits)));
                    if (b < bit8)
                    {
                        if (bits <= 7)
                        {
                            return val;
                        }
                        else
                        {
                            bits -= 7;
                            return val.LShift(bits).RShift(bits); // Sign extend if signed
                        }
                    }
                    else if (bits <= 7) // Don't consume more input than the maximum to ensure a proper error message is produced even when at the end of the input
                    {
                        // TODO: Print value
                        throw new ParseException($"Value too large for u{@int.Bits}");
                    }
                    bits -= 7;
                }
                else
                {
                    // TODO: Print value
                    throw new ParseException($"Value too large for u{@int.Bits}");
                }
            }

            return val;
        }

        internal T Float<T>(T @float)
            where T : IFloatingPoint<T>
            => @float.FromBytesLE(NextBytes(@float.Bytes));

        public bool TryMakeChildParser(int length, [NotNullWhen(true)] out BinaryParser? parser)
        {
            int max = Index + length;
            if(max > MaxIndex)
            {
                parser = default;
                return false;
            }

            parser = new SubParser(this, max);
            return true;
        }

        public static BinaryParser From(byte[] bytes) => new RootParser(bytes, 0, bytes.Length);

        public class SubParser : BinaryParser
        {
            private readonly BinaryParser Parent;

            public SubParser(BinaryParser parent, int max)
                : base(Math.Min(parent.MaxIndex, max))
                => Parent = parent;

            public override byte this[int i] => Parent[i];

            public override int Index
            {
                get => Parent.Index;
                protected set => Parent.Index = value;
            }

            protected override string OutOfBoundsMsg => "Listed size does not match actual parsed size";
        }

        public class RootParser : BinaryParser
        {
            private readonly byte[] Bytes;
            private int index;

            public RootParser(byte[] bytes, int start, int length) : base(Math.Min(bytes.Length, start + length))
            {
                Bytes = bytes;
                index = start;
            }

            public override byte this[int i] => Bytes[i];

            public override int Index
            {
                get => index;
                protected set => index = value;
            }

            protected override string OutOfBoundsMsg => "Unexpected end of file";
        }
    }
}
