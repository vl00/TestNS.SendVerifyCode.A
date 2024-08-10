using System;

partial class Program
{
    static Type _Startup_Type;
    static Type Startup_Type => _Startup_Type;

    [System.Runtime.CompilerServices.ModuleInitializer()]
    internal static void OnModuleInitialize()
    {
        _Startup_Type = (
        typeof(TestNS.Program_test1)

        //typeof(Program)
        );
    }
    
}