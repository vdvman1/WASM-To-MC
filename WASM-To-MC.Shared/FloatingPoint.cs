using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WASM_To_MC.Shared
{
    public interface IFloatingPoint<TSelf>
        where TSelf: IFloatingPoint<TSelf>
    {
        /// <summary>
        /// Bitwidth of the underlying value, must be a valid bitwidth of an IEEE 754-2019 floating point number
        /// </summary>
        public byte Bits { get; }

        /// <summary>
        /// Number of bytes in the underlying value
        /// </summary>
        // This is not implemented here because default member implementations are not inherited
        public byte Bytes { get; }

        /// <summary>
        /// Get a value of this type given the provided little endian bytes
        /// </summary>
        /// <param name="bytes">sequence of bytes in little endian order that follow the IEEE 754-2019 spec</param>
        /// <returns>An instance of this with the value specified by <paramref name="bytes"/></returns>
        public TSelf FromBytesLE(IEnumerable<byte> bytes);
    }

    public interface IFloatingPoint<TSelf, TFloat> : IFloatingPoint<TSelf>
        where TSelf: IFloatingPoint<TSelf, TFloat>
    {
        /// <summary>
        /// Underlying storage of this floating point
        /// </summary>
        public TFloat Value { get; }
    }

    public struct Float32 : IFloatingPoint<Float32, float>
    {
        public const byte BitWidth = 32;
        public const byte ByteCount = BitWidth / 8;

        public float Value { get; set; }

        public byte Bits => BitWidth;
        public byte Bytes => ByteCount;

        public Float32(float value) => Value = value;

        public Float32 FromBytesLE(IEnumerable<byte> bytes)
        {
            var arr = bytes.Take(ByteCount).ToArray();
            if(!BitConverter.IsLittleEndian)
            {
                Array.Reverse(arr);
            }
            return new Float32(BitConverter.ToSingle(arr));
        }
    }

    public struct Float64 : IFloatingPoint<Float64, double>
    {
        public const byte BitWidth = 64;
        public const byte ByteCount = BitWidth / 8;

        public double Value { get; set; }

        public byte Bits => BitWidth;
        public byte Bytes => ByteCount;

        public Float64(double value) => Value = value;

        public Float64 FromBytesLE(IEnumerable<byte> bytes)
        {
            var arr = bytes.Take(ByteCount).ToArray();
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(arr);
            }
            return new Float64(BitConverter.ToDouble(arr));
        }
    }
}
