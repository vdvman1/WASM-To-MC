using System;
using System.Collections.Generic;
using WASM_To_MC.Shared;

namespace WASM_To_MC.Parsing.Binary
{
    public partial class WasmFileParser
    {

        internal Instruction ParseSimpleBlock(InstructionCode inst)
        {
            var type = ParseBlockType();
            var body = ParseExpression();
            return new Instruction.SimpleBlock(inst, type, body);
        }

        internal Instruction ParseIf()
        {
            var type = ParseBlockType();
            var body = new List<Instruction>();
            var res = ParseInstruction();
            while (res is ParseInstructionResult.Success(var inst))
            {
                try
                {
                    body.Add(inst);
                }
                catch (Exception e)
                {
                    throw new ParseException("Unable to build expression list, see inner exception for details", e);
                }
                res = ParseInstruction();
            }

            return res switch
            {
                ParseInstructionResult.BlockEnd => new Instruction.SimpleBlock(InstructionCode.If, type, body),
                ParseInstructionResult.Else => new Instruction.IfElse(type, body, ParseExpression()),
                _ => throw new ParseException($"Unknown {nameof(ParseInstructionResult)}: {res}"),
            };
        }

        internal Instruction ParseSaturatingInstruction()
        {
            var op = (SaturatingInstructionCode)Parser.LEB128(new UInt(32)).Value;
            return Enum.IsDefined(op) ? new Instruction.Saturating(op) : throw new ParseException($"Unknown saturating instruction type: {op}");
        }

        // Discriminated union pattern
        internal abstract record ParseInstructionResult
        {
            protected ParseInstructionResult() { }

            public record Success(Instruction Inst) : ParseInstructionResult { }
            public record BlockEnd : ParseInstructionResult { }
            public record Else : ParseInstructionResult { }
        }

        internal ParseInstructionResult ParseInstruction()
        {
            var inst = (InstructionCode)Parser.NextByte();
            return inst switch
            {
                InstructionCode.Unreachable
                    or InstructionCode.Nop
                    or InstructionCode.Return
                    or InstructionCode.Drop
                    or InstructionCode.Select
                    or (>= InstructionCode.I32_EqZ and <= InstructionCode.I64_Extend32_S)
                    => new ParseInstructionResult.Success(new Instruction.Basic(inst)),

                >= InstructionCode.Block and < InstructionCode.If
                    => new ParseInstructionResult.Success(ParseSimpleBlock(inst)),

                InstructionCode.If
                    => new ParseInstructionResult.Success(ParseIf()),

                InstructionCode.Br or InstructionCode.BrIf or InstructionCode.Call or (>= InstructionCode.Local_Get and <= InstructionCode.Global_Set)
                    => new ParseInstructionResult.Success(new Instruction.SingleIndex(inst, Parser.LEB128(new UInt(32)).Value)),

                InstructionCode.BrTable
                    => new ParseInstructionResult.Success(new Instruction.BrTable(Vector(() => Parser.LEB128(new UInt(32)).Value), Parser.LEB128(new UInt(32)).Value)),

                InstructionCode.CallIndirect
                    => new ParseInstructionResult.Success(new Instruction.CallIndirect(Parser.LEB128(new UInt(32)).Value, 0)),

                >= InstructionCode.I32_Load and <= InstructionCode.I64_Store32
                    => new ParseInstructionResult.Success(new Instruction.Memory(inst, Parser.LEB128(new UInt(32)).Value, Parser.LEB128(new UInt(32)).Value)),

                InstructionCode.Memory_Size or InstructionCode.Memory_Grow
                    => new ParseInstructionResult.Success(new Instruction.MemTable(inst, 0)),

                InstructionCode.I32_Const
                    => new ParseInstructionResult.Success(new Instruction.Constant(new WasmValue.I32(Parser.LEB128(new SInt(32)).Value))),

                InstructionCode.I64_Const
                    => new ParseInstructionResult.Success(new Instruction.Constant(new WasmValue.I64(Parser.LEB128(new SLong(64)).Value))),

                InstructionCode.F32_Const
                    => new ParseInstructionResult.Success(new Instruction.Constant(new WasmValue.F32(Parser.Float(new Float32()).Value))),

                InstructionCode.F64_Const
                    => new ParseInstructionResult.Success(new Instruction.Constant(new WasmValue.F64(Parser.Float(new Float64()).Value))),

                InstructionCode.Saturating
                    => new ParseInstructionResult.Success(ParseSaturatingInstruction()),

                InstructionCode.End
                    => new ParseInstructionResult.BlockEnd(),

                InstructionCode.Else
                    => new ParseInstructionResult.Else(),

                _ => throw new ParseException($"Unknown opcode: {inst}")
            };
        }

        internal IReadOnlyList<Instruction> ParseExpression()
        {
            var instructions = new List<Instruction>();
            var res = ParseInstruction();
            while(res is ParseInstructionResult.Success(var inst))
            {
                try
                {
                    instructions.Add(inst);
                }
                catch (Exception e)
                {
                    throw new ParseException("Unable to build expression list, see inner exception for details", e);
                }

                res = ParseInstruction();
            }

            return res switch
            {
                ParseInstructionResult.BlockEnd => instructions,
                ParseInstructionResult.Else => throw new ParseException($"Unexpected else instruction"),
                _ => throw new ParseException($"Unknown {nameof(ParseInstructionResult)}: {res}")
            };
        }
    }
}