namespace EquationGenerator;

public class MathFunction(string name, int argumentCount, List<ArgumentRange> argumentRanges)
{
    public string Name { get; set; } = name;
    public int ArgumentCount { get; set; } = argumentCount;
    public List<ArgumentRange> ArgumentRanges { get; set; } = argumentRanges;
}