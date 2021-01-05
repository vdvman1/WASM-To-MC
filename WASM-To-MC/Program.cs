using System;
using System.Diagnostics;
using System.IO;
using WASM_To_MC;
using WASM_To_MC.Parsing.Binary;

if (Arguments.Parse(args) is not Arguments arguments)
{
    return;
}

byte[] bytes;
try
{
    bytes = await File.ReadAllBytesAsync(arguments.Input);
}
catch (IOException e) when (e is DirectoryNotFoundException or FileNotFoundException)
{
    Console.Error.WriteLine($"Input WASM binary could not be found: {arguments.Input}");
    return;
}
catch (Exception e)
{
    Console.Error.WriteLine($"Unable to read input WASM binary: {arguments.Input}");
    Console.WriteLine(e);
    return;
}

Console.WriteLine("Parsing WASM binary");
var module = new WasmFileParser(bytes).Parse();
Console.WriteLine($"Finished parsing WASM binary");
