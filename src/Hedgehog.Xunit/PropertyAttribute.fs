namespace Hedgehog.Xunit

open System
open Xunit.Sdk
open Hedgehog

/// Generates arguments using GenX.auto (or autoWith if you provide an AutoGenConfig), then runs Property.check
[<AttributeUsage(AttributeTargets.Method ||| AttributeTargets.Property, AllowMultiple = false)>]
[<XunitTestCaseDiscoverer("Hedgehog.Xunit.XunitOverrides+PropertyTestCaseDiscoverer", "Hedgehog.Xunit")>]
type PropertyAttribute(autoGenConfig, tests, skip) =
  inherit Xunit.FactAttribute(Skip = skip)

  let mutable _autoGenConfig: Type       option = autoGenConfig
  let mutable _tests        : int<tests> option = tests

  new()                     = PropertyAttribute(None              , None       , null)
  new(autoGenConfig)        = PropertyAttribute(Some autoGenConfig, None       , null)
  new(autoGenConfig, tests) = PropertyAttribute(Some autoGenConfig, Some tests , null)
  new(autoGenConfig, skip)  = PropertyAttribute(Some autoGenConfig, None       , skip)
  new(tests)                = PropertyAttribute(None              , Some tests , null)
  new(skip)                 = PropertyAttribute(None              , None       , skip)

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
  member             _.AutoGenConfig with set v = _autoGenConfig <- Some v
  member             _.Tests         with set v = _tests         <- Some v
  member internal _.GetAutoGenConfig            = _autoGenConfig
  member internal _.GetTests                    = _tests


///Set a default AutoGenConfig for all [<Property>] attributed methods in this class/module
[<AttributeUsage(AttributeTargets.Class, AllowMultiple = false)>]
type PropertiesAttribute(autoGenConfig: Type, tests: int<tests>) =
  inherit PropertyAttribute(autoGenConfig, tests)
  new(autoGenConfig:Type      ) = PropertiesAttribute(autoGenConfig, 100<tests>)
  new(tests        :int<tests>) = PropertiesAttribute(null         , tests     )

module internal PropertyHelper =

  module Option =
    let requireSome msg =
      function
      | Some x -> x
      | None   -> failwith msg
  let (++) (x: 'a option) (y: 'a option) =
    match x with
    | Some _ -> x
    | None -> y

  open System.Reflection
  type private Marker = class end
  let private genxAutoBoxWith<'T> x = x |> GenX.autoWith<'T> |> Gen.map box
  let private genxAutoBoxWithMethodInfo =
    typeof<Marker>.DeclaringType.GetTypeInfo().DeclaredMethods
    |> Seq.find (fun m -> m.Name = nameof(genxAutoBoxWith))

  let parseAttributes (testMethod:MethodInfo) (testClass:Type) =
    let classAutoGenConfig, classTests =
      testClass.GetCustomAttributes(typeof<PropertiesAttribute>)
      |> Seq.tryExactlyOne
      |> Option.map (fun x -> x :?> PropertiesAttribute)
      |> function
      | Some x -> x.GetAutoGenConfig, x.GetTests
      | None   -> None              , None
    let configType, tests =
      testMethod.GetCustomAttributes(typeof<PropertyAttribute>)
      |> Seq.exactlyOne
      :?> PropertyAttribute
      |> fun methodAttribute ->
        methodAttribute.GetAutoGenConfig ++ classAutoGenConfig,
        methodAttribute.GetTests         ++ classTests        |> Option.defaultValue 100<tests>
    let config =
      match configType with
      | None -> GenX.defaults
      | Some t ->
        t.GetProperties()
        |> Seq.filter (fun p ->
          p.GetMethod.IsStatic &&
          p.GetMethod.ReturnType = typeof<AutoGenConfig>
        ) |> Seq.tryExactlyOne
        |> Option.requireSome $"{t.FullName} must have exactly one static property that returns an {nameof(AutoGenConfig)}.

An example type definition:

type {t.Name} =
  static member __ =
    {{ GenX.defaults with
        Int = Gen.constant 13 }}
"       |> fun x -> x.GetMethod.Invoke(null, [||])
        :?> AutoGenConfig
    config, tests

  let report (testMethod:MethodInfo) testClass testClassInstance =
    let config, tests = parseAttributes testMethod testClass
    let gens =
      testMethod.GetParameters()
      |> Array.mapi (fun i p ->
        if p.ParameterType.ContainsGenericParameters then
          invalidArg p.Name $"The parameter type '{p.ParameterType.Name}' at index {i} is generic, which is unsupported. Consider using a type annotation to make the parameter's type concrete."
        genxAutoBoxWithMethodInfo
          .MakeGenericMethod(p.ParameterType)
          .Invoke(null, [|config|])
        :?> Gen<obj>)
      |> ArrayGen.toGenTuple
    let invoke t =
      let args =
        match testMethod.GetParameters() with
        | [||] -> [||]
        | _ -> Microsoft.FSharp.Reflection.FSharpValue.GetTupleFields t
      testMethod.Invoke(testClassInstance, args)
      |> function
      | :? bool as b -> Property.ofBool b
      | _            -> Property.success ()
    Property.forAll gens invoke |> Property.report' tests


module internal XunitOverrides =
  type PropertyTestInvoker  (test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, beforeAfterAttributes, aggregator, cancellationTokenSource) =
    inherit XunitTestInvoker(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, beforeAfterAttributes, aggregator, cancellationTokenSource)
  
    override this.CallTestMethod testClassInstance =
      PropertyHelper.report this.TestMethod this.TestClass testClassInstance
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
