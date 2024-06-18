namespace EquationGenerator;


public class ArgumentRange
{
    public int Min { get; set; }
    public int Max { get; set; }

    public ArgumentRange(int min, int max)
    {
        Min = min;
        Max = max;
    }
}