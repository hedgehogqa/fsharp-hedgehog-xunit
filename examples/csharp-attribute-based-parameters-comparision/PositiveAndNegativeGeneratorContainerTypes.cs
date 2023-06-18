using Hedgehog;
using Hedgehog.Linq;
using Hedgehog.Xunit;
using Gen = Hedgehog.Linq.Gen;
using Range = Hedgehog.Linq.Range;


namespace csharp_attribute_based_parameters_comparision;

public record PositiveInt(int Value);
public record NegativeInt( int Value );

public class Generators
{
  public static Gen<PositiveInt> GenPositiveInt =>
    from x in Gen.Int32(Range.Constant(1, Int32.MaxValue))
    select new PositiveInt(x);

  public static Gen<NegativeInt> GenNegativeInt =>
    from x in Gen.Int32(Range.Constant(Int32.MinValue, -1))
    select new NegativeInt(x);

  public static AutoGenConfig _ => GenX.defaults
    .WithGenerator(GenPositiveInt)
    .WithGenerator(GenNegativeInt);
}

public class PositiveAndNegativeGeneratorContainerTypes
{
  [Property(typeof(Generators))]
  public bool ResultOfAddingPositiveAndNegativeLessThanPositive(
    PositiveInt positive,
    NegativeInt negative)
    => positive.Value + negative.Value < positive.Value;

}
