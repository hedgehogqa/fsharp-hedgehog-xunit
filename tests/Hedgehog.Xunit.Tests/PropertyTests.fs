namespace Hedgehog.Xunit.Tests

open Hedgehog.Xunit

module PropertyModuleTests =

  [<Property>]
  [<AssertExceptionRegex("""\*\*\* Failed! Falsifiable \(after 1 test\):\s+\(0\)""")>]
  let ``fails for false`` (_: int) =
    false

  [<Property>]
  let ``Can generate an int`` (i: int) =
    printfn "Test input: %i" i

  [<Property>]
  [<AssertExceptionRegex("""\*\*\* Failed! Falsifiable \(after \d+ tests(?: and \d+ shrinks?)?\):\s+\(50\)""")>]
  let ``Can shrink an int`` (i: int) =
    if i >= 50 then failwith "Some error."

  [<Property>]
  let ``Can generate two ints`` (i1: int, i2: int) =
    printfn "Test input: %i, %i" i1 i2

  [<Property>]
  [<AssertExceptionRegex("""\*\*\* Failed! Falsifiable \(after \d+ tests(?: and \d+ shrinks?)?\):\s+\(10, 20\)""")>]
  let ``Can shrink both ints`` (i1: int, i2: int) =
    if i1 >= 10 &&
       i2 >= 20 then failwith "Some error."
  
  [<Property>]
  let ``Can generate an int and string`` (i: int, s: string) =
    printfn "Test input: %i, %s" i s
  
  [<Property>]
  [<AssertExceptionRegex("""\*\*\* Failed! Falsifiable \(after \d+ tests(?: and \d+ shrinks?)?\):\s+\(2, \"b\"\)""")>]
  let ``Can shrink an int and string`` (i: int, s: string) =
    if i >= 2 && s.Contains "b" then failwith "Some error."

type PropertyClassTests(output: Xunit.Abstractions.ITestOutputHelper) =

  [<Property>]
  [<AssertExceptionRegex("""\*\*\* Failed! Falsifiable \(after 1 test\):\s+\(0\)""")>]
  let ``fails for false`` (_: int) =
    false
  
  [<Property>]
  let ``Can generate an int`` (i: int) =
    sprintf "Test input: %i" i |> output.WriteLine
  
  [<Property>]
  [<AssertExceptionRegex("""\*\*\* Failed! Falsifiable \(after \d+ tests(?: and \d+ shrinks?)?\):\s+\(50\)""")>]
  let ``Can shrink an int`` (i: int) =
    if i >= 50 then failwith "Some error."
  
  [<Property>]
  let ``Can generate two ints`` (i1: int, i2: int) =
    sprintf "Test input: %i, %i" i1 i2 |> output.WriteLine
  
  [<Property>]
  [<AssertExceptionRegex("""\*\*\* Failed! Falsifiable \(after \d+ tests(?: and \d+ shrinks?)?\):\s+\(10, 20\)""")>]
  let ``Can shrink both ints`` (i1: int, i2: int) =
    if i1 >= 10 &&
       i2 >= 20 then failwith "Some error."
    
  [<Property>]
  let ``Can generate an int and string`` (i: int, s: string) =
    sprintf "Test input: %i, %s" i s |> output.WriteLine
    
  [<Property>]
  [<AssertExceptionRegex("""\*\*\* Failed! Falsifiable \(after \d+ tests(?: and \d+ shrinks?)?\):\s+\(2, \"b\"\)""")>]
  let ``Can shrink an int and string`` (i: int, s: string) =
    if i >= 2 && s.Contains "b" then failwith "Some error."

module ``Property with AutoGenConfig tests`` =
  open Xunit
  open Hedgehog
  open System

  module NormalTests =

    type Int13 = static member __ = { GenX.defaults with Int = Gen.constant 13 }
    [<Property(                typeof<Int13>)>]
    let ``Uses custom Int gen``                i = i = 13
    [<Property(AutoGenConfig = typeof<Int13>)>]
    let ``Uses custom Int gen with named arg`` i = i = 13

  module FailingTests =

    type private Marker = class end
    let [<Literal>] skipReason = "Skipping because it's just here to be the target of a [<Fact>] test"

    type NonstaticProperty = member _.__ = GenX.defaults
    [<Property(typeof<NonstaticProperty>, skipReason)>]
    let ``Instance property fails, skipped`` () = ()
    [<Fact>]
    let ``Instance property fails`` () =
      let testMethod = typeof<Marker>.DeclaringType.GetMethod(nameof(``Instance property fails, skipped``))
      let e = Assert.Throws<Exception>(fun () -> PropertyHelper.check testMethod null)
      Assert.Equal(e.Message, "Hedgehog.Xunit.Tests.Property with AutoGenConfig tests+FailingTests+NonstaticProperty must have exactly one static property that returns an AutoGenConfig")

    type NonAutoGenConfig = static member __ = ()
    [<Property(typeof<NonAutoGenConfig>, skipReason)>]
    let ``Non AutoGenConfig static property fails, skipped`` () = ()
    [<Fact>]
    let ``Non AutoGenConfig static property fails`` () =
      let testMethod = typeof<Marker>.DeclaringType.GetMethod(nameof(``Non AutoGenConfig static property fails, skipped``))
      let e = Assert.Throws<Exception>(fun () -> PropertyHelper.check testMethod null)
      Assert.Equal(e.Message, "Hedgehog.Xunit.Tests.Property with AutoGenConfig tests+FailingTests+NonAutoGenConfig must have exactly one static property that returns an AutoGenConfig")
