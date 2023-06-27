using Hedgehog;
using Hedgehog.Linq;
using Hedgehog.Xunit;
using Gen = Hedgehog.Linq.Gen;
using Range = Hedgehog.Linq.Range;

namespace csharp_examples.attribute_based_parameter_comparison;

public record PositiveInt(int Value);
public record NegativeInt(int Value);

public class Generators
{
    public static Gen<PositiveInt> GenPositiveInt =>
        from i in Gen.Int32(Range.Constant(1, int.MaxValue))
        select new PositiveInt(i);

    public static Gen<NegativeInt> GenNegativeInt =>
        from i in Gen.Int32(Range.Constant(int.MinValue, -1))
        select new NegativeInt(i);

    public static AutoGenConfig _ => GenX.defaults
        .WithGenerator(GenPositiveInt)
        .WithGenerator(GenNegativeInt);
}

public class PositiveAndNegativeGeneratorContainerTypes
{
    [Property(typeof(Generators))]
    public bool ResultOfAddingPositiveAndNegativeLessThanPositive(
        PositiveInt positive,
        NegativeInt negative) =>
            positive.Value + negative.Value < positive.Value;

}
