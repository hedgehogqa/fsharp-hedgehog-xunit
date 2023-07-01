namespace Hedgehog.Xunit.CSharp.Tests;

public class Async
{

  public Task Fast()
  {
    return Task.Delay(100);
  }

  [Property]
  public async Task Async_property_which_returns_task_can_run(
    int i)
  {
     await Fast();
     Assert.True(i == i);
  }

  [Property]
  public async Task<bool> Async_property_which_returns_boolean_task_can_run(bool i)
  {
    await Fast();
    return i || !i;
  }


}
