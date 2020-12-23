namespace Hedgehog.Xunit

open System
open Xunit.Sdk
open Hedgehog

/// Generates arguments using GenX.auto (or autoWith if you provide an AutoGenConfig), then runs Property.check
[<AttributeUsage(AttributeTargets.Method ||| AttributeTargets.Property, AllowMultiple = false)>]
[<XunitTestCaseDiscoverer("Hedgehog.Xunit.XunitOverrides+PropertyTestCaseDiscoverer", "Hedgehog.Xunit")>]
type PropertyAttribute(t) =
  inherit Xunit.FactAttribute()

  let mutable _autoGenConfig: Type = t

  new() = PropertyAttribute(null)

  // https://github.com/dotnet/fsharp/issues/4154 sigh
  /// This requires a type with a single static member (with any name) that returns an AutoGenConfig.
  ///
  /// Example usage:
  ///
  /// ```
  ///
  /// type Int13 = static member AnyName = { GenX.defaults with Int = Gen.constant 13 }
  ///
  /// [<Property(typeof<Int13>)>]
  ///
  /// let myTest (i:int) = ...
  ///
  /// ```
  member _.AutoGenConfig
    with get() = _autoGenConfig
    and  set v = _autoGenConfig <- v


module internal PropertyHelper =
  open System.Reflection

  type private MarkerRecord = class end
  let private genxAutoBoxWith<'T> x = x |> GenX.autoWith<'T> |> Gen.map box
  let private genxAutoBoxWithMethodInfo =
    typeof<MarkerRecord>.DeclaringType.GetTypeInfo().DeclaredMethods
    |> Seq.find (fun meth -> meth.Name = nameof(genxAutoBoxWith))

  module Option =
    let requireSome msg =
      function
      | Some x -> x
      | None   -> failwith msg
    let plus (x: 'a option) (y: 'a option) =
      match x with
      | Some _ -> x
      | None -> y

  let check (methodinfo:MethodInfo) testClassInstance =
    let config =
      methodinfo.CustomAttributes
      |> Seq.filter (fun x -> x.AttributeType = typeof<PropertyAttribute>)
      |> Seq.tryExactlyOne
      |> Option.requireSome $"There must be exactly one {nameof(PropertyAttribute)}."
      |> fun attribute ->
        let ctorArgType =
          attribute.ConstructorArguments
          |> Seq.filter (fun x -> x.ArgumentType = typeof<Type>)
          |> Seq.tryExactlyOne
          |> Option.map (fun x -> x.Value :?> Type)
        let namedArgType =
          attribute.NamedArguments
          |> Seq.filter(fun x -> x.TypedValue.ArgumentType = typeof<Type>)
          |> Seq.tryExactlyOne
          |> Option.map (fun x -> x.TypedValue.Value :?> Type)
        Option.plus ctorArgType namedArgType
      |> Option.map(fun t ->
          t.GetProperties()
          |> Seq.filter (fun x ->
            x.GetMethod.IsStatic &&
            x.GetMethod.ReturnType = typeof<AutoGenConfig>
          ) |> Seq.tryExactlyOne
          |> Option.requireSome $"{t.FullName} must have exactly one static property that returns an {nameof(AutoGenConfig)}"
          |> fun x -> x.GetMethod.Invoke(null, [||])
          :?> AutoGenConfig
      ) |> Option.defaultValue GenX.defaults
    let gens =
      methodinfo.GetParameters()
      |> Array.map (fun p ->
        genxAutoBoxWithMethodInfo
          .MakeGenericMethod(p.ParameterType)
          .Invoke(null, [|config|])
        :?> Gen<obj>)
      |> ArrayGen.toGenTuple
    let invoke t =
      methodinfo.Invoke(testClassInstance, Microsoft.FSharp.Reflection.FSharpValue.GetTupleFields t)
      |> function
      | :? bool as b -> Property.ofBool b
      | _            -> Property.success ()
    Property.forAll gens invoke |> Property.check


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
