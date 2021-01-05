using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WASM_To_MC.Shared
{
    public enum WasmValueType : byte
    {
        _Min = 0x7B,
        F64,
        F32,
        I64,
        I32,
        _Max
    }

    // Discriminated union pattern
    public abstract record WasmValue
    {
        public WasmValueType Type { get; init; }

        protected WasmValue(WasmValueType type) => Type = type;

        public void Deconstruct(out WasmValueType type) => type = Type;

        public abstract InstructionCode Instruction { get; }

        public record F64(double Value) : WasmValue(WasmValueType.F64)
        {
            public override InstructionCode Instruction => InstructionCode.F64_Const;
        }

        public record F32(float Value) : WasmValue(WasmValueType.F32)
        {
            public override InstructionCode Instruction => InstructionCode.F32_Const;
        }

        public record I64(long Value) : WasmValue(WasmValueType.I64)
        {
            public override InstructionCode Instruction => InstructionCode.I64_Const;
        }

        public record I32(int Value) : WasmValue(WasmValueType.I32)
        {
            public override InstructionCode Instruction => InstructionCode.I32_Const;
        }
    }
}
