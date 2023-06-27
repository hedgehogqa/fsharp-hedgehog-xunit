using Hedgehog;
using Hedgehog.Xunit;
using Gen = Hedgehog.Linq.Gen;
using Range = Hedgehog.Linq.Range;

namespace csharp_examples.attribute_based_parameter_comparison;

public class Negative : GenAttribute<int>
{
    public override Gen<int> Generator => Gen.Int32(Range.Constant(int.MinValue, -1));
}

public class Positive : GenAttribute<int>
{
    public override Gen<int> Generator => Gen.Int32(Range.Constant(1, int.MaxValue));
}

public class PositiveAndNegativeUtilizingIntegerRangeAttribute
{
    [Property]
    public bool ResultOfAddingPositiveAndNegativeLessThanPositive(
        [Positive] int positive,
        [Negative] int negative) =>
            positive + negative < positive;
}
