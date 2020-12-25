namespace Hedgehog.Xunit

open System
open Xunit.Sdk
open Hedgehog

/// Generates arguments using GenX.auto (or autoWith if you provide an AutoGenConfig), then runs Property.check
[<AttributeUsage(AttributeTargets.Method ||| AttributeTargets.Property, AllowMultiple = false)>]
[<XunitTestCaseDiscoverer("Hedgehog.Xunit.XunitOverrides+PropertyTestCaseDiscoverer", "Hedgehog.Xunit")>]
type PropertyAttribute(autoGenConfig: Type, tests:int<tests>, skip) =
  inherit Xunit.FactAttribute(Skip = skip)

  new()                     = PropertyAttribute(null         , 100<tests>, null)
  new(autoGenConfig)        = PropertyAttribute(autoGenConfig, 100<tests>, null)
  new(autoGenConfig, tests) = PropertyAttribute(autoGenConfig, tests     , null)
  new(autoGenConfig, skip)  = PropertyAttribute(autoGenConfig, 100<tests>, skip)
  new(tests)                = PropertyAttribute(null         , tests     , null)
  new(skip)                 = PropertyAttribute(null         , 100<tests>, skip)

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
  member _.AutoGenConfig with set (_: Type      ) = ()
  member _.Tests         with set (_: int<tests>) = ()


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

  let ctorArg<'A> (attribute:CustomAttributeData) =
    attribute.ConstructorArguments
    |> Seq.filter (fun x -> x.ArgumentType = typeof<'A>)
    |> Seq.tryExactlyOne
    |> Option.map (fun x -> x.Value :?> 'A)
  let namedArg<'A> (attribute:CustomAttributeData) =
    attribute.NamedArguments
    |> Seq.filter(fun x -> x.TypedValue.ArgumentType = typeof<'A>)
    |> Seq.tryExactlyOne
    |> Option.map (fun x -> x.TypedValue.Value :?> 'A)

  let parseAttributes (testMethod:MethodInfo) (testClass:Type) =
    let classProperties =
      testClass.CustomAttributes
      |> Seq.tryFind (fun x -> x.AttributeType = typeof<PropertiesAttribute>)
    let configType, tests =
      testMethod.CustomAttributes
      |> Seq.filter (fun x -> x.AttributeType = typeof<PropertyAttribute>)
      |> Seq.exactlyOne
      |> fun methodProperty ->
        let methodCtorT  =             ctorArg<Type>  methodProperty
        let methodNamedT =             namedArg<Type> methodProperty
        let classCtorT   = Option.bind ctorArg<Type>  classProperties
        let classNamedT  = Option.bind namedArg<Type> classProperties
        let methodCtorI  =             ctorArg<int>   methodProperty
        let methodNamedI =             namedArg<int>  methodProperty
        let classCtorI   = Option.bind ctorArg<int>   classProperties
        let classNamedI  = Option.bind namedArg<int>  classProperties
        methodCtorT ++ methodNamedT ++ classCtorT ++ classNamedT,
        methodCtorI ++ methodNamedI ++ classCtorI ++ classNamedI
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
    config, (tests |> Option.defaultValue 100 |> LanguagePrimitives.Int32WithMeasure)

  let report (testMethod:MethodInfo) testClass testClassInstance =
    let config, tests = parseAttributes testMethod testClass
    let gens =
      testMethod.GetParameters()
      |> Array.map (fun p ->
        genxAutoBoxWithMethodInfo
          .MakeGenericMethod(p.ParameterType)
          .Invoke(null, [|config|])
        :?> Gen<obj>)
      |> ArrayGen.toGenTuple
    let invoke t =
      testMethod.Invoke(testClassInstance, Microsoft.FSharp.Reflection.FSharpValue.GetTupleFields t)
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
