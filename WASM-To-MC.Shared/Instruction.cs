using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WASM_To_MC.Shared
{
    // Discriminated union pattern
    public abstract record Instruction
    {
        private readonly InstructionCode opCode;
        public virtual InstructionCode OpCode
        {
            get => opCode;
            init => opCode = value;
        }

        protected Instruction(InstructionCode opCode) => OpCode = opCode;

        public record Basic(InstructionCode OpCode) : Instruction(OpCode)
        {
            public override InstructionCode OpCode
            {
                get => base.OpCode;
                init
                {
                    Trace.Assert(value is
                        InstructionCode.Unreachable
                        or InstructionCode.Nop
                        or InstructionCode.Return
                        or InstructionCode.Drop
                        or InstructionCode.Select
                        or (>= InstructionCode.I32_EqZ and <= InstructionCode.I64_Extend32_S)
                    );
                    base.OpCode = value;
                }
            }
        }

        public record SimpleBlock(InstructionCode OpCode, BlockType Type, IReadOnlyList<Instruction> Body) : Instruction(OpCode)
        {
            public override InstructionCode OpCode
            {
                get => base.OpCode;
                init
                {
                    Trace.Assert(value is >= InstructionCode.Block and <= InstructionCode.If);
                    base.OpCode = value;
                }
            }
        }

        public record IfElse(BlockType Type, IReadOnlyList<Instruction> TrueBody, IReadOnlyList<Instruction> FalseBody) : Instruction(InstructionCode.If) { }

        public record SingleIndex(InstructionCode OpCode, uint Index) : Instruction(OpCode)
        {
            public override InstructionCode OpCode
            {
                get => base.OpCode;
                init
                {
                    Trace.Assert(value is InstructionCode.Br or InstructionCode.BrIf or InstructionCode.Call or (>= InstructionCode.Local_Get and <= InstructionCode.Global_Set));
                    base.OpCode = value;
                }
            }
        }

        public record BrTable(IReadOnlyList<uint> Cases, uint Default) : Instruction(InstructionCode.BrTable) { }

        public record CallIndirect(uint TypeIndex, byte Table) : Instruction(InstructionCode.CallIndirect) { }

        public record Memory(InstructionCode OpCode, uint Align, uint Offset) : Instruction(OpCode)
        {
            public override InstructionCode OpCode
            {
                get => base.OpCode;
                init
                {
                    Trace.Assert(value is >= InstructionCode.I32_Load and <= InstructionCode.I64_Store32);
                    base.OpCode = value;
                }
            }
        }

        public record MemTable(InstructionCode OpCode, byte Table) : Instruction(OpCode)
        {
            public override InstructionCode OpCode
            {
                get => base.OpCode;
                init
                {
                    Trace.Assert(value is InstructionCode.Memory_Size or InstructionCode.Memory_Grow);
                    base.OpCode = value;
                }
            }
        }

        public record Constant(WasmValue Value) : Instruction(Value.Instruction) { }

        public record Saturating(SaturatingInstructionCode SubOp) : Instruction(InstructionCode.Saturating) { }
    }

    public enum InstructionCode : byte
    {
        Unreachable,
        Nop,
        Block,
        Loop,
        If,
        Else,
        End = 0x0B,
        Br,
        BrIf,
        BrTable,
        Return,
        Call,
        CallIndirect,

        Drop = 0x1A,
        Select,

        Local_Get = 0x20,
        Local_Set,
        Local_Tee,
        Global_Get,
        Global_Set,

        I32_Load = 0x28,
        I64_Load,
        F32_Load,
        F64_Load,
        I32_Load8_S,
        I32_Load8_U,
        I32_Load16_S,
        I32_Load16_U,
        I64_Load8_S,
        I64_Load8_U,
        I64_Load16_S,
        I64_Load16_U,
        I64_Load32_S,
        I64_Load32_U,
        I32_Store,
        I64_Store,
        F32_Store,
        F64_Store,
        I32_Store8,
        I32_Store16,
        I64_Store8,
        I64_Store16,
        I64_Store32,
        Memory_Size,
        Memory_Grow,

        I32_Const,
        I64_Const,
        F32_Const,
        F64_Const,

        I32_EqZ,
        I32_Eq,
        I32_NE,
        I32_LT_S,
        I32_LT_U,
        I32_GT_S,
        I32_GT_U,
        I32_LE_S,
        I32_LE_U,
        I32_GE_S,
        I32_GE_U,

        I64_EqZ,
        I64_Eq,
        I64_NE,
        I64_LT_S,
        I64_LT_U,
        I64_GT_S,
        I64_GT_U,
        I64_LE_S,
        I64_LE_U,
        I64_GE_S,
        I64_GE_U,

        F32_Eq,
        F32_NE,
        F32_LT,
        F32_GT,
        F32_LE,
        F32_GE,

        F64_Eq,
        F64_NE,
        F64_LT,
        F64_GT,
        F64_LE,
        F64_GE,

        I32_CLZ,
        I32_CTZ,
        I32_PopCount,
        I32_Add,
        I32_Sub,
        I32_Mul,
        I32_Div_S,
        I32_Div_U,
        I32_Rem_S,
        I32_Rem_U,
        I32_And,
        I32_Or,
        I32_XOr,
        I32_ShL,
        I32_ShR_S,
        I32_ShR_U,
        I32_RotL,
        I32_RotR,

        I64_CLZ,
        I64_CTZ,
        I64_PopCount,
        I64_Add,
        I64_Sub,
        I64_Mul,
        I64_Div_S,
        I64_Div_U,
        I64_Rem_S,
        I64_Rem_U,
        I64_And,
        I64_Or,
        I64_XOr,
        I64_ShL,
        I64_ShR_S,
        I64_ShR_U,
        I64_RotL,
        I64_RotR,

        F32_Abs,
        F32_Neg,
        F32_Ceil,
        F32_Floor,
        F32_Trunc,
        F32_Nearest,
        F32_Sqrt,
        F32_Add,
        F32_Sub,
        F32_Mul,
        F32_Div,
        F32_Min,
        F32_Max,
        F32_CopySign,

        F64_Abs,
        F64_Neg,
        F64_Ceil,
        F64_Floor,
        F64_Trunc,
        F64_Nearest,
        F64_Sqrt,
        F64_Add,
        F64_Sub,
        F64_Mul,
        F64_Div,
        F64_Min,
        F64_Max,
        F64_CopySign,

        I32_Wrap_I64,
        I32_Trunc_F32_S,
        I32_Trunc_F32_U,
        I32_Trunc_F64_S,
        I32_Trunc_F64_U,
        I64_Extend_I32_S,
        I64_Extend_I32_U,
        I64_Trunc_F32_S,
        I64_Trunc_F32_U,
        I64_Trunc_F64_S,
        I64_Trunc_F64_U,
        F32_Convert_I32_S,
        F32_Convert_I32_U,
        F32_Convert_I64_S,
        F32_Convert_I64_U,
        F32_Demote_F64,
        F64_Convert_I32_S,
        F64_Convert_I32_U,
        F64_Convert_I64_S,
        F64_Convert_I64_U,
        F64_Promote_F32,
        I32_Reinterpret_F32,
        I64_Reinterpret_F64,
        F32_Reinterpret_I32,
        F64_Reinterpret_I64,

        I32_Extend8_S,
        I32_Extend16_S,
        I64_Extend8_S,
        I64_Extend16_S,
        I64_Extend32_S,

        Saturating
    }

    public enum SaturatingInstructionCode : uint
    {
        I32_Trunc_Sat_F32_S,
        I32_Trunc_Sat_F32_U,
        I32_Trunc_Sat_F64_S,
        I32_Trunc_Sat_F64_U,
        I64_Trunc_Sat_F32_S,
        I64_Trunc_Sat_F32_U,
        I64_Trunc_Sat_F64_S,
        I64_Trunc_Sat_F64_U
    }
}
