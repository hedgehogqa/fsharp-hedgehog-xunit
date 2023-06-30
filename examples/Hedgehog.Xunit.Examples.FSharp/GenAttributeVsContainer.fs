module GenAttributeVsContainer

open System
open Hedgehog
open Hedgehog.Xunit

let positiveInt() = Range.constant 1 Int32.MaxValue |> Gen.int32 
let negativeInt() = Range.constant Int32.MinValue 1 |> Gen.int32

module ``With AutoGenConfig Container`` =
  type PositiveInt = { Value: int }
  type NegativeInt = { Value: int }

  type AutoGenConfigContainer =
    static member __ =
      GenX.defaults
        |> AutoGenConfig.addGenerator (positiveInt() |> Gen.map(fun x -> { PositiveInt.Value = x }))
        |> AutoGenConfig.addGenerator (negativeInt() |> Gen.map(fun x -> { NegativeInt.Value = x }))

  [<Property(typeof<AutoGenConfigContainer>)>]
  let ``Positive + Negative < Positive`` (positive: PositiveInt) (negative: NegativeInt) =
    positive.Value + negative.Value < positive.Value

module ``With GenAttribute`` =
  type PosInt() =
    inherit GenAttribute<int>()
    override _.Generator = positiveInt()

  type NegInt() =
    inherit GenAttribute<int>()
    override _.Generator = negativeInt()

  [<Property>]
  let ``Positive + Negative < Positive`` ([<PosInt>] positive) ([<NegInt>] negative) =
    positive + negative < positive

module ``With Parameterized GenAttribute`` =
  type IntRange(min: int32, max: int32) =
    inherit GenAttribute<int>()
    override _.Generator = Range.constant min max |> Gen.int32

  [<Property>]
  let ``Positive + Negative < Positive, parameterized``
    ([<IntRange(0, Int32.MaxValue)>] positive)
    ([<IntRange(Int32.MinValue, 0)>] negative) =
    positive + negative < positive
