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

        internal byte NextByte()
        {
            if (TryNextByte(out byte b))
            {
                return b;
            }

            throw new ParseException("Unexpected end of file");
        }

        internal T ULEB128<T>(T @int)
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
                        return val;
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
