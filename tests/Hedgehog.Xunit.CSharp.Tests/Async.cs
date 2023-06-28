﻿using System.Threading.Tasks.Sources;
using Hedgehog.Linq;

namespace Hedgehog.Xunit.CSharp.Tests;

public class Async
{
    internal static Task Fast()
  {
        return Task.CompletedTask;
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