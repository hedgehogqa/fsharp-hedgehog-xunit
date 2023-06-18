using Hedgehog;
using Hedgehog.Xunit;
using Gen = Hedgehog.Linq.Gen;
using Range = Hedgehog.Linq.Range;

namespace csharp_attribute_based_parameters_comparision;

public class Int32Range : ParameterGeneraterBaseType<int>
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
    [Int32Range(1, Int32.MaxValue)] int positive,
    [Int32Range(Int32.MinValue, -1 )] int negative)
    => positive + negative < positive;
}
