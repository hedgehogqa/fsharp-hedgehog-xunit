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
      |> Option.requireSome $"{t.FullName} must have exactly one static property that returns an {nameof AutoGenConfig}.

An example type definition:

type {t.Name} =
  static member __ =
    GenX.defaults |> AutoGenConfig.addGenerator (Gen.constant 13)
"       |> fun x -> x.GetMethod.Invoke(null, [||])
      :?> AutoGenConfig
  config, tests

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

let report (testMethod:MethodInfo) testClass testClassInstance =
  if testMethod.ReturnParameter.ParameterType.ContainsGenericParameters then
    invalidOp $"The return type '{testMethod.ReturnParameter.ParameterType.Name}' is generic, which is unsupported. Consider using a type annotation to make the return type concrete."
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
    |> List.ofArray
    |> ListGen.sequence
  let invoke args =
    try
      testMethod.Invoke(testClassInstance, args |> Array.ofList)
      |> toProperty
    finally
      List.iter dispose args
  let config =
    PropertyConfig.defaultConfig
    |> PropertyConfig.withTests tests
  Property.forAll invoke gens |> Property.reportWith config
