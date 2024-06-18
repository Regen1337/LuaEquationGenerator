namespace EquationGenerator;

public static class MathFunctions
{
    private static readonly Random Random = new();
    public static readonly List<MathFunction> Functions =
    [
        new MathFunction("math.sin", 1, [new ArgumentRange(-360, 360)]),
        new MathFunction("math.cos", 1, [new ArgumentRange(-360, 360)]),
        new MathFunction("math.tan", 1, [new ArgumentRange(-89, 89)]),
        new MathFunction("math.asin", 1, [new ArgumentRange(-1, 1)]),
        new MathFunction("math.acos", 1, [new ArgumentRange(-1, 1)]),
        new MathFunction("math.atan", 1, [new ArgumentRange(-10, 10)]),
        new MathFunction("math.abs", 1, [new ArgumentRange(-100000, 100000)]),
        new MathFunction("math.sqrt", 1, [new ArgumentRange(0, 100)]),
        //new MathFunction("math.log", 1, new List<ArgumentRange> { new(1, 100) }),
        new MathFunction("math.exp", 1, [new ArgumentRange(0, 10)]),
        new MathFunction("math.floor", 1, [new ArgumentRange(-10000, 10000)]),
        new MathFunction("math.ceil", 1, [new ArgumentRange(-10000, 10000)]),
        new MathFunction("math.min", 2, [
            new ArgumentRange(-10000, 10000),
            new ArgumentRange(-10000, 10000)
        ]),

        new MathFunction("math.max", 2, [
            new ArgumentRange(-10000, 10000),
            new ArgumentRange(-10000, 10000)
        ]),

        //Bits.lua additions
        //new MathFunction("bits.band", 2, new List<ArgumentRange> { new(0, 32^2-1), new(0, 32^2-1) }),
        //new MathFunction("bits.bor", 2, new List<ArgumentRange> { new(0, 32^2-1), new(0, 32^2-1) }),
        //new MathFunction("bits.bxor", 2, new List<ArgumentRange> { new(0, 32^2-1), new(0, 32^2-1) }),
        //new MathFunction("bits.bnot", 1, new List<ArgumentRange> { new(0, 32^2-1) }),

        //new MathFunction("bits.lshift", 2, [new ArgumentRange(0, 2), new ArgumentRange(0, 2)]),
        //new MathFunction("bits.rshift", 2, [new ArgumentRange(0, 2), new ArgumentRange(0, 2)])
        //new MathFunction("bits.arshift", 2, new List<ArgumentRange> { new(0, 32^2-1), new(0, 32^2-1) }),
        //new MathFunction("bits.rol", 2, new List<ArgumentRange> { new(0, 32^2-1), new(0, 32^2-1) }),
        //new MathFunction("bits.ror", 2, new List<ArgumentRange> { new(0, 32^2-1), new(0, 32^2-1) }),
        //new MathFunction("bits.bswap", 1, new List<ArgumentRange> { new(0, 32^2-1) }),

    ];
    
    public static MathFunction? GetFunction(string name) => Functions.FirstOrDefault(f => f.Name == name);
    public static bool FunctionExists(string name) => Functions.Any(f => f.Name == name);
    public static bool ArgumentCountValid(string name, int argumentCount) => Functions.Any(f => f.Name == name && f.ArgumentCount == argumentCount);
    public static bool ArgumentRangesValid(string name, List<int> arguments)
    {
        var function = GetFunction(name);
        if (function == null) return false;
        if (function.ArgumentRanges.Count != arguments.Count) return false;
        for (var i = 0; i < function.ArgumentRanges.Count; i++)
        {
            if (arguments[i] < function.ArgumentRanges[i].Min || arguments[i] > function.ArgumentRanges[i].Max) return false;
        }
        return true;
    }
    public static bool FunctionValid(string name, int argumentCount, List<int> arguments) => FunctionExists(name) && ArgumentCountValid(name, argumentCount) && ArgumentRangesValid(name, arguments);
    public static bool GetFunctionAndArgumentRanges(string name, out MathFunction? function, out List<ArgumentRange> argumentRanges)
    {
        function = GetFunction(name);
        argumentRanges = function?.ArgumentRanges ?? [];
        return function != null;
    }
    
    // get a random function from the list
    public static MathFunction GetRandomFunction()
    {
        return Functions[Random.Next(Functions.Count)];
    }
}