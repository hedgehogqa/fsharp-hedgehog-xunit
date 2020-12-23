namespace Hedgehog.Xunit.Tests

open Hedgehog.Xunit

module PropertyModuleTests =

  [<Property>]
  let ``fails for false`` () =
    false

  [<Property>]
  let ``Can generate an int`` (i: int) =
    printfn "Test input: %i" i

  [<Property>]
  let ``Can shrink an int (expect 50)`` (i: int) =
    if i >= 50 then failwith "Some error."

  [<Property>]
  let ``Can generate two ints`` (i1: int, i2: int) =
    printfn "Test input: %i, %i" i1 i2

  [<Property>]
  let ``Can shrink both ints (expect 10 and 20)`` (i1: int, i2: int) =
    if i1 >= 10 &&
       i2 >= 20 then failwith "Some error."
  
  [<Property>]
  let ``Can generate an int and string`` (i: int, s: string) =
    printfn "Test input: %i, %s" i s
  
  [<Property>]
  let ``Can shrink an int and string (expect 2 and "b")`` (i: int, s: string) =
    if i >= 2 && s.Contains "b" then failwith "Some error."

type PropertyClassTests(output: Xunit.Abstractions.ITestOutputHelper) =

  [<Property>]
  let ``fails for false`` () =
    false
  
  [<Property>]
  let ``Can generate an int`` (i: int) =
    sprintf "Test input: %i" i |> output.WriteLine
  
  [<Property>]
  let ``Can shrink an int (expect 50)`` (i: int) =
    if i >= 50 then failwith "Some error."
  
  [<Property>]
  let ``Can generate two ints`` (i1: int, i2: int) =
    sprintf "Test input: %i, %i" i1 i2 |> output.WriteLine
  
  [<Property>]
  let ``Can shrink both ints (expect 10 and 20)`` (i1: int, i2: int) =
    if i1 >= 10 &&
       i2 >= 20 then failwith "Some error."
    
  [<Property>]
  let ``Can generate an int and string`` (i: int, s: string) =
    sprintf "Test input: %i, %s" i s |> output.WriteLine
    
  [<Property>]
  let ``Can shrink an int and string (expect 2 and "b")`` (i: int, s: string) =
    if i >= 2 && s.Contains "b" then failwith "Some error."

module ``Property with AutoGenConfig tests`` =
  open Hedgehog

  type Int13 = static member __ = { GenX.defaults with Int = Gen.constant 13 }
  [<Property(                typeof<Int13>)>]
  let ``Uses custom Int gen``                i = i = 13
  [<Property(AutoGenConfig = typeof<Int13>)>]
  let ``Uses custom Int gen with named arg`` i = i = 13

  type NonstaticProperty = member _.__ = GenX.defaults
  [<Property(typeof<NonstaticProperty>)>]
  let ``Instance property fails`` () = ()
