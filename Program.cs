using EquationGenerator;

MathExpressionGenerator.LoadLuaFunctions();
await MathExpressionGenerator.Run("420", 5, 30, 15);
Console.WriteLine("Press any key to exit...");
Console.ReadKey();