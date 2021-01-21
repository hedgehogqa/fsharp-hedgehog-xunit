module internal XunitOverrides

open Xunit.Sdk
open Hedgehog
open System

type PropertyTestInvoker  (test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, beforeAfterAttributes, aggregator, cancellationTokenSource) =
  inherit XunitTestInvoker(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, beforeAfterAttributes, aggregator, cancellationTokenSource)
  
  override this.CallTestMethod testClassInstance =
    InternalLogic.report this.TestMethod this.TestClass testClassInstance
    |> Report.tryRaise
    null
  
type PropertyTestRunner  (test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, skipReason, beforeAfterAttributes, aggregator, cancellationTokenSource) =
  inherit XunitTestRunner(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, skipReason, beforeAfterAttributes, aggregator, cancellationTokenSource)
  
  override this.InvokeTestMethodAsync aggregator =
    PropertyTestInvoker(this.Test, this.MessageBus, this.TestClass, this.ConstructorArguments, this.TestMethod, this.TestMethodArguments, this.BeforeAfterAttributes, aggregator, this.CancellationTokenSource)
      .RunAsync()
  
type PropertyTestCaseRunner(testCase: IXunitTestCase, displayName, skipReason, constructorArguments, testMethodArguments, messageBus, aggregator, cancellationTokenSource) =
  inherit XunitTestCaseRunner(testCase,               displayName, skipReason, constructorArguments, testMethodArguments, messageBus, aggregator, cancellationTokenSource)
  
  override this.RunTestAsync() =
    let args = this.TestMethod.GetParameters().Length |> Array.zeroCreate // need to pass the right number of args otherwise an exception will be thrown by XunitTestInvoker's InvokeTestMethodAsync, whose behavior I don't feel like overriding.
    PropertyTestRunner(this.CreateTest(this.TestCase, this.DisplayName), this.MessageBus, this.TestClass, this.ConstructorArguments, this.TestMethod, args, this.SkipReason, this.BeforeAfterAttributes, this.Aggregator, this.CancellationTokenSource)
      .RunAsync()
  
open System.ComponentModel
type PropertyTestCase  (diagnosticMessageSink, defaultMethodDisplay, testMethodDisplayOptions, testMethod, ?testMethodArguments) =
  inherit XunitTestCase(diagnosticMessageSink, defaultMethodDisplay, testMethodDisplayOptions, testMethod, (testMethodArguments |> Option.defaultValue null))
  
  [<EditorBrowsable(EditorBrowsableState.Never)>]
  [<Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")>]
  [<System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage>]
  new() = new PropertyTestCase(null, TestMethodDisplay.ClassAndMethod, TestMethodDisplayOptions.All, null)
  
  override this.RunAsync(_, messageBus, constructorArguments, aggregator, cancellationTokenSource) =
    PropertyTestCaseRunner(this, this.DisplayName, this.SkipReason, constructorArguments, this.TestMethodArguments, messageBus, aggregator, cancellationTokenSource)
      .RunAsync()
  
type PropertyTestCaseDiscoverer(messageSink) =
  
  interface IXunitTestCaseDiscoverer with
    override _.Discover(discoveryOptions, testMethod, _) =
      new PropertyTestCase(messageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod)
      :> IXunitTestCase
      |> Seq.singleton
