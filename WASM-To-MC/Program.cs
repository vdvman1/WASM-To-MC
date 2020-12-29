using System;
using System.Diagnostics;
using System.IO;
using WASM_To_MC;

if(Arguments.Parse(args) is not Arguments arguments)
{
    return;
}