using Hedgehog;
using Hedgehog.Xunit;
using Gen = Hedgehog.Linq.Gen;
using Range = Hedgehog.Linq.Range;

namespace csharp_examples.attribute_based_parameter_comparison;

public class Int32Range : ParameterGenerator<int>
{
    private readonly int _min;
    private readonly int _max;

    public Int32Range(int min, int max)
    {
        _min = min;
        _max = max;
    }

    public override Gen<int> Generator => Gen.Int32(Range.Constant(_min, _max));
}

public class PositiveAndNegativeWithAttributes
{
    [Property]
    public bool ResultOfAddingPositiveAndNegativeLessThanPositive(
        [Int32Range(1, int.MaxValue)] int positive,
        [Int32Range(int.MinValue, -1)] int negative) =>
            positive + negative < positive;
}
