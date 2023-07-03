namespace Hedgehog.Xunit.Examples.CSharp;

using global::Xunit.Abstractions;
using Hedgehog;
using Hedgehog.Linq;
using Hedgehog.Xunit;
using Microsoft.FSharp.Core;
using Gen = Linq.Gen;
using Property = Linq.Property;
using Range = Linq.Range;

public class DocumentationSamples
{
    [Fact]
    public void Reversing_a_list_twice_yields_the_original_list()
    {
        var gen = GenX.auto<List<int>>();
        var prop = from xs in Property.ForAll(gen)
                   let testList = Enumerable.Reverse(xs).Reverse().ToList()
                   select Assert.Equal(xs, testList);
        prop.Check();
    }

    [Property]
    public void Reversing_a_list_twice_yields_the_original_list_with_xunit(List<int> xs)
    {
        var testList = Enumerable.Reverse(xs).Reverse().ToList();
        Assert.Equal(xs, testList);
    }

    private readonly ITestOutputHelper _output;

    public DocumentationSamples(ITestOutputHelper output)
    {
        _output = output;
    }

    [Property]
    public void Can_generate_an_int(
      int i)
    {
        _output.WriteLine($"Test input: {i}");
    }

    //[Property]
    [Property(Skip = "For documentation purposes")]
    public bool Will_fail(bool value) => value;

    //[Property]
    [Property(Skip = "Problem with async method")]
    public async Task Task_with_exception_shrinks(int i)
    {
        await Task.Delay(100);
        if (i > 10) throw new Exception("whoops");
    }

    //[Property]
    [Property(Skip = "Problem with Result")]
    public FSharpResult<int, string> Result_with_Error_shrinks(int i) =>
        i < 10
            ? FSharpResult<int, string>.NewOk(i)
            : FSharpResult<int, string>.NewError("humbug!");

    //[Property]
    [Property(Skip = "For documentation purposes")]
    public Property<bool> Returning_a_failing_property_bool_with_an_external_number_gen_fails_and_shrinks(int i) =>
        from fifty in Property.ForAll(Hedgehog.Gen.constant(50))
        select i <= fifty;

    public class AutoGenConfigContainer
    {
        public static AutoGenConfig _ =>
            GenX.defaults.WithGenerator(Gen.Int32(Range.FromValue(13)));
    }

    [Property(typeof(AutoGenConfigContainer))]
    public bool This_test_passes_because_always_13(int i) => i == 13;

    public class ConfigWithArgs
    {
        public static AutoGenConfig _(
            string word,
            int number) =>
            GenX.defaults
                .WithGenerator(Hedgehog.Gen.constant(word))
                .WithGenerator(Hedgehog.Gen.constant(number));
    }

    [Property(AutoGenConfig = typeof(ConfigWithArgs), AutoGenConfigArgs = new object[] { "foo", 13 })]
    public bool This_also_passes(string s, int i) =>
        s == "foo" && i == 13;

    [Property(tests: 3)]
    public void This_runs_3_times() => _output.WriteLine($"Test run");

    [Property(Shrinks = 0, Skip = "For documentation purposes")]
    public void No_shrinks_occur(int i)
    {
        if (i > 50)
        {
            throw new Exception("oops");
        }
    }

    [Property(Size = 2)]
    public void i_mostly_ranges_between_neg_1_and_1(int i) => _output.WriteLine(i.ToString());

    [Property]
    [Recheck("44_13097736474433561873_6153509253234735533_")]
    public bool this_passes(int i) => i == 12345;

    public class Five : GenAttribute<int>
    {
        public override Gen<int> Generator => Gen.Int32(Range.FromValue(5));
    }

    [Property]
    public bool Can_set_parameter_as_5(
        [Five] int five) =>
            five == 5;

    public class ConstInt : GenAttribute<int>
    {
        private readonly int _i;
        public ConstInt(int i)
        {
            _i = i;
        }
        public override Gen<int> Generator => Gen.Int32(Range.FromValue(_i));
    }

    [Property(typeof(AutoGenConfigContainer))]
    public bool GenAttribute_overrides_Property_AutoGenConfig(int thirteen, [ConstInt(6)] int six) =>
        thirteen == 13 && six == 6;

    [Properties(Tests = 13, AutoGenConfig = typeof(AutoGenConfigContainer))]
    public class __
    {

        [Property(AutoGenConfig = typeof(AutoGenConfigContainer), Tests = 2718, Skip = "just because")]
        public void Not_sure_why_youd_do_this_but_okay() { }
    }

    public class Int13
    {
        public static AutoGenConfig _ =>
            GenX.defaults.WithGenerator(Gen.Int32(Range.FromValue(13)));
    }

    public class PropertyInt13Attribute : PropertyAttribute
    {
        public PropertyInt13Attribute() : base(typeof(Int13)) { }
    }

    [PropertyInt13]
    public bool This_passes(int i) => i == 13;

    public class PropertiesInt13Attribute : PropertiesAttribute
    {
        public PropertiesInt13Attribute() : base(typeof(Int13)) { }
    }

    [PropertiesInt13]
    public class ___
    {
        [Property]
        public bool This_also_passes(int i) => i == 13;
    }
}


public class Int13
{
    public static AutoGenConfig _ => GenX.defaults.WithGenerator(Hedgehog.Gen.constant(13));
}

public class Int2718
{
    public static AutoGenConfig _ => GenX.defaults.WithGenerator(Hedgehog.Gen.constant(2718));
}

[Properties(typeof(Int13), 1)]
public class PropertiesSample
{
    [Property]
    public bool this_passes_and_runs_once(
        int i) =>
        i == 13;

    [Property(typeof(Int2718), 2)]
    public bool this_passes_passes_and_runs_twice(
        int i) =>
        i == 2718;
}
