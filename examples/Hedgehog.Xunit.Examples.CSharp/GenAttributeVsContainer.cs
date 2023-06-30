namespace Hedgehog.Xunit.Examples.CSharp;

using Hedgehog;
using Hedgehog.Linq;
using Hedgehog.Xunit;
using Gen = Linq.Gen;
using Range = Linq.Range;

public class PositiveAndNegativeWithAutoGenConfigContainer
{
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

    [Property(typeof(Generators))]
    public bool ResultOfAddingPositiveAndNegativeLessThanPositive(
        PositiveInt positive,
        NegativeInt negative) =>
            positive.Value + negative.Value < positive.Value;
}

public class PositiveAndNegativeWithGenAttribute
{
    public class Negative : GenAttribute<int>
    {
        public override Gen<int> Generator => Gen.Int32(Range.Constant(int.MinValue, -1));
    }

    public class Positive : GenAttribute<int>
    {
        public override Gen<int> Generator => Gen.Int32(Range.Constant(1, int.MaxValue));
    }

    [Property]
    public bool ResultOfAddingPositiveAndNegativeLessThanPositive(
        [Positive] int positive,
        [Negative] int negative) =>
            positive + negative < positive;
}

public class PositiveAndNegativeWithParameterizedGenAttribute
{
    public class Int32Range : GenAttribute<int>
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

    [Property]
    public bool ResultOfAddingPositiveAndNegativeLessThanPositive(
        [Int32Range(1, int.MaxValue)] int positive,
        [Int32Range(int.MinValue, -1)] int negative) =>
            positive + negative < positive;
}
