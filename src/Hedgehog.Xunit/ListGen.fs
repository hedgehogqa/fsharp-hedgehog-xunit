module internal ListGen // https://github.com/hedgehogqa/fsharp-hedgehog/pull/260

open Hedgehog

let traverse (f: 'a -> Gen<'b>) (ma: list<'a>) : Gen<list<'b>> =
    let rec loop input output =
        match input with
        | [] -> output |> List.rev |> Gen.constant
        | a :: input ->
            gen {
                let! b = f a
                return! loop input (b :: output)
            }
    loop ma []

let sequence ma = traverse id ma 
