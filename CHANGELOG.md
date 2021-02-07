# Changelog

### 0.1.3 (2021-01-18)

* `PropertiesAttribute` extends `Attribute` - it no longer extends `PropertyAttribute`.
* Tests returning a `Result` in an `Error` state will be treated as a failure.
* Tests returning `Async<Result<_,_>>` or `Task<Result<_,_>>` are run synchronously and are expected to be in the `Ok` state.

### 0.1.0 (2021-01-08)

* Updated to Hedgehog.Experimental 0.3.0

### 0.0.1 (2020-12-30)

* Initial release