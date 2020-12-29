using System;
using System.Diagnostics;
using System.IO;
using WASM_To_MC;

if(Arguments.Parse(args) is not Arguments arguments)
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