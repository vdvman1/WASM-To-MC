using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WASM_To_MC.Shared;

namespace WASM_To_MC.Parsing
{
    public class WasmFileParser
    {
        private readonly byte[] Bytes;
        internal int Index { get; private set; } = 0;

        public WasmFileParser(byte[] bytes)
        {
            Bytes = bytes;
        }

        /// <summary>
        /// Attempt to read the next byte
        /// </summary>
        /// <param name="b">Byte that was read, left unchanged if <code>false</code> is returned</param>
        /// <returns>Whether a byte was read or not</returns>
        internal bool TryNextByte(out byte b)
        {
            if (Index >= Bytes.Length)
            {
                Index = Bytes.Length;
                b = default;
                return false;
            }

            b = Bytes[Index];
            Index++;
            return true;
        }

        /// <summary>
        /// Read the next byte, throws a <see cref="ParseException"/> if no byte could be read
        /// </summary>
        /// <returns>The byte that was read</returns>
        /// <exception cref="ParseException">No byte could be read</exception>
        internal byte NextByte()
        {
            if (TryNextByte(out byte b))
            {
                return b;
            }

            throw new ParseException("Unexpected end of file");
        }

        /// <summary>
        /// Read a numeric value encoded in LEB128 format
        /// </summary>
        /// <typeparam name="T">The type of numeric value to read</typeparam>
        /// <param name="int">An arbitrary value of the numeric type, used to specify the bitwidth. The actual value is not used</param>
        /// <returns>The read numeric value</returns>
        /// <exception cref="ParseException">The encoded value is invalid for the specified type <typeparamref name="T"/></exception>
        internal T LEB128<T>(T @int)
            where T : struct, IInteger<T>
        {
            const byte bit8 = 1 << 7;

            T val = @int.From(0);
            byte bits = @int.Bits;
            while(bits > 0)
            {
                byte b = NextByte();
                var magnitude = (byte)(b & ~bit8);
                if (bits >= 7 || magnitude <= new UByte(bits).Max.Value)
                {
                    val = val.Or(@int.From(magnitude).LShift((byte)(@int.Bits - bits)));
                    if(b < bit8)
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
                    else if(bits <= 7) // Don't consume more input than the maximum to ensure a proper error message is produced even when at the end of the input
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
    }
}
