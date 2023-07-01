namespace Hedgehog.Xunit.Tests

open Xunit
open System
open Hedgehog
open Hedgehog.Xunit

module Common =
  let [<Literal>] skipReason = "Skipping because it's just here to be the target of a [<Fact>] test"

open Common

type Int13 = static member __ = GenX.defaults |> AutoGenConfig.addGenerator (Gen.constant 13)

module ``Property module tests`` =

  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod
  let assertShrunk methodName expected =
    let report = InternalLogic.report (getMethod methodName) typeof<Marker>.DeclaringType null
    match report.Status with
    | Status.Failed r ->
      Assert.Equal(expected, r.Journal |> Journal.eval |> Seq.head)
    | _ -> failwith "impossible"
    
  [<Property(Skip = skipReason)>]
  let ``fails for false, skipped`` (_: int) = false
  [<Fact>]
  let ``fails for false`` () =
    assertShrunk (nameof ``fails for false, skipped``) "[0]"

  [<Property(Skip = skipReason)>]
  let ``Result with Error shrinks, skipped`` (i: int) =
    if i > 10 then
      Error ()
    else
      Ok ()
  [<Fact>]
  let ``Result with Error shrinks`` () =
    assertShrunk (nameof ``Result with Error shrinks, skipped``) "[11]"

  [<Property(Skip = skipReason)>]
  let ``Result with Error reports exception with Error value, skipped`` (i: int) =
    if i > 10 then
      Error "Too many digits!"
    else
      Ok ()
  [<Fact>]
  let ``Result with Error reports exception with Error value`` () =
    let report = InternalLogic.report (nameof ``Result with Error reports exception with Error value, skipped`` |> getMethod) typeof<Marker>.DeclaringType null
    match report.Status with
    | Status.Failed r ->
      let errorMessage = r.Journal |> Journal.eval |> Seq.skip 1 |> Seq.exactlyOne
      Assert.Contains($"System.Exception: Result is in the Error case with the following value:{Environment.NewLine}\"Too many digits!\"", errorMessage)
    | _ -> failwith "impossible"

  [<Property>]
  let ``Can generate an int`` (i: int) =
    printfn "Test input: %i" i

  [<Property(Skip = skipReason)>]
  let ``Can shrink an int, skipped`` (i: int) =
    if i >= 50 then failwith "Some error."
  [<Fact>]
  let ``Can shrink an int`` () =
    assertShrunk (nameof ``Can shrink an int, skipped``) "[50]"

  [<Property>]
  let ``Can generate two ints`` (i1: int, i2: int) =
    printfn "Test input: %i, %i" i1 i2

  [<Property(Skip = skipReason)>]
  let ``Can shrink both ints, skipped`` (i1: int, i2: int) =
    if i1 >= 10 &&
       i2 >= 20 then failwith "Some error."
  [<Fact>]
  let ``Can shrink both ints`` () =
    assertShrunk (nameof ``Can shrink both ints, skipped``) "[10; 20]"
  
  [<Property>]
  let ``Can generate an int and string`` (i: int, s: string) =
    printfn "Test input: %i, %s" i s
  
  [<Property(Tests = 1000<tests>, Skip = skipReason)>]
  let ``Can shrink an int and string, skipped`` (i: int, s: string) =
    if i >= 2 && s.Contains "b" then failwith "Some error."
  [<Fact>]
  let ``Can shrink an int and string`` () =
    assertShrunk (nameof ``Can shrink an int and string, skipped``) "[2; \"b\"]"

  [<Property(typeof<Int13>, 1<tests>)>]
  let ``runs with 13 once`` () = ()
  [<Fact>]
  let ``Tests 'runs with 13 once'`` () =
    let config, tests, shrinks, _, _ = InternalLogic.parseAttributes (nameof ``runs with 13 once`` |> getMethod) typeof<Marker>.DeclaringType
    Assert.Equal(None, shrinks)
    Assert.Equal(Some 1<tests>, tests)
    let generated = GenX.autoWith config |> Gen.sample 1 1 |> Seq.exactlyOne
    Assert.Equal(13, generated)

  type CustomRecord = { Herp: int; Derp: string }
  [<Property>]
  let ``Works up to 26 parameters`` (
                                    a: string,
                                    b: char,
                                    c: double,
                                    d: bool,
                                    e: DateTime,
                                    f: DateTimeOffset,
                                    g: string list,
                                    h: char list,
                                    i: int,
                                    j: int array,
                                    k: char array,
                                    l: DateTime array,
                                    m: DateTimeOffset array,
                                    n: CustomRecord option,
                                    o: DateTime option,
                                    p: Result<string, string>,
                                    q: Result<string, int>,
                                    r: Result<int, int>,
                                    s: Result<int, string>,
                                    t: Result<DateTime, string>,
                                    u: Result<CustomRecord, string>,
                                    v: Result<DateTimeOffset, DateTimeOffset>,
                                    w: Result<double, DateTimeOffset>,
                                    x: Result<double, bool>,
                                    y: CustomRecord,
                                    z: int list list) =
    printfn "%A %A %A %A %A %A %A %A %A %A %A %A %A %A %A %A %A %A %A %A %A %A %A %A %A %A "
      a b c d e f g h i j k l m n o p q r s t u v w x y z

  [<Property>]
  let ``multiple unresolved generics works`` _ _ = ()

  [<Property>]
  let ``mixed unresolved generics works 1`` (_: int) _ = ()

  [<Property>]
  let ``mixed unresolved generics works 2`` _ (_: int) = ()

  [<Property>]
  let ``unresolved nested generics works`` (_: _ list) (_: Result<_, _>) = ()

  [<Property>]
  let ``mixed nested generics works`` (_: int) _ (_: _ list) (_: Result<_, _>) = ()

  [<Property>]
  let ``returning unresolved generic works`` (x: 'a) = x

  [<Property>]
  let ``returning unresolved nested generic works`` () : Result<unit, 'a> = Ok ()

  [<Property>]
  let ``0 parameters passes`` () = ()


type ``Property class tests``(output: Xunit.Abstractions.ITestOutputHelper) =

  [<Property>]
  let ``Can generate an int`` (i: int) =
    sprintf "Test input: %i" i |> output.WriteLine
  
  [<Property>]
  let ``Can generate two ints`` (i1: int, i2: int) =
    sprintf "Test input: %i, %i" i1 i2 |> output.WriteLine
  
  [<Property>]
  let ``Can generate an int and string`` (i: int, s: string) =
    sprintf "Test input: %i, %s" i s |> output.WriteLine

type ConfigArg = static member __ a = GenX.defaults |> AutoGenConfig.addGenerator (Gen.constant a)

[<Properties(AutoGenConfig = typeof<ConfigArg>, AutoGenConfigArgs = [|'a'|])>]
module ``AutoGenConfigArgs tests`` =

  let [<Property>] ``PropertiesAttribute passes its AutoGenConfigArgs`` a = a = 'a'

  let config a b =
    GenX.defaults
    |> AutoGenConfig.addGenerator (Gen.constant a)
    |> AutoGenConfig.addGenerator (Gen.constant b)
  type ConfigGenericArgs = static member __ (a: 'a)     (b: 'b)  = config a b
  type ConfigArgs        = static member __ (a: string) (b: int) = config a b
  type ConfigMixedArgsA  = static member __ (a: 'a)     (b: int) = config a b
  type ConfigMixedArgsB  = static member __ (a: string) (b: 'b)  = config a b

  let test s i = s = "foo" && i = 13
  let [<Property(AutoGenConfig = typeof<ConfigGenericArgs>, AutoGenConfigArgs = [|"foo"; 13|])>] ``all generics``      s i = test s i
  let [<Property(AutoGenConfig = typeof<ConfigArgs>       , AutoGenConfigArgs = [|"foo"; 13|])>] ``all non-generics``  s i = test s i
  let [<Property(AutoGenConfig = typeof<ConfigMixedArgsA> , AutoGenConfigArgs = [|"foo"; 13|])>] ``mixed generics, 1`` s i = test s i
  let [<Property(AutoGenConfig = typeof<ConfigMixedArgsB> , AutoGenConfigArgs = [|"foo"; 13|])>] ``mixed generics, 2`` s i = test s i

module ``Property module with AutoGenConfig tests`` =

  module NormalTests =

    [<Property(                typeof<Int13>)>]
    let ``Uses custom Int gen``                i = i = 13
    [<Property(AutoGenConfig = typeof<Int13>)>]
    let ``Uses custom Int gen with named arg`` i = i = 13

  module FailingTests =
    type private Marker = class end

    type NonstaticProperty = member _.__ = GenX.defaults
    [<Property(typeof<NonstaticProperty>, Skip = skipReason)>]
    let ``Instance property fails, skipped`` () = ()
    [<Fact>]
    let ``Instance property fails`` () =
      let testMethod = typeof<Marker>.DeclaringType.GetMethod(nameof ``Instance property fails, skipped``)
      let e = Assert.Throws<Exception>(fun () -> InternalLogic.parseAttributes testMethod typeof<Marker>.DeclaringType |> ignore)
      Assert.Equal("Hedgehog.Xunit.Tests.Property module with AutoGenConfig tests+FailingTests+NonstaticProperty must have exactly one static property that returns an AutoGenConfig.

An example type definition:

type NonstaticProperty =
  static member __ =
    GenX.defaults |> AutoGenConfig.addGenerator (Gen.constant 13)
", e.Message)

    type NonAutoGenConfig = static member __ = ()
    [<Property(typeof<NonAutoGenConfig>, Skip = skipReason)>]
    let ``Non AutoGenConfig static property fails, skipped`` () = ()
    [<Fact>]
    let ``Non AutoGenConfig static property fails`` () =
      let testMethod = typeof<Marker>.DeclaringType.GetMethod(nameof ``Non AutoGenConfig static property fails, skipped``)
      let e = Assert.Throws<Exception>(fun () -> InternalLogic.parseAttributes testMethod typeof<Marker>.DeclaringType |> ignore)
      Assert.Equal("Hedgehog.Xunit.Tests.Property module with AutoGenConfig tests+FailingTests+NonAutoGenConfig must have exactly one static property that returns an AutoGenConfig.

An example type definition:

type NonAutoGenConfig =
  static member __ =
    GenX.defaults |> AutoGenConfig.addGenerator (Gen.constant 13)
", e.Message)

type Int2718 = static member __ = GenX.defaults |> AutoGenConfig.addGenerator (Gen.constant 2718)

[<Properties(typeof<Int13>, 200<tests>)>]
module ``Module with <Properties> tests`` =

  [<Property>]
  let ``Module <Properties> works`` (i: int) =
    i = 13

  [<Property(typeof<Int2718>)>]
  let ``Module <Properties> is overriden by Method <Property>`` (i: int) =
    i = 2718

  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod

  [<Fact>]
  let ``Module <Properties> tests (count) works`` () =
    let testMethod = getMethod (nameof ``Module <Properties> works``)
    let _, tests, _, _, _ = InternalLogic.parseAttributes testMethod typeof<Marker>.DeclaringType
    Assert.Equal(Some 200<tests>, tests)

  [<Property(300<tests>)>]
  let ``Module <Properties> tests (count) is overriden by Method <Property>, skipped`` (_: int) = ()
  [<Fact>]
  let ``Module <Properties> tests (count) is overriden by Method <Property>`` () =
    let testMethod = getMethod (nameof ``Module <Properties> tests (count) is overriden by Method <Property>, skipped``)
    let _, tests, _, _, _ = InternalLogic.parseAttributes testMethod typeof<Marker>.DeclaringType
    Assert.Equal(Some 300<tests>, tests)


[<Properties(typeof<Int13>)>]
type ``Class with <Properties> tests``(output: Xunit.Abstractions.ITestOutputHelper) =

  [<Property>]
  let ``Class <Properties> works`` (i: int) =
    i = 13

  [<Property(typeof<Int2718>)>]
  let ``Class <Properties> is overriden by Method level <Property>`` (i: int) =
    i = 2718


type PropertyInt13Attribute() = inherit PropertyAttribute(typeof<Int13>)
module ``Property inheritance tests`` =
  [<PropertyInt13>]
  let ``Property inheritance works`` (i: int) =
    i = 13


type PropertiesInt13Attribute() = inherit PropertiesAttribute(typeof<Int13>)
[<PropertiesInt13>]
module ``Properties inheritance tests`` =
  [<Property>]
  let ``Properties inheritance works`` (i: int) =
    i = 13


[<Properties(Tests = 1<tests>, AutoGenConfig = typeof<Int13>)>]
module ``Properties named arg tests`` =
  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod
  [<Property>]
  let ``runs once with 13`` () = ()
  [<Fact>]
  let ``Tests 'runs once with 13'`` () =
    let config, tests, _, _, _ = InternalLogic.parseAttributes (nameof ``runs once with 13`` |> getMethod) typeof<Marker>.DeclaringType
    Assert.Equal(Some 1<tests>, tests)
    let generated = GenX.autoWith config |> Gen.sample 1 1 |> Seq.exactlyOne
    Assert.Equal(13, generated)


[<Properties(1<tests>)>]
module ``Properties (tests count) tests`` =
  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod
  [<Property>]
  let ``runs once`` () = ()
  [<Fact>]
  let ``Tests 'runs once'`` () =
    let _, tests, _, _, _ = InternalLogic.parseAttributes (nameof ``runs once`` |> getMethod) typeof<Marker>.DeclaringType
    Assert.Equal(Some 1<tests>, tests)


module ``Asynchronous tests`` =

  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod
  let assertShrunk methodName expected =
    let report = InternalLogic.report (getMethod methodName) typeof<Marker>.DeclaringType null
    match report.Status with
    | Status.Failed r ->
      Assert.Equal(expected, r.Journal |> Journal.eval |> Seq.head)
    | _ -> failwith "impossible"

  open System.Threading.Tasks
  let Fast() =
      Task.Delay 100

  [<Property(Skip = skipReason)>]
  let ``Returning Task with exception fails, skipped`` (i: int) : Task =
    if i > 10 then
      Exception() |> Task.FromException
    else Task.Delay 100
  [<Fact>]
  let ``Returning Task with exception fails`` () =
    assertShrunk (nameof ``Returning Task with exception fails, skipped``) "[11]"

  open FSharp.Control.Tasks
  [<Property(Skip = skipReason)>]
  let ``TaskBuilder (returning Task<unit>) with exception shrinks, skipped`` (i: int) : Task<unit> =
    task {
      do! Fast()
      if i > 10 then
        raise <| Exception()
    }
  [<Fact>]
  let ``TaskBuilder (returning Task<unit>) with exception shrinks`` () =
    assertShrunk (nameof ``TaskBuilder (returning Task<unit>) with exception shrinks, skipped``) "[11]"
    
  [<Property(Skip = skipReason)>]
  let ``Async with exception shrinks, skipped`` (i: int) =
    async {
      do! Async.Sleep 100
      if i > 10 then
        raise <| Exception()
    }
  [<Fact>]
  let ``Async with exception shrinks`` () =
    assertShrunk (nameof ``Async with exception shrinks, skipped``) "[11]"

  [<Property(Skip = skipReason)>]
  let ``AsyncResult with Error shrinks, skipped`` (i: int) =
    async {
      do! Async.Sleep 100
      if i > 10 then
        return Error ()
      else
        return Ok ()
    }
  [<Fact>]
  let ``AsyncResult with Error shrinks`` () =
    assertShrunk (nameof ``AsyncResult with Error shrinks, skipped``) "[11]"

  [<Property(Skip = skipReason)>]
  let ``TaskResult with Error shrinks, skipped`` (i: int) =
    task {
      do! Task.Delay 100
      if i > 10 then
        return Error ()
      else
        return Ok ()
    }

  [<Fact>]
  let ``TaskResult with Error shrinks`` () =
    assertShrunk (nameof ``TaskResult with Error shrinks, skipped``) "[11]"

  [<Property(Skip = skipReason)>]
  let ``Non Unit TaskResult with Error shrinks, skipped`` (i: int) =
    task {
      do! Task.Delay 100
      if i > 10 then
        return Error "Test fails"
      else
        return Ok 1
    }

  [<Fact>]
  let ``Non Unit TaskResult with Error shrinks`` () =
    assertShrunk (nameof ``Non Unit TaskResult with Error shrinks, skipped``) "[11]"




module ``IDisposable test module`` =
  let mutable runs = 0
  let mutable disposes = 0

  type DisposableImplementation() =
    interface IDisposable with
      member _.Dispose() =
        disposes <- disposes + 1
  let getMethod = typeof<DisposableImplementation>.DeclaringType.GetMethod

  [<Property(Skip = skipReason)>]
  let ``IDisposable arg get disposed even if exception thrown, skipped`` (_: DisposableImplementation) (i: int) =
    runs <- runs + 1
    if i > 10 then raise <| Exception()
  [<Fact>]
  let ``IDisposable arg get disposed even if exception thrown`` () =
    let report = InternalLogic.report (getMethod (nameof ``IDisposable arg get disposed even if exception thrown, skipped``)) typeof<DisposableImplementation>.DeclaringType null
    match report.Status with
    | Status.Failed _ ->
      Assert.NotEqual(0, runs)
      Assert.Equal(runs, disposes)
    | _ -> failwith "impossible"


module ``PropertyTestCaseDiscoverer works`` =
  let mutable runs = 0
  [<Property>]
  let ``increment runs`` () =
    runs <- runs + 1

  // This assumes that ``increment runs`` runs before this test runs. The tests *seem* to run in alphabetical order.
  // https://github.com/asherber/Xunit.Priority doesn't seem to work; perhaps modules are treated differently. Ref: https://stackoverflow.com/questions/9210281/
  [<Fact>]
  let ``PropertyAttribute is discovered and run`` () =
    Assert.True(runs > 0)

module TupleTests =
  [<Fact>]
  let ``Non-Hedgehog.Xunit passes`` () =
    Property.check <| property {
      let! a, b =
        GenX.defaults
        |> AutoGenConfig.addGenerator (Gen.constant (1, 2))
        |> GenX.autoWith<int*int>
      Assert.Equal(1, a)
      Assert.Equal(2, b)
    }
  
  type CustomTupleGen = static member __ = GenX.defaults |> AutoGenConfig.addGenerator (Gen.constant (1, 2))
  [<Property(typeof<CustomTupleGen>)>]
  let ``Hedgehog.Xunit requires another param to pass`` (((a,b) : int*int), _: bool) =
    Assert.Equal(1, a)
    Assert.Equal(2, b)

module ShrinkTests =
  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod

  [<Property(100<tests>, 0<shrinks>, Skip = skipReason)>]
  let ``0 shrinks, skipped`` i =
    i < 2500
  [<Fact>]
  let ``0 shrinks`` () =
    let _, _, shrinks, _, _ = InternalLogic.parseAttributes (nameof ``0 shrinks, skipped`` |> getMethod) typeof<Marker>.DeclaringType
    Assert.Equal(Some 0<shrinks>, shrinks)

  [<Property(Shrinks = 1<shrinks>, Skip = skipReason)>]
  let ``1 shrinks, run, skipped`` i =
    i < 2500
  [<Fact>]
  let ``1 shrinks, run`` () =
    let report = InternalLogic.report (nameof ``1 shrinks, run, skipped`` |> getMethod) typeof<Marker>.DeclaringType null
    match report.Status with
    | Failed data ->
      Assert.Equal(1<shrinks>, data.Shrinks)
    | _ -> failwith "impossible"

  [<Property(typeof<Int13>, 100<tests>, 0<shrinks>, Skip = skipReason)>]
  let ``0 shrinks, run, skipped`` () : unit =
    failwith "oops"
  [<Fact>]
  let ``0 shrinks, run`` () =
    let report = InternalLogic.report (nameof ``0 shrinks, run, skipped`` |> getMethod) typeof<Marker>.DeclaringType null
    match report.Status with
    | Failed data ->
      Assert.Equal(0<shrinks>, data.Shrinks)
    | _ -> failwith "impossible"

type Forever = static member __ = GenX.defaults |> AutoGenConfig.addGenerator (Gen.constant "...")
[<Properties(typeof<Forever>, 100<tests>, 0<shrinks>)>]
module ``Module with <Properties> tests, 0 shrinks`` =
  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod

  [<Property(Skip = skipReason)>]
  let ``0 shrinks, skipped`` i =
    i < 2500
  [<Fact>]
  let ``0 shrinks`` () =
    let _, _, shrinks, _, _ = InternalLogic.parseAttributes (nameof ``0 shrinks, skipped`` |> getMethod) typeof<Marker>.DeclaringType
    Assert.Equal(Some 0<shrinks>, shrinks)

  [<Property(Shrinks = 1<shrinks>, Skip = skipReason)>]
  let ``1 shrinks, run, skipped`` i =
    i < 2500
  [<Fact>]
  let ``1 shrinks, run`` () =
    let report = InternalLogic.report (nameof ``1 shrinks, run, skipped`` |> getMethod) typeof<Marker>.DeclaringType null
    match report.Status with
    | Failed data ->
      Assert.Equal(1<shrinks>, data.Shrinks)
    | _ -> failwith "impossible"

[<Properties(Shrinks = 0<shrinks>)>]
module ``Module with <Properties> tests, 0 shrinks manual`` =
  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod

  [<Property(Skip = skipReason)>]
  let ``0 shrinks, skipped`` i =
    i < 2500
  [<Fact>]
  let ``0 shrinks`` () =
    let _, _, shrinks, _, _ = InternalLogic.parseAttributes (nameof ``0 shrinks, skipped`` |> getMethod) typeof<Marker>.DeclaringType
    Assert.Equal(Some 0<shrinks>, shrinks)

  [<Property(Shrinks = 1<shrinks>, Skip = skipReason)>]
  let ``1 shrinks, run, skipped`` i =
    i < 2500
  [<Fact>]
  let ``1 shrinks, run`` () =
    let report = InternalLogic.report (nameof ``1 shrinks, run, skipped`` |> getMethod) typeof<Marker>.DeclaringType null
    match report.Status with
    | Failed data ->
      Assert.Equal(1<shrinks>, data.Shrinks)
    | _ -> failwith "impossible"

[<Properties(100<tests>, 0<shrinks>)>]
module ``Module with <Properties> tests, whatever tests and 0 shrinks`` =
  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod

  [<Property(Skip = skipReason)>]
  let ``0 shrinks, skipped`` i =
    i < 2500
  [<Fact>]
  let ``0 shrinks`` () =
    let _, _, shrinks, _, _ = InternalLogic.parseAttributes (nameof ``0 shrinks, skipped`` |> getMethod) typeof<Marker>.DeclaringType
    Assert.Equal(Some 0<shrinks>, shrinks)

  [<Property(Shrinks = 1<shrinks>, Skip = skipReason)>]
  let ``1 shrinks, run, skipped`` i =
    i < 2500
  [<Fact>]
  let ``1 shrinks, run`` () =
    let report = InternalLogic.report (nameof ``1 shrinks, run, skipped`` |> getMethod) typeof<Marker>.DeclaringType null
    match report.Status with
    | Failed data ->
      Assert.Equal(1<shrinks>, data.Shrinks)
    | _ -> failwith "impossible"

module RecheckTests =
  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod

  let [<Literal>] expectedRecheckData = "0_16700074754810023652_2867022503662193831_"
  [<Property(Skip = skipReason)>]
  [<Recheck(expectedRecheckData)>]
  let ``recheck, skipped`` () = ()
  [<Fact>]
  let ``recheck`` () =
    let _, _, _, recheck, _ = InternalLogic.parseAttributes (nameof ``recheck, skipped`` |> getMethod) typeof<Marker>.DeclaringType
    match recheck with
    | None -> failwith "impossible"
    | Some actualRecheckData ->
      Assert.Equal(expectedRecheckData, actualRecheckData)

  let mutable runs = 0
  [<Property>]
  [<Recheck("33_6868290028601943647_16954941586199361379_10110110111101111110")>]
  let ``recheck runs once`` (i: int) =
    runs <- runs + 1
    Assert.Equal(1, runs)
    //Assert.True(i < 100) // used to generate the Recheck Data

  [<Recheck("99_99_99_")>]
  let [<Property(Size = 1, Skip = skipReason)>] ``Recheck's Size overrides Property's Size, skipped, 99`` (_: int) = false
  [<Recheck("1_1_1_")>]
  let [<Property(Size = 99, Skip = skipReason)>] ``Recheck's Size overrides Property's Size, skipped, 1`` (_: int) = false
  [<Fact>]
  let ``Recheck's Size overrides Property's Size`` () =
    let lastGennedInt test =
      let report = InternalLogic.report (test |> getMethod) typeof<Marker>.DeclaringType null
      let exn = Assert.Throws<Exception>(fun () -> InternalLogic.tryRaise report)
      System.Text.RegularExpressions.Regex.Match(exn.ToString(), "\[-?(\d+)\]").Groups.Item(1).Value |> Int32.Parse
    
    let gennedWithSize99 = nameof ``Recheck's Size overrides Property's Size, skipped, 99`` |> lastGennedInt
    gennedWithSize99 > 999_999_999 |> Assert.True

    let gennedWithSize1 = nameof ``Recheck's Size overrides Property's Size, skipped, 1`` |> lastGennedInt
    gennedWithSize1 < 100 |> Assert.True

[<Properties(Size=1)>]
module SizeTests =
  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod

  [<Property(Size = 2)>]
  let ``property size, actual`` () = ()
  [<Fact>]
  let ``property size`` () =
    let _, _, _, _, size = InternalLogic.parseAttributes (nameof ``property size, actual`` |> getMethod) typeof<Marker>.DeclaringType
    match size with
    | None -> failwith "impossible"
    | Some size ->
      Assert.Equal(2, size)

  [<Property>]
  let ``properites size, actual`` () = ()
  [<Fact>]
  let ``properites size`` () =
    let _, _, _, _, size = InternalLogic.parseAttributes (nameof ``properites size, actual`` |> getMethod) typeof<Marker>.DeclaringType
    match size with
    | None -> failwith "impossible"
    | Some size ->
      Assert.Equal(1, size)

module ``tryRaise tests`` =
  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod

  [<Property(Skip = skipReason)>]
  let ``always fails, skipped`` () = false
  [<Fact>]
  let ``always fails`` () =
    let report = InternalLogic.report (nameof ``always fails, skipped`` |> getMethod) typeof<Marker>.DeclaringType null
    let actual = Assert.Throws<Exception>(fun () -> InternalLogic.tryRaise report)
    let expectedMessage = """*** Failed! Falsifiable (after 1 test):
[]"""
    actual.Message.Contains(expectedMessage) |> Assert.True
    let expectedMessage = """
This failure can be reproduced by running:
> Property.recheck "0_"""
    actual.Message.Contains(expectedMessage) |> Assert.True

module ``returning a property runs it`` =
  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod

  [<Property(Skip = skipReason)>]
  let ``returning a passing property with internal gen passes, skipped`` () = property {
    let! a = Gen.constant 13
    Assert.Equal(13, a)
  }
  [<Fact>]
  let ``returning a passing property with internal gen passes`` () =
    let report = InternalLogic.report (nameof ``returning a passing property with internal gen passes, skipped`` |> getMethod) typeof<Marker>.DeclaringType null
    Assert.Equal(100<tests>, report.Tests)

  [<Property(Skip = skipReason)>]
  let ``returning a failing property with internal gen fails and shrinks, skipped`` () = property {
    let! a = Gen.int32 (Range.constant 1 100)
    Assert.True(a <= 50)
  }
  [<Fact>]
  let ``returning a failing property with internal gen fails and shrinks`` () =
    let report = InternalLogic.report (nameof ``returning a failing property with internal gen fails and shrinks, skipped`` |> getMethod) typeof<Marker>.DeclaringType null
    let actual = Assert.Throws<Exception>(fun () -> InternalLogic.tryRaise report)
    actual.Message.Contains("51") |> Assert.True

  [<Property(typeof<Int13>, Skip = skipReason)>]
  let ``returning a passing property with external gen passes, skipped`` i = property {
    Assert.Equal(13, i)
  }
  [<Fact>]
  let ``returning a passing property with external gen passes`` () =
    let report = InternalLogic.report (nameof ``returning a passing property with external gen passes, skipped`` |> getMethod) typeof<Marker>.DeclaringType null
    Assert.Equal(100<tests>, report.Tests)

  [<Property(Skip = skipReason)>]
  let ``returning a failing property with external gen fails and shrinks, skipped`` i = property {
    let! _50 = Gen.constant 50
    Assert.True(i <= _50)
  }
  [<Fact>]
  let ``returning a failing property with external gen fails and shrinks`` () =
    let report = InternalLogic.report (nameof ``returning a failing property with external gen fails and shrinks, skipped`` |> getMethod) typeof<Marker>.DeclaringType null
    let actual = Assert.Throws<Exception>(fun () -> InternalLogic.tryRaise report)
    actual.Message.Contains("51") |> Assert.True

  [<Property(Skip = skipReason)>]
  let ``returning a passing property<bool> with internal gen passes, skipped`` () = property {
    let! a = Gen.constant 13
    return 13 = a
  }
  [<Fact>]
  let ``returning a passing property<bool> with internal gen passes`` () =
    let report = InternalLogic.report (nameof ``returning a passing property<bool> with internal gen passes, skipped`` |> getMethod) typeof<Marker>.DeclaringType null
    Assert.Equal(100<tests>, report.Tests)

  [<Property(Skip = skipReason)>]
  let ``returning a failing property<bool> with internal gen fails and shrinks, skipped`` () = property {
    let! a = Gen.int32 (Range.constant 1 100)
    return a <= 50
  }
  [<Fact>]
  let ``returning a failing property<bool> with internal gen fails and shrinks`` () =
    let report = InternalLogic.report (nameof ``returning a failing property<bool> with internal gen fails and shrinks, skipped`` |> getMethod) typeof<Marker>.DeclaringType null
    let actual = Assert.Throws<Exception>(fun () -> InternalLogic.tryRaise report)
    actual.Message.Contains("51") |> Assert.True

  [<Property(typeof<Int13>, Skip = skipReason)>]
  let ``returning a passing property<bool> with external gen passes, skipped`` i = property {
    return 13 = i
  }
  [<Fact>]
  let ``returning a passing property<bool> with external gen passes`` () =
    let report = InternalLogic.report (nameof ``returning a passing property<bool> with external gen passes, skipped`` |> getMethod) typeof<Marker>.DeclaringType null
    Assert.Equal(100<tests>, report.Tests)

  [<Property(Skip = skipReason)>]
  let ``returning a failing property<bool> with external gen fails and shrinks, skipped`` i = property {
    let! _50 = Gen.constant 50
    return i <= _50
  }
  [<Fact>]
  let ``returning a failing property<bool> with external gen fails and shrinks`` () =
    let report = InternalLogic.report (nameof ``returning a failing property<bool> with external gen fails and shrinks, skipped`` |> getMethod) typeof<Marker>.DeclaringType null
    let actual = Assert.Throws<Exception>(fun () -> InternalLogic.tryRaise report)
    actual.Message.Contains("51") |> Assert.True
