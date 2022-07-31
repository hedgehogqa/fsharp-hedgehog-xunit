module internal InternalLogic

open Hedgehog

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
open System
open Hedgehog.Xunit

// https://github.com/dotnet/fsharp/blob/b9942004e8ba19bf73862b69b2d71151a98975ba/src/FSharp.Core/list.fs#L861-L865
let listTryExactlyOne (list: _ list) =
  match list with
  | [ x ] -> Some x
  | _ -> None

// https://github.com/dotnet/fsharp/blob/b9942004e8ba19bf73862b69b2d71151a98975ba/src/FSharp.Core/seqcore.fs#L172-L174
let inline checkNonNull argName arg =
  if isNull arg then
    nullArg argName

// https://github.com/dotnet/fsharp/blob/b9942004e8ba19bf73862b69b2d71151a98975ba/src/FSharp.Core/seq.fs#L1710-L1719
let seqTryExactlyOne (source: seq<_>) =
  checkNonNull "source" source
  use e = source.GetEnumerator()

  if e.MoveNext() then
    let v = e.Current
    if e.MoveNext() then None else Some v
  else
    None

type private Marker = class end // helps with using System.Reflection
let private genxAutoBoxWith<'T> x = x |> GenX.autoWith<'T> |> Gen.map box
let private genxAutoBoxWithMethodInfo =
  typeof<Marker>.DeclaringType.GetTypeInfo().GetDeclaredMethod "genxAutoBoxWith"

let parseAttributes (testMethod:MethodInfo) (testClass:Type) =
  let classAutoGenConfig, classAutoGenConfigArgs, classTests, classShrinks, classSize =
    testClass.GetCustomAttributes(typeof<PropertiesAttribute>)
    |> seqTryExactlyOne
    |> Option.map (fun x -> x :?> PropertiesAttribute)
    |> function
    | Some x -> x.GetAutoGenConfig, x.GetAutoGenConfigArgs, x.GetTests, x.GetShrinks, x.GetSize
    | None   -> None              , None                  , None      , None        , None
  let configType, configArgs, tests, shrinks, size =
    typeof<PropertyAttribute>
    |> testMethod.GetCustomAttributes
    |> Seq.exactlyOne
    :?> PropertyAttribute
    |> fun methodAttribute ->
      methodAttribute.GetAutoGenConfig     ++ classAutoGenConfig                                ,
      methodAttribute.GetAutoGenConfigArgs ++ classAutoGenConfigArgs |> Option.defaultValue [||],
      methodAttribute.GetTests             ++ classTests                                        ,
      methodAttribute.GetShrinks           ++ classShrinks                                      ,
      methodAttribute.GetSize              ++ classSize
  let recheck =
    typeof<RecheckAttribute>
    |> testMethod.GetCustomAttributes
    |> seqTryExactlyOne
    |> Option.map (fun x ->
      x
      :?> RecheckAttribute
      |> fun x -> x.GetRecheckData
    )
  let config =
    match configType with
    | None -> GenX.defaults
    | Some t ->
      t.GetMethods()
      |> Seq.filter (fun p ->
        p.IsStatic &&
        p.ReturnType = typeof<AutoGenConfig>
      ) |> seqTryExactlyOne
      |> Option.requireSome (sprintf "%s must have exactly one static property that returns an AutoGenConfig.

An example type definition:

type %s =
  static member __ =
    GenX.defaults |> AutoGenConfig.addGenerator (Gen.constant 13)
" t.FullName t.Name)
      |> fun methodInfo ->
        let methodInfo =
          if methodInfo.IsGenericMethod then
            methodInfo.GetParameters()
            |> Array.map (fun p -> p.ParameterType.IsGenericParameter)
            |> Array.zip configArgs
            |> Array.filter snd
            |> Array.map (fun (arg, _) -> arg.GetType())
            |> fun argTypes -> methodInfo.MakeGenericMethod argTypes
          else methodInfo
        methodInfo.Invoke(null, configArgs)
      :?> AutoGenConfig
  config, tests, shrinks, recheck, size

let resultIsOk r =
  match r with
  | Ok _ -> true
  | Error e -> failwithf "Result is in the Error case with the following value:%s%A" Environment.NewLine e

open System.Threading.Tasks
open System.Threading
open System.Linq

let rec yieldAndCheckReturnValue (x: obj) =
  match x with
  | :? bool        as b -> if not b then TestReturnedFalseException() |> raise
  | :? Task<unit>  as t -> Async.AwaitTask t |> yieldAndCheckReturnValue
  | _ when x <> null && x.GetType().IsGenericType && x.GetType().GetGenericTypeDefinition().IsSubclassOf typeof<Task> ->
    typeof<Async>
      .GetMethods()
      .First(fun x -> x.Name = "AwaitTask" && x.IsGenericMethod)
      .MakeGenericMethod(x.GetType().GetGenericArguments())
      .Invoke(null, [|x|])
    |> yieldAndCheckReturnValue
  | :? Task        as t -> Async.AwaitTask t |> yieldAndCheckReturnValue
  | :? Async<unit> as a -> Async.RunSynchronously(a, cancellationToken = CancellationToken.None) |> yieldAndCheckReturnValue
  | _ when x <> null && x.GetType().IsGenericType && x.GetType().GetGenericTypeDefinition() = typedefof<Async<_>> ->
    typeof<Async> // Invoked with Reflection because we can't cast an Async<MyType> to Async<obj> https://stackoverflow.com/a/26167206
      .GetMethod("RunSynchronously")
      .MakeGenericMethod(x.GetType().GetGenericArguments())
      .Invoke(null, [| x; None; Some CancellationToken.None |])
    |> yieldAndCheckReturnValue
  | _ when x <> null && x.GetType().IsGenericType && x.GetType().GetGenericTypeDefinition() = typedefof<Result<_,_>> ->
    typeof<Marker>
      .DeclaringType
      .GetTypeInfo()
      .GetDeclaredMethod("resultIsOk")
      .MakeGenericMethod(x.GetType().GetGenericArguments())
      .Invoke(null, [|x|])
    |> yieldAndCheckReturnValue
  | _                   -> ()

let dispose (o:obj) =
  match o with
  | :? IDisposable as d -> d.Dispose()
  | _ -> ()

let withTests = function
  | Some x -> PropertyConfig.withTests x
  | None -> id

let withShrinks = function
  | Some x -> PropertyConfig.withShrinks x
  | None -> PropertyConfig.withoutShrinks

let report (testMethod:MethodInfo) testClass testClassInstance =
  let config, tests, shrinks, recheck, size = parseAttributes testMethod testClass
  let gens =
    testMethod.GetParameters()
    |> Array.mapi (fun i p ->
      if p.ParameterType.ContainsGenericParameters then
        Gen.constant Unchecked.defaultof<_>
      else
        genxAutoBoxWithMethodInfo
          .MakeGenericMethod(p.ParameterType)
          .Invoke(null, [|config|])
        :?> Gen<obj>)
    |> List.ofArray
    |> ListGen.sequence
  let gens =
    match  size, recheck with
    | _        , Some _ // could pull the size out of the recheckData... but it seems like it isn't necessary? Unable to write failing test.
    | None     ,      _ -> gens
    | Some size,      _ -> gens |> Gen.resize size
  let invoke args =
    try
      ( if testMethod.ContainsGenericParameters then
          Array.create
            (testMethod.GetGenericArguments().Length)
            (typeof<obj>)
          |> fun x -> testMethod.MakeGenericMethod x
        else
          testMethod
      ) |> fun testMethod -> testMethod.Invoke(testClassInstance, args |> Array.ofList)
      // `testMethod` is the body of a method that has been decorated with the [<Property>] attribute.
      // Above, we `Invoke` `testMethod`. Invoke returns whatever `testMethod` returns.
      // The return value is piped into `yieldAndCheckReturnValue`, which awaits, or asserts that the value is true, or is in the `Ok` state, etc.
      |> yieldAndCheckReturnValue
    finally
      List.iter dispose args
  let config =
    PropertyConfig.defaultConfig
    |> withTests tests
    |> withShrinks shrinks
  PropertyBuilder.property.BindReturn(gens, invoke)
  |> match recheck with
     | Some recheckData -> Property.reportRecheckWith recheckData config
     | None             -> Property.reportWith config

let tryRaise (report : Report) : unit =
  match report.Status with
  | Failed _ -> report |> Report.render |> Exception |> raise // todo: make it print the attribute (instead of using the default Hedgehog output)
  | _ -> Report.tryRaise report
