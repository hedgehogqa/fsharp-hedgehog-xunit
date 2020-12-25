namespace Hedgehog.Xunit.Tests

open Xunit
open System
open Hedgehog
open Hedgehog.Xunit

module Common =
  let [<Literal>] skipReason = "Skipping because it's just here to be the target of a [<Fact>] test"
  let assertMatch (e: Exception) pattern =
    System.Text.RegularExpressions.Regex.IsMatch(e.Message, pattern)
    |> Assert.True

open Common

module ``Property module tests`` =

  type private Marker = class end
  let getMethod = typeof<Marker>.DeclaringType.GetMethod
  let assertShrunk methodName expected =
    let report = PropertyHelper.report (getMethod methodName) typeof<Marker>.DeclaringType null
    match report.Status with
    | Status.Failed(_, journal) ->
      Assert.Equal(expected, Journal.toList journal |> Seq.head)
    | _ -> failwith "impossible"
    
  [<Property(Skip = skipReason)>]
  let ``fails for false, skipped`` (_: int) = false
  [<Fact>]
  let ``fails for false`` () =
    assertShrunk (nameof ``fails for false, skipped``) "(0)"

  [<Property>]
  let ``Can generate an int`` (i: int) =
    printfn "Test input: %i" i

  [<Property(Skip = skipReason)>]
  let ``Can shrink an int, skipped`` (i: int) =
    if i >= 50 then failwith "Some error."
  [<Fact>]
  let ``Can shrink an int`` () =
    assertShrunk (nameof ``Can shrink an int, skipped``) "(50)"

  [<Property>]
  let ``Can generate two ints`` (i1: int, i2: int) =
    printfn "Test input: %i, %i" i1 i2

  [<Property(Skip = skipReason)>]
  let ``Can shrink both ints, skipped`` (i1: int, i2: int) =
    if i1 >= 10 &&
       i2 >= 20 then failwith "Some error."
  [<Fact>]
  let ``Can shrink both ints`` () =
    assertShrunk (nameof ``Can shrink both ints, skipped``) "(10, 20)"
  
  [<Property>]
  let ``Can generate an int and string`` (i: int, s: string) =
    printfn "Test input: %i, %s" i s
  
  [<Property(Skip = skipReason)>]
  let ``Can shrink an int and string, skipped`` (i: int, s: string) =
    if i >= 2 && s.Contains "b" then failwith "Some error."
  [<Fact>]
  let ``Can shrink an int and string`` () =
    assertShrunk (nameof ``Can shrink an int and string, skipped``) "(2, \"b\")"

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
    
type Int13 = static member __ = { GenX.defaults with Int = Gen.constant 13 }

module ``Property module with AutoGenConfig tests`` =

  module NormalTests =

    [<Property(                typeof<Int13>)>]
    let ``Uses custom Int gen``                i = i = 13
    [<Property(AutoGenConfig = typeof<Int13>)>]
    let ``Uses custom Int gen with named arg`` i = i = 13

  module FailingTests =

    type private Marker = class end

    type NonstaticProperty = member _.__ = GenX.defaults
    [<Property(typeof<NonstaticProperty>, skipReason)>]
    let ``Instance property fails, skipped`` () = ()
    [<Fact>]
    let ``Instance property fails`` () =
      let testMethod = typeof<Marker>.DeclaringType.GetMethod(nameof ``Instance property fails, skipped``)
      let e = Assert.Throws<Exception>(fun () -> PropertyHelper.getConfig testMethod typeof<Marker>.DeclaringType |> ignore)
      Assert.Equal("Hedgehog.Xunit.Tests.Property module with AutoGenConfig tests+FailingTests+NonstaticProperty must have exactly one static property that returns an AutoGenConfig.

An example type definition:

type NonstaticProperty =
  static member __ =
    { GenX.defaults with
        Int = Gen.constant 13 }
", e.Message)

    type NonAutoGenConfig = static member __ = ()
    [<Property(typeof<NonAutoGenConfig>, skipReason)>]
    let ``Non AutoGenConfig static property fails, skipped`` () = ()
    [<Fact>]
    let ``Non AutoGenConfig static property fails`` () =
      let testMethod = typeof<Marker>.DeclaringType.GetMethod(nameof ``Non AutoGenConfig static property fails, skipped``)
      let e = Assert.Throws<Exception>(fun () -> PropertyHelper.getConfig testMethod typeof<Marker>.DeclaringType |> ignore)
      Assert.Equal("Hedgehog.Xunit.Tests.Property module with AutoGenConfig tests+FailingTests+NonAutoGenConfig must have exactly one static property that returns an AutoGenConfig.

An example type definition:

type NonAutoGenConfig =
  static member __ =
    { GenX.defaults with
        Int = Gen.constant 13 }
", e.Message)

type Int2718 = static member __ = { GenX.defaults with Int = Gen.constant 2718 }

[<Properties(typeof<Int13>)>]
module ``Module with <Properties> tests`` =

  [<Property>]
  let ``Module <Properties> works`` (i: int) =
    i = 13

  [<Property(typeof<Int2718>)>]
  let ``Module <Properties> is overriden by Method <Property>`` (i: int) =
    i = 2718

[<Properties(typeof<Int13>)>]
type ``Class with <Properties> tests``(output: Xunit.Abstractions.ITestOutputHelper) =

  [<Property>]
  let ``Class <Properties> works`` (i: int) =
    i = 13

  [<Property(typeof<Int2718>)>]
  let ``Class <Properties> is overriden by Method level <Property>`` (i: int) =
    i = 2718
