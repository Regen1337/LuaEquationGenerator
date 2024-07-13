using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using NLua;
using static System.Threading.CancellationToken;

namespace EquationGenerator;

// ReSharper disable once ClassNeverInstantiated.Global
public class MathExpressionGenerator : IDisposable
{
    private static Lua Lua => new();
    private static readonly Random Random = new();
    private static readonly ConcurrentDictionary<string, double> EquationCache = new();
    
    private static readonly string[] LogicalOperators = { " and ", " or " };
    private static readonly double[] LogicalOperatorProbabilities = { 0.7, 0.3 };
    
    private static readonly string[] BooleanOperators = { "not", "false", "true", "nil" };
    private static readonly double[] BooleanOperatorProbabilities = { 0.2, 0.1, 0.1, 0.1 };
    
    private static readonly string[] Operators = { " ^ ", " % ", " + ", " - ", " * ", " / " };
    private static readonly double[] OperatorProbabilities = { 0.1, 0.1, 0.2, 0.2, 0.2, 0.2 };
    
    private static readonly Regex OperatorsRegex = new(@"[^a-zA-Z0-9]", RegexOptions.Compiled);
    private static readonly Regex FunctionsRegex = new(@"[a-zA-Z]+\.[a-zA-Z]+", RegexOptions.Compiled);

    void IDisposable.Dispose()
    {
        GC.SuppressFinalize(this);
    }
    
    public static async Task Run(string equation = "1", int count = 1, int targetComplexity = 1, int recursionDepth = 1, CancellationToken cancellationToken = default)
    {
        var solution = Evaluate(equation);
        await GenerateExpressionsAsync(solution, count, targetComplexity, recursionDepth, None);
    }
    
    public static void LoadLuaFunctions()
    {
        Lua.DoString("""
                      local a=false;bits={};local b=2^32;function bits.bnot(c)c=c%b;return b-1-c end;function bits.band(c,d)local e=0;local f=1;while c>0 and d>0 do local g,h=c%2,d%2;if g==1 and h==1 then e=e+f end;f=f*2;c=math.floor(c/2)d=math.floor(d/2)end;return e end;function bits.bor(c,d)local e=0;local f=1;while c>0 or d>0 do local g,h=c%2,d%2;if g==1 or h==1 then e=e+f end;f=f*2;c=math.floor(c/2)d=math.floor(d/2)end;return e end;function bits.bxor(c,d)local e=0;local f=1;while c>0 or d>0 do local g,h=c%2,d%2;if g~=h then e=e+f end;f=f*2;c=math.floor(c/2)d=math.floor(d/2)end;return e end;function bits.lshift(c,i)return c*2^i%b end;function bits.rshift(c,i)return math.floor(c/2^i)end;function bits.arshift(c,i)c=c%b;if i>=0 then if i>31 then return c>=0x80000000 and b-1 or 0 else local j=math.floor(c/2^i)if c>=0x80000000 then j=j+(2^i-1)*2^(32-i)end;return j end else return math.floor(c*2^-i)end end;function bits.rol(c,i)i=i%32;return bits.bor(bits.lshift(c,i),bits.rshift(c,32-i))end;function bits.ror(c,i)i=i%32;return bits.bor(bits.rshift(c,i),bits.lshift(c,32-i))end;function bits.btest(c,d)return bits.band(c,d)~=0 end;function bits.extract(c,k,l)local m=bits.lshift(bits.lshift(1,l)-1,k)return bits.rshift(bits.band(c,m),k)end;function bits.replace(c,n,k,l)local m=bits.lshift(bits.lshift(1,l)-1,k)n=bits.lshift(bits.band(n,bits.lshift(1,l)-1),k)return bits.band(c,bits.bnot(m))+bits.band(n,m)end;function bits.tohex(c,o)local p="0123456789abcdef"local e=""if o==nil then o=8 end;for q=o-1,0,-1 do local r=bits.band(bits.rshift(c,q*4),0xf)e=e..string.sub(p,r+1,r+1)end;return tonumber(e,16)end
                      """); // doesn't seem to work? can't test my bits library currently!!!
    }
    private static double Evaluate(string expression)
    {
        if (EquationCache.TryGetValue(expression, out var cachedResult))
        {
            return cachedResult;
        }

        try
        {
            var result = Lua.DoString($"return {expression}")[0];
            var evaluatedResult = result switch
            {
                double d => double.IsInfinity(d) || double.IsNaN(d) ? double.NaN : d,
                long l => l,
                _ => double.NaN
            };

            // Store the result in the cache
            EquationCache[expression] = evaluatedResult;

            return evaluatedResult;
        }
        catch (NLua.Exceptions.LuaScriptException)
        {
            // Handle Lua script exceptions
            EquationCache[expression] = double.NaN;
            return double.NaN;
        }
        catch (InvalidOperationException)
        {
            // Handle unsupported result types
            EquationCache[expression] = double.NaN;
            return double.NaN;
        }
        catch
        {
            // Handle other exceptions
            EquationCache[expression] = double.NaN;
            return double.NaN;
        }
    }

    private static List<double> EvaluateBatch(IReadOnlyCollection<string> expressions)
    {
        var results = new List<double>(expressions.Count);
        results.AddRange(expressions.Select(Evaluate));
        return results;
    }
    
    private static string ExtractSubexpression(string expression)
    {
        var matches = OperatorsRegex.Matches(expression);
        if (matches.Count == 0)
        {
            return expression;
        }

        var index = Random.Next(matches.Count);
        var match = matches[index];
        var subExpressionBuilder = new StringBuilder();
        var depth = 0;
        var i = match.Index;
        while (i < expression.Length)
        {
            var c = expression[i];
            if (c == '(')
            {
                depth++;
            }
            else if (c == ')')
            {
                depth--;
            }

            subExpressionBuilder.Append(c);

            if (depth == 0)
            {
                break;
            }

            i++;
        }

        return subExpressionBuilder.ToString();
    }

    private static string ApplyFunctionManipulation(string expression)
    {
        var functionMatch = FunctionsRegex.Match(expression);
        if (!functionMatch.Success)
        {
            return expression;
        }

        var functionName = functionMatch.Value;
        if (MathFunctions.Functions.All(f => f.Name != functionName))
        {
            return expression;
        }

        if (functionMatch.Index + functionMatch.Length >= 0 && functionMatch.Index + functionMatch.Length <= expression.Length)
        {
            var innerExpression = ExtractSubexpression(expression[(functionMatch.Index + functionMatch.Length)..]);
            if (string.IsNullOrEmpty(innerExpression))
            {
                return expression;
            }
            
            var manipulatedExpression = $"{functionName}({innerExpression}";

            var argumentCount = MathFunctions.Functions.First(f => f.Name == functionName).ArgumentCount;
            for (var i = 0; i < argumentCount - 1; i++)
            {
                var range = MathFunctions.Functions.First(f => f.Name == functionName).ArgumentRanges[i];
                var argument = Random.Next(range.Min, range.Max).ToString();
                manipulatedExpression += $",{argument}";
            }

            manipulatedExpression += ")";
            return expression.Replace(functionMatch.Value + "(" + innerExpression + ")", manipulatedExpression);
        }
        
        return expression;
    }

    private static string ApplyExpressionManipulation(string expression)
    {
        var manipulationType = Random.Next(4);
        switch (manipulationType)
        {
            case 0:
                var subExpression = ExtractSubexpression(expression);
                if (string.IsNullOrEmpty(subExpression))
                {
                    return expression;
                }
                var manipulatedSubExpression = ApplyFunctionManipulation(subExpression);
                expression = expression.Replace(subExpression, manipulatedSubExpression);
                break;
            case 1:
                expression = ApplyFunctionManipulation(expression);
                break;
            case 2:
                var @operator = Operators[Random.Next(Operators.Length)];
                var rightExpression = $"({GenerateExpression(1, 0)})";
                expression = $"({expression}){@operator}{rightExpression}";
                break;
        }

        return expression;
    }
    
    private static string GenerateParentheses(int depth, double target, string expression, bool shouldBuildParentheses = true, int parenthesesCount = 1)
    {
        var expressionBuilder = new StringBuilder();

        for (var i = 0; i < parenthesesCount; i++)
        {
            if (shouldBuildParentheses)
            {
                expressionBuilder.Append('(');
            }
        }
        
        expressionBuilder.Append(expression);
        
        for (var i = 0; i < parenthesesCount; i++)
        {
            if (shouldBuildParentheses)
            {
                expressionBuilder.Append(')');
            }
        }

        return expressionBuilder.ToString();
    }
    
    private static string GenerateExpression(int depth, double target)
    {
        if (depth < 1)
        {
            var value = Random.Next(-1000, 1000);
            while (value == (int)target)
            {
                value = Random.Next(-10000, 10000);
            }
            return value.ToString();
        }

        var expressionBuilder = new StringBuilder();
        var shouldBuildParentheses = Random.NextDouble() < 0.3;
        var parenthesesCount = Random.Next(1, 6);

        // Operator distribution
        var operatorIndex = SelectIndexBasedOnProbabilities(OperatorProbabilities);
        var logicalOperatorIndex = SelectIndexBasedOnProbabilities(LogicalOperatorProbabilities);
        var @operator = Operators[operatorIndex];
        var @logicalOperator = LogicalOperators[logicalOperatorIndex];
        
        // Generate left expression
        var randomCase = Random.Next(0, 5);
        switch (randomCase)
        {
            case 0:
                expressionBuilder.Append(GenerateParentheses(depth - 1, target, GenerateExpression(depth - 1, target), shouldBuildParentheses, parenthesesCount));
                break;
            case 1:
                expressionBuilder.Append(GenerateParentheses(depth - 1, target, $"({GenerateExpression(depth - 1, target)})", shouldBuildParentheses, parenthesesCount));
                break;
            case 2:
                expressionBuilder.Append(GenerateParentheses(depth - 1, target, $"{MathFunctions.GetRandomFunction().Name}({GenerateExpression(depth - 1, target)})", shouldBuildParentheses, parenthesesCount));
                break;
            case 3:
                // Primitive Roots and Modular Arithmetic
                var p = Random.Next(1, 10000);
                var g = Random.Next(1, p);
                var x = Random.Next(1, p);
                var y = Random.Next(1, p);
                expressionBuilder.Append($"(({g} ^ ({x} + {y})) % {p})");
                break;
            case 4:
                // Summations (Lua-compatible)
                var n = Random.Next(0, 25);
                var cache_variable = GenerateRandomVariableName();
                var sum_variable = GenerateRandomVariableName();
                var a = string.Join(", ", Enumerable.Range(0, n).Select(_ => Random.Next(-100, 100)));
                expressionBuilder.Append($"(function();local {cache_variable}={{ {a} }}; local {sum_variable}=0; for i=1, {n} do;{sum_variable}={sum_variable}+{cache_variable}[i];end;return sum;end)()");
                break;
        }
        
        // build a direct boolean operator (not including <not>)
        if (Random.NextDouble() < 0.1)
        {
            expressionBuilder.Append(logicalOperator);
            switch (logicalOperatorIndex)
            {
                case 0:
                    break;
                case 1:
                    expressionBuilder.Append(BooleanOperators[Random.Next(1, BooleanOperators.Length)]);
                    
                    for (var i = 0; i < Random.Next(1, 5); i++)
                    {
                        expressionBuilder.Append(@logicalOperator);
                        
                        switch (Random.Next(0, 3))
                        {
                            case 0:
                                expressionBuilder.Append(GenerateParentheses(depth - 1, target, GenerateExpression(depth - 1, target), shouldBuildParentheses, parenthesesCount + 6));
                                break;
                            case 1:
                                expressionBuilder.Append(GenerateParentheses(depth - 1, target, $"({GenerateExpression(depth - 1, target)})", shouldBuildParentheses, parenthesesCount + 4));
                                break;
                            case 2:
                                expressionBuilder.Append(GenerateParentheses(depth - 1, target, $"{MathFunctions.GetRandomFunction().Name}({GenerateExpression(depth - 1, target)})", shouldBuildParentheses, parenthesesCount));
                                break;
                        }
                    }
                    break;
            
            }
        }
        expressionBuilder.Append(Random.NextDouble() < 0.8 ? @operator : @logicalOperator);
        
        // insert a not operator
        if (Random.NextDouble() < 0.2)
        {
            expressionBuilder.Append(BooleanOperators[0]);
        }
        
        // Generate right expression
        randomCase = Random.Next(0, 5);
        switch (randomCase)
        {
            case 0:
                //parentheses
                expressionBuilder.Append(GenerateParentheses(depth - 1, target, GenerateExpression(depth - 1, target), shouldBuildParentheses, parenthesesCount));
                break;
            case 1:
                expressionBuilder.Append(GenerateParentheses(depth - 1, target, $"{GenerateExpression(depth - 1, target)}", shouldBuildParentheses, parenthesesCount));
                break;
            case 2:
                expressionBuilder.Append(GenerateParentheses(depth - 1, target, $"{MathFunctions.GetRandomFunction().Name}({GenerateExpression(depth - 1, target)})", shouldBuildParentheses, parenthesesCount));
                break;
            case 3:
                var p = Random.Next(1, 100);
                var g = Random.Next(1, p);
                var x = Random.Next(1, p);
                var y = Random.Next(1, p);
                expressionBuilder.Append($"(({g} ^ ({x} + {y})) % {p})");
                break;
            case 4:
                // Summations (Lua-compatible)
                var n = Random.Next(0, 25);
                var cache_variable = GenerateRandomVariableName();
                var sum_variable = GenerateRandomVariableName();
                var a = string.Join(", ", Enumerable.Range(0, n).Select(_ => Random.Next(-100, 100)));
                expressionBuilder.Append($"(function();local {cache_variable}={{ {a} }}; local {sum_variable}=0; for i=1, {n} do;{sum_variable}={sum_variable}+{cache_variable}[i];end;return {sum_variable};end)()");
                break;
        }

        return expressionBuilder.ToString();
    }
    
    private static async Task<string> GenerateEquationAsync(double previousAnswer, int targetComplexity, int recursionDepth, CancellationToken cancellationToken)
    {
        var equation = string.Empty;
        var complexity = 0;

        while (complexity < targetComplexity)
        {
            var depth = Random.Next(0, 2);
            var expression = GenerateExpression(depth, previousAnswer);

            for (var i = 0; i < recursionDepth; i++)
            {
                expression = ApplyExpressionManipulation(expression);
            }

            var equationResult = Evaluate(expression);
            var difference = previousAnswer - equationResult;

            if (double.IsNaN(equationResult) || equationResult == 0 || Math.Abs(difference) < 1000)
            {
                continue;
            }

            if (Math.Abs(difference) <= 1e-10)
            {
                equation = expression;
                complexity = CountComplexity(equation);
            }
            else
            {
                // we check if the difference is near or equal to the target and if it is, we continue
                if (Math.Abs(difference) <= 1e-5)
                {
                    continue;
                }

                
                // we want to use the different as a constant to adjust the equation to equal the target
                var adjustedEquation = $"({expression}) " + (difference > 0 ? "+ " : "- ") + Math.Abs(difference);
                var adjustedResult = Evaluate(adjustedEquation);

                if (double.IsNaN(adjustedResult) || Math.Abs(adjustedResult - previousAnswer) > 1e-10)
                {
                    continue;
                }

                equation = adjustedEquation;
                complexity = CountComplexity(equation);
            }
        }

        return equation;
    }
    
    private static int SelectIndexBasedOnProbabilities(double[] probabilities)
    {
        var sum = probabilities.Sum();
        var randomValue = Random.NextDouble() * sum;
        var cumulativeProbability = 0.0;

        for (var i = 0; i < probabilities.Length; i++)
        {
            cumulativeProbability += probabilities[i];
            if (randomValue <= cumulativeProbability)
            {
                return i;
            }
        }

        return probabilities.Length - 1;
    }

    private static string GenerateContinuedFraction(int depth)
    {
        if (depth == 0)
        {
            return Random.Next(-100, 100).ToString();
        }

        var numerator = Random.Next(-100, 100);
        var denominator = GenerateContinuedFraction(depth - 1);
        return $"{numerator} / ({denominator})";
    }
    
    // generate a random variable name that is valid for lua (eg: starting with underscore or letter and can have numbers after)
    private static string GenerateRandomVariableName()
    {
        var variableName = new StringBuilder();
        var length = Random.Next(1, 10);
        for (var i = 0; i < length; i++)
        {
            if (i == 0)
            {
                var firstCharacter = Random.Next(0, 2) == 0 ? (char)Random.Next(65, 91) : '_';
                variableName.Append(firstCharacter);
            }
            else
            {
                var character = Random.Next(0, 3) == 0 ? (char)Random.Next(48, 58) : (char)Random.Next(65, 91);
                variableName.Append(character);
            }
        }

        return variableName.ToString();
    }
    
    private static int CountComplexity(string equation)
    {
        var complexity = Operators.Sum(op => equation.Split(op).Length - 1) * 2;
        var functionMatches = FunctionsRegex.Matches(equation);
        complexity += functionMatches.Count * 2;
        return complexity;
    }

    private static async Task<List<string>> GenerateEquationsAsync(double solution, int count, int targetComplexity, int recursionDepth, CancellationToken cancellationToken)
    {
        var equations = new ConcurrentBag<string>();

        var tasks = Enumerable.Range(0, count)
            .Select(_ => Task.Run(async () =>
            {
                var equation = await GenerateEquationAsync(solution, targetComplexity, recursionDepth, cancellationToken);
                equations.Add(equation);
            }, cancellationToken));

        await Task.WhenAll(tasks);

        return equations.ToList();
    }

    public static async Task GenerateExpressionsAsync(double solution, int count, int targetComplexity, int recursionDepth, CancellationToken cancellationToken)
    {
        var equations = await GenerateEquationsAsync(solution, count, targetComplexity, recursionDepth, cancellationToken);
        await PrintEquationsAsync(solution, equations);
    }

    private static async Task PrintEquationsAsync(double solution, List<string> equations)
    {
        var results = await Task.Run(() => EvaluateBatch(equations));
        for (var i = 0; i < equations.Count; i++)
        {
            var equation = equations[i];
            equation = Regex.Replace(equation, @"(?<!\band\b)(?<!\bor\b)(?<!\bnot\b)(?<!\bfalse\b)(?<!\btrue\b)(?<!\bnil\b)\s(?!\band\b)(?!\bor\b)(?!\bnot\b)(?!\bfalse\b)(?!\btrue\b)(?!\bnil\b)", "");
            
            
            equation = equation.Replace(@"for", "for ");
            equation = equation.Replace(@"local", "local ");
            equation = equation.Replace(@"do", " do");
            equation = equation.Replace(@"return", "return ");
            
            equation = equation.Replace("--", " -");
            Console.WriteLine($"Equation {i + 1}:");
            Console.WriteLine("Original answer: " + solution);
            Console.WriteLine("Generated equation: " + equation);
            Console.WriteLine("Equation answer: " + results[i]);
            Console.WriteLine();
        }
    }
} 
