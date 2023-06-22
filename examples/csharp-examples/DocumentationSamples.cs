using Hedgehog;
using Hedgehog.Linq;
using Hedgehog.Xunit;
using Xunit.Abstractions;
using Property = Hedgehog.Linq.Property;

namespace csharp_examples;

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


  [Fact]
  public void Reversing_a_list_twice_yields_the_original_list()
  {
    var gen = GenX.auto<List<int>>();
    var prop = from data in Property.ForAll(gen)
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
}
