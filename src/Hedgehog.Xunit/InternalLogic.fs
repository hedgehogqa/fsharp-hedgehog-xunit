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

type private Marker = class end
let private genxAutoBoxWith<'T> x = x |> GenX.autoWith<'T> |> Gen.map box
let private genxAutoBoxWithMethodInfo =
  typeof<Marker>.DeclaringType.GetTypeInfo().GetDeclaredMethod(nameof genxAutoBoxWith)

let parseAttributes (testMethod:MethodInfo) (testClass:Type) =
  let classAutoGenConfig, classAutoGenConfigArgs, classTests, classShrinks, classSize =
    testClass.GetCustomAttributes(typeof<PropertiesAttribute>)
    |> Seq.tryExactlyOne
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
    |> Seq.tryExactlyOne
    |> Option.map (fun x ->
      x
      :?> RecheckAttribute
      |> fun x -> x.GetSize, { Value = x.GetValue; Gamma = x.GetGamma }
    )
  let config =
    match configType with
    | None -> GenX.defaults
    | Some t ->
      t.GetMethods()
      |> Seq.filter (fun p ->
        p.IsStatic &&
        p.ReturnType = typeof<AutoGenConfig>
      ) |> Seq.tryExactlyOne
      |> Option.requireSome $"{t.FullName} must have exactly one static property that returns an {nameof AutoGenConfig}.

An example type definition:

type {t.Name} =
  static member __ =
    GenX.defaults |> AutoGenConfig.addGenerator (Gen.constant 13)
"       |> fun methodInfo ->
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
  | Error e -> failwith $"Result is in the Error case with the following value:{Environment.NewLine}%A{e}"

open System.Threading.Tasks
open System.Threading
open System.Linq

let rec toProperty (x: obj) =
  match x with
  | :? bool        as b -> Property.ofBool b
  | :? Task<unit>  as t -> Async.AwaitTask t |> toProperty
  | _ when x <> null && x.GetType().IsGenericType && x.GetType().GetGenericTypeDefinition().IsSubclassOf typeof<Task> ->
    typeof<Async>
      .GetMethods()
      .First(fun x -> x.Name = nameof Async.AwaitTask && x.IsGenericMethod)
      .MakeGenericMethod(x.GetType().GetGenericArguments())
      .Invoke(null, [|x|])
    |> toProperty
  | :? Task        as t -> Async.AwaitTask t |> toProperty
  | :? Async<unit> as a -> Async.RunSynchronously(a, cancellationToken = CancellationToken.None) |> toProperty
  | _ when x <> null && x.GetType().IsGenericType && x.GetType().GetGenericTypeDefinition() = typedefof<Async<_>> ->
    typeof<Async> // Invoked with Reflection because we can't cast an Async<MyType> to Async<obj> https://stackoverflow.com/a/26167206
      .GetMethod(nameof Async.RunSynchronously)
      .MakeGenericMethod(x.GetType().GetGenericArguments())
      .Invoke(null, [| x; None; Some CancellationToken.None |])
    |> toProperty
  | _ when x <> null && x.GetType().IsGenericType && x.GetType().GetGenericTypeDefinition() = typedefof<Result<_,_>> ->
    typeof<Marker>
      .DeclaringType
      .GetTypeInfo()
      .GetDeclaredMethod(nameof resultIsOk)
      .MakeGenericMethod(x.GetType().GetGenericArguments())
      .Invoke(null, [|x|])
    |> toProperty
  | _                   -> Property.success ()

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
    match size with
    | Some size -> gens |> Gen.resize size
    | None      -> gens
  let invoke args =
    try
      ( if testMethod.ContainsGenericParameters then
          Array.create
            (testMethod.GetGenericArguments().Length)
            (typeof<obj>)
          |> fun x -> testMethod.MakeGenericMethod x
        else
          testMethod
      ) |> fun x -> x.Invoke(testClassInstance, args |> Array.ofList)
      |> toProperty
    finally
      List.iter dispose args
  let config =
    PropertyConfig.defaultConfig
    |> withTests tests
    |> withShrinks shrinks
  Property.forAll invoke gens
  |> match recheck with
     | Some (size, seed) -> Property.reportRecheckWith size seed config
     | None              -> Property.reportWith config
