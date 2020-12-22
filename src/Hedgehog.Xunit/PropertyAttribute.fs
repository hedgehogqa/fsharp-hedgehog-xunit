namespace Hedgehog.Xunit

module internal PropertyHelper =
  open Hedgehog
  open System.Reflection

  type private MarkerRecord = class end
  let private genxAutoBox<'T> = GenX.auto<'T> |> Gen.map box
  let private genxAutoBoxMethodInfo =
    typeof<MarkerRecord>.DeclaringType.GetTypeInfo().DeclaredMethods
    |> Seq.find (fun meth -> meth.Name = nameof(genxAutoBox))

  let check (methodinfo:MethodInfo) testClassInstance =
    let gens =
      methodinfo.GetParameters()
      |> Array.map (fun p ->
        genxAutoBoxMethodInfo
          .MakeGenericMethod(p.ParameterType)
          .Invoke(null, null)
        :?> Gen<obj>)
      |> ArrayGen.toGenTuple
    let invoke t =
      methodinfo.Invoke(testClassInstance, Microsoft.FSharp.Reflection.FSharpValue.GetTupleFields t)
      |> function
      | :? bool as b -> Property.ofBool b
      | _            -> Property.success ()
    Property.forAll gens invoke |> Property.check

open System
open Xunit.Sdk

module internal XunitOverrides =
  type PropertyTestInvoker  (test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, beforeAfterAttributes, aggregator, cancellationTokenSource) =
    inherit XunitTestInvoker(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, beforeAfterAttributes, aggregator, cancellationTokenSource)
  
    override this.CallTestMethod testClassInstance =
      PropertyHelper.check this.TestMethod testClassInstance
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
    new() = new PropertyTestCase(null, TestMethodDisplay.ClassAndMethod, TestMethodDisplayOptions.All, null)
  
    override this.RunAsync(_, messageBus, constructorArguments, aggregator, cancellationTokenSource) =
      PropertyTestCaseRunner(this, this.DisplayName, this.SkipReason, constructorArguments, this.TestMethodArguments, messageBus, aggregator, cancellationTokenSource)
        .RunAsync()
  
  type PropertyTestCaseDiscoverer(messageSink) =
  
    member _.MessageSink = messageSink
  
    interface IXunitTestCaseDiscoverer with
      override this.Discover(discoveryOptions, testMethod, _) =
        new PropertyTestCase(this.MessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod)
        :> IXunitTestCase
        |> Seq.singleton

/// Generates arguments using Hedgehog.Experimental.GenX.auto, then runs Property.forAll
[<AttributeUsage(AttributeTargets.Method ||| AttributeTargets.Property, AllowMultiple = false)>]
[<XunitTestCaseDiscoverer("Hedgehog.Xunit.XunitOverrides+PropertyTestCaseDiscoverer", "Hedgehog.Xunit")>]
type PropertyAttribute() =
  inherit Xunit.FactAttribute()
