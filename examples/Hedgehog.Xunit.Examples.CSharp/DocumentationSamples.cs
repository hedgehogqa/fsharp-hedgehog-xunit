namespace Hedgehog.Xunit.Examples.CSharp;

using global::Xunit.Abstractions;
using Hedgehog;
using Hedgehog.Linq;
using Hedgehog.Xunit;
using Microsoft.FSharp.Core;
using static Hedgehog.Linq.Property;
using Gen = Linq.Gen;
using Range = Linq.Range;

public class DocumentationSamples
{
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
    [Property(Skip = "For documentation purposes")]
    public void Will_fail_by_assertion(
        bool value) =>
        Assert.True(value, "All booleans values should be true!");

    //[Property]
    [Property(Skip = "Problem with async method")]
    public async Task<bool> Async_with_exception_shrinks(int i)
    {
        await Task.Delay(100);
        //Assert.True(i +i == 1);
        return true;
    }

    public class AutoGenConfigContainer
    {
        public static AutoGenConfig _ =>
            GenX.defaults.WithGenerator(Gen.Int32(Range.FromValue(13)));
    }

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

    [Fact]
    public void Can_create_some()
    {
        var t = FSharpOption<int>.Some(5);
    }

    [Property(typeof(AutoGenConfigContainer))]
    public bool This_test_passes_because_always_13(
        int i) =>
        i == 13;

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

    internal static Task FooAsync()
    {
        return Task.Delay(100);
    }

    [Property]
    public async Task Async_Task_property(
      int i)
    {
        await FooAsync();
        Assert.StrictEqual(i, i);
    }

    [Property]
    public Task Task_property(
      int i)
    {
        Assert.StrictEqual(i, i);
        return Task.Delay(100);
    }

    [Property]
    public async Task<bool> Async_boolean(bool i)
    {
        await FooAsync();
        return i || !i;
    }

    [Property]
    public async Task<bool> Task_boolean(bool i)
    {
        await Task.Delay(100);
        return i || !i;
    }

    [Fact]
    public void Reversing_a_list_twice_yields_the_original_list()
    {
        var gen = GenX.auto<List<int>>();
        var prop = from data in ForAll(gen)
                   let testList = Enumerable.Reverse(data).Reverse().ToList()
                   select Assert.Equivalent(data, testList, true);
        prop.Check();
    }

    [Property]
    public void Reversing_a_list_twice_yields_the_original_list_with_xunit(List<int> xs)
    {
        var testList = Enumerable.Reverse(xs).ToList();
        Assert.Equivalent(xs, testList, true);
    }

    [Property]
    [Recheck("44_13097736474433561873_6153509253234735533_")]
    public bool this_passes_now(int i) =>
      i == 12345;
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


