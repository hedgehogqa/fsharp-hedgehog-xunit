namespace Hedgehog.Xunit

open System
open Xunit.Sdk
open Hedgehog

/// Generates arguments using GenX.auto (or autoWith if you provide an AutoGenConfig), then runs Property.check
[<AttributeUsage(AttributeTargets.Method ||| AttributeTargets.Property, AllowMultiple = false)>]
[<XunitTestCaseDiscoverer("XunitOverrides+PropertyTestCaseDiscoverer", "Hedgehog.Xunit")>]
type PropertyAttribute(autoGenConfig, tests, shrinks) =
  inherit Xunit.FactAttribute()

  let mutable _autoGenConfig: Type         option = autoGenConfig
  let mutable _tests        : int<tests>   option = tests
  let mutable _shrinks      : int<shrinks> option = shrinks

  new()                                   = PropertyAttribute(None              , None      , None        )
  new(tests)                              = PropertyAttribute(None              , Some tests, None        )
  new(tests, shrinks)                     = PropertyAttribute(None              , Some tests, Some shrinks)
  new(autoGenConfig)                      = PropertyAttribute(Some autoGenConfig, None      , None        )
  new(autoGenConfig:Type, tests)          = PropertyAttribute(Some autoGenConfig, Some tests, None        )
  new(autoGenConfig:Type, tests, shrinks) = PropertyAttribute(Some autoGenConfig, Some tests, Some shrinks)

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
  member          _.Shrinks          with set v = _shrinks       <- Some v
  member internal _.GetAutoGenConfig            = _autoGenConfig
  member internal _.GetTests                    = _tests
  member internal _.GetShrinks                  = _shrinks


/// Set a default AutoGenConfig or <tests> for all [<Property>] attributed methods in this class/module
[<AttributeUsage(AttributeTargets.Class, AllowMultiple = false)>]
type PropertiesAttribute(autoGenConfig, tests, shrinks) =
  inherit Attribute() // don't inherit Property - it exposes members like Skip which are unsupported

  let mutable _autoGenConfig: Type         option = autoGenConfig
  let mutable _tests        : int<tests>   option = tests
  let mutable _shrinks      : int<shrinks> option = shrinks

  new()                                   = PropertiesAttribute(None              , None      , None        )
  new(tests)                              = PropertiesAttribute(None              , Some tests, None        )
  new(tests, shrinks)                     = PropertiesAttribute(None              , Some tests, Some shrinks)
  new(autoGenConfig)                      = PropertiesAttribute(Some autoGenConfig, None      , None        )
  new(autoGenConfig:Type, tests)          = PropertiesAttribute(Some autoGenConfig, Some tests, None        )
  new(autoGenConfig:Type, tests, shrinks) = PropertiesAttribute(Some autoGenConfig, Some tests, Some shrinks)

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
  member          _.Shrinks          with set v = _shrinks       <- Some v
  member internal _.GetAutoGenConfig            = _autoGenConfig
  member internal _.GetTests                    = _tests
  member internal _.GetShrinks                  = _shrinks
