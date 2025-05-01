# Changelog

### 0.7.0 (2025-05-01)

* Updated to .NET 8
* Updated to Hedgehog.Experimental 0.9.0

### 0.6.0 (2023-07-05)

* Added `GenAttribute`, which sets a parameter's generator
* Named arguments work for `Property` and `Properties` in C#
* Returning a Task works in C#

### 0.5.2 (2023-01-13)

* Reduced Xunit dependencies to Xunit.Core

### 0.5.1 (2022-10-12)

* Added support for tests that return a property.

### 0.5.0 (2022-07-01)

* Fixed a bug where Recheck ran multiple times.
* Updated to Hedgehog.Experimental 0.7.0

### 0.4.1 (2022-05-01)

* Updated to Hedgehog.Experimental 0.6.1

### 0.4.0 (2022-05-01)

* Updated to Hedgehog.Experimental 0.6.0

### 0.3.0 (2021-09-29)

* Tests decorated with `PropertyAttribute` may have generics in their parameter types and return types.
* Added `AutoGenConfigArgs`
* `RecheckAttribute`'s `Size` overrides `PropertyAttribute`'s `Size`
* If a test fails, a `RecheckAttribute` is appended to the the Exception message for easy copy/pasting
* Updated to Hedgehog.Experimental 0.5.0

### 0.2.1 (2021-05-15)

* Added `Shrinks` and `Size` to `PropertyAttribute` and `PropertiesAttribute`
* Added `RecheckAttribute`

### 0.2.0 (2021-02-07)

* Updated to Hedgehog.Experimental 0.4.0
* If a test fails due to returning a `Result` in an `Error` state, its `Exception` will report the value in `Error`.

### 0.1.3 (2021-01-18)

* `PropertiesAttribute` extends `Attribute` - it no longer extends `PropertyAttribute`.
* Tests returning a `Result` in an `Error` state will be treated as a failure.
* Tests returning `Async<Result<_,_>>` or `Task<Result<_,_>>` are run synchronously and are expected to be in the `Ok` state.

### 0.1.0 (2021-01-08)

* Updated to Hedgehog.Experimental 0.3.0

### 0.0.1 (2020-12-30)

* Initial release
