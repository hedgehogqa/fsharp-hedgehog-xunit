namespace Hedgehog.Xunit

open System
open Xunit.Sdk
open Hedgehog

/// Generates arguments using GenX.auto (or autoWith if you provide an AutoGenConfig), then runs Property.check
[<AttributeUsage(AttributeTargets.Method ||| AttributeTargets.Property, AllowMultiple = false)>]
[<XunitTestCaseDiscoverer("XunitOverrides+PropertyTestCaseDiscoverer", "Hedgehog.Xunit")>]
type PropertyAttribute(autoGenConfig, autoGenConfigArgs, tests, shrinks, size) =
  inherit Xunit.FactAttribute()

  let mutable _autoGenConfig    : Type         option = autoGenConfig
  let mutable _autoGenConfigArgs: obj []       option = autoGenConfigArgs
  let mutable _tests            : int<tests>   option = tests
  let mutable _shrinks          : int<shrinks> option = shrinks
  let mutable _size             : Size         option = size

  new()                                   = PropertyAttribute(None              , None, None      , None        , None)
  new(tests)                              = PropertyAttribute(None              , None, Some tests, None        , None)
  new(tests, shrinks)                     = PropertyAttribute(None              , None, Some tests, Some shrinks, None)
  new(autoGenConfig)                      = PropertyAttribute(Some autoGenConfig, None, None      , None        , None)
  new(autoGenConfig:Type, tests)          = PropertyAttribute(Some autoGenConfig, None, Some tests, None        , None)
  new(autoGenConfig:Type, tests, shrinks) = PropertyAttribute(Some autoGenConfig, None, Some tests, Some shrinks, None)

  // https://github.com/dotnet/fsharp/issues/4154 sigh
  /// This requires a type with a single static member (with any name) that returns an AutoGenConfig.
  ///
  /// Example usage:
  ///
  /// ```
  ///
  /// type Int13 = static member AnyName = GenX.defaults |> AutoGenConfig.addGenerator (Gen.constant 13)
  ///
  /// [<Property(typeof<Int13>)>]
  ///
  /// let myTest (i:int) = ...
  ///
  /// ```
  member          _.AutoGenConfig     with set v = _autoGenConfig     <- Some v
  member          _.AutoGenConfigArgs with set v = _autoGenConfigArgs <- Some v
  member          _.Tests             with set v = _tests             <- Some v
  member          _.Shrinks           with set v = _shrinks           <- Some v
  member          _.Size              with set v = _size              <- Some v
  member internal _.GetAutoGenConfig             = _autoGenConfig
  member internal _.GetAutoGenConfigArgs         = _autoGenConfigArgs
  member internal _.GetTests                     = _tests
  member internal _.GetShrinks                   = _shrinks
  member internal _.GetSize                      = _size


/// Set a default AutoGenConfig or <tests> for all [<Property>] attributed methods in this class/module
[<AttributeUsage(AttributeTargets.Class, AllowMultiple = false)>]
type PropertiesAttribute(autoGenConfig, autoGenConfigArgs, tests, shrinks, size) =
  inherit Attribute() // don't inherit Property - it exposes members like Skip which are unsupported

  let mutable _autoGenConfig    : Type         option = autoGenConfig
  let mutable _autoGenConfigArgs: obj []       option = autoGenConfigArgs
  let mutable _tests            : int<tests>   option = tests
  let mutable _shrinks          : int<shrinks> option = shrinks
  let mutable _size             : Size         option = size

  new()                                   = PropertiesAttribute(None              , None, None      , None        , None)
  new(tests)                              = PropertiesAttribute(None              , None, Some tests, None        , None)
  new(tests, shrinks)                     = PropertiesAttribute(None              , None, Some tests, Some shrinks, None)
  new(autoGenConfig)                      = PropertiesAttribute(Some autoGenConfig, None, None      , None        , None)
  new(autoGenConfig:Type, tests)          = PropertiesAttribute(Some autoGenConfig, None, Some tests, None        , None)
  new(autoGenConfig:Type, tests, shrinks) = PropertiesAttribute(Some autoGenConfig, None, Some tests, Some shrinks, None)

  // https://github.com/dotnet/fsharp/issues/4154 sigh
  /// This requires a type with a single static member (with any name) that returns an AutoGenConfig.
  ///
  /// Example usage:
  ///
  /// ```
  ///
  /// type Int13 = static member AnyName = GenX.defaults |> AutoGenConfig.addGenerator (Gen.constant 13)
  ///
  /// [<Property(typeof<Int13>)>]
  ///
  /// let myTest (i:int) = ...
  ///
  /// ```
  member          _.AutoGenConfig     with set v = _autoGenConfig     <- Some v
  member          _.AutoGenConfigArgs with set v = _autoGenConfigArgs <- Some v
  member          _.Tests             with set v = _tests             <- Some v
  member          _.Shrinks           with set v = _shrinks           <- Some v
  member          _.Size              with set v = _size              <- Some v
  member internal _.GetAutoGenConfig             = _autoGenConfig
  member internal _.GetAutoGenConfigArgs         = _autoGenConfigArgs
  member internal _.GetTests                     = _tests
  member internal _.GetShrinks                   = _shrinks
  member internal _.GetSize                      = _size

/// Runs Property.reportRecheck
[<AttributeUsage(AttributeTargets.Method ||| AttributeTargets.Property, AllowMultiple = false)>]
type RecheckAttribute(recheckData) =
  inherit Attribute()

  let _recheckData : string = recheckData

  member internal _.GetRecheckData = _recheckData

/// Set a Generator for a parameter of a test annotated with `Property`
///
/// Example usage:
///
/// ```
///
/// type ConstantInt(i: int) =
///   inherit ParameterGeneraterBaseType<int>()
///   override _.Generator = Gen.constant i
///
/// [<Property>]
/// let ``is always 2`` ([<ConstantInt(2)>] i) =
///   Assert.StrictEqual(2, i)
///
/// ```
[<AbstractClass>]
[<AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)>]
type ParameterGeneraterBaseType<'a>() =
  inherit Attribute()
  
  abstract member Generator : Gen<'a> 
  member this.Box() = this.Generator |> Gen.map box
