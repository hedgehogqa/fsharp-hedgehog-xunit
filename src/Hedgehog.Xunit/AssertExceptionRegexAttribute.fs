namespace Hedgehog.Xunit

open System

[<AttributeUsage(AttributeTargets.Method ||| AttributeTargets.Property, AllowMultiple = false)>]
type internal AssertExceptionRegexAttribute(regexPattern: string) =
  inherit Attribute()
