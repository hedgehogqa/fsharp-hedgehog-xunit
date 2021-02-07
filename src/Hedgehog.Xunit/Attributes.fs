namespace Hedgehog.Xunit

open System
open Xunit.Sdk
open Hedgehog

/// Generates arguments using GenX.auto (or autoWith if you provide an AutoGenConfig), then runs Property.check
[<AttributeUsage(AttributeTargets.Method ||| AttributeTargets.Property, AllowMultiple = false)>]
[<XunitTestCaseDiscoverer("XunitOverrides+PropertyTestCaseDiscoverer", "Hedgehog.Xunit")>]
type PropertyAttribute(autoGenConfig, tests, skip) =
  inherit Xunit.FactAttribute(Skip = skip)

  let mutable _autoGenConfig: Type       option = autoGenConfig
  let mutable _tests        : int<tests> option = tests

  new()                     = PropertyAttribute(None              , None      , null)
  new(autoGenConfig)        = PropertyAttribute(Some autoGenConfig, None      , null)
  new(autoGenConfig, tests) = PropertyAttribute(Some autoGenConfig, Some tests, null)
  new(tests, autoGenConfig) = PropertyAttribute(Some autoGenConfig, Some tests, null)
  new(autoGenConfig, skip)  = PropertyAttribute(Some autoGenConfig, None      , skip)
  new(tests)                = PropertyAttribute(None              , Some tests, null)
  new(skip)                 = PropertyAttribute(None              , None      , skip)

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
  member          _.AutoGenConfig    with set v = _autoGenConfig <- Some v
  member          _.Tests            with set v = _tests         <- Some v
  member internal _.GetAutoGenConfig            = _autoGenConfig
  member internal _.GetTests                    = _tests


/// Set a default AutoGenConfig or <tests> for all [<Property>] attributed methods in this class/module
[<AttributeUsage(AttributeTargets.Class, AllowMultiple = false)>]
type PropertiesAttribute(autoGenConfig, tests) =
  inherit Attribute() // don't inherit Property - it exposes members like Skip which are unsupported

  let mutable _autoGenConfig: Type       option = autoGenConfig
  let mutable _tests        : int<tests> option = tests

  new()                          = PropertiesAttribute(None              , None)
  new(autoGenConfig)             = PropertiesAttribute(Some autoGenConfig, None      )
  new(tests)                     = PropertiesAttribute(None              , Some tests)
  new(autoGenConfig:Type, tests) = PropertiesAttribute(Some autoGenConfig, Some tests)
  new(tests, autoGenConfig:Type) = PropertiesAttribute(Some autoGenConfig, Some tests)

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
  member          _.AutoGenConfig    with set v = _autoGenConfig <- Some v
  member          _.Tests            with set v = _tests         <- Some v
  member internal _.GetAutoGenConfig            = _autoGenConfig
  member internal _.GetTests                    = _tests