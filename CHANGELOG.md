# Changelog

### 0.3.0

* Tests decorated with `PropertyAttribute` may have generics in their parameter types and return types.
* Added `AutoGenConfigArgs`
* `RecheckAttribute`'s `Size` overrides `PropertyAttribute`'s `Size`

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
