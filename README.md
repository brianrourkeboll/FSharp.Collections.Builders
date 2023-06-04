# FSharp.Collections.Builders

This library offers a set of computation expressions for conveniently and efficiently constructing common collections.

## Installation

Get it on NuGet: [FSharp.Collections.Builders](https://www.nuget.org/packages/FSharp.Collections.Builders).

```powershell
dotnet add package FSharp.Collections.Builders
```

## API documentation

See the [API documentation](https://brianrourkeboll.github.io/FSharp.Collections.Builders/reference/index.html)
for the full set of supported collections and operations.

## Quick start

The computation expression builders exposed by this library support most of the operations available in the built-in
list, array, and sequence expressions, including `for`, `while`, `yield!`, `try`/`with`, `try`/`finally`, and conditionals.[^1]

### `FSharp.Collections.Builders`

Open `FSharp.Collections.Builders` for type-inference-friendly builders that behave similarly to the built-in list,
array, and sequence expressions in that they treat any collection used with `for` or `yield!` as a `seq<_>`.

```fsharp
open FSharp.Collections.Builders
```

## Mutable collections from `System.Collections.Generic`

### `resizeArray`

Enables efficiently constructing and transforming instances of `ResizeArray<'T>` (`System.Collections.Generic.List<'T>`).

```fsharp
let xs = resizeArray { 1; 2; 3 }
```

> Compiles to:
>
> ```fsharp
> let xs = ResizeArray ()
> xs.Add 1
> xs.Add 2
> xs.Add 3
> ```

```fsharp
let ys = resizeArray { for x in xs -> float x * 2.0 }
```

> Compiles to:
>
> ```fsharp
> let ys = ResizeArray ()
> for x in xs do
>     ys.Add (float x * 2.0)
> ```

```fsharp
let f xs = resizeArray { for x in xs do if x % 3 = 0 then x }
```

> `xs` is `seq<int>`.

```fsharp
let g xs = resizeArray<float> { for x in xs -> x * x }
```

> `xs` is `seq<float>`.

### `hashSet`

Enables efficiently constructing and transforming instances of `System.Collections.Generic.HashSet<'T>`.

```fsharp
let xs = hashSet { 1; 2; 3 }
```

> `hashSet [1; 2; 3]`

```fsharp
let ys = hashSet { yield! xs; yield! xs }
```

> `hashSet [1; 2; 3]`

### `sortedSet`

Enables efficiently constructing and transforming instances of `System.Collections.Generic.SortedSet<'T>`.

```fsharp
let xs = sortedSet { 3; 2; 1; 2 }
```

> `sortedSet [1; 2; 3]`

### `dictionary`

Enables efficiently constructing and transforming instances of `System.Collections.Generic.Dictionary<'TKey, 'TValue>`.

```fsharp
let kvs = dictionary { 1, "a"; 2, "b"; 3, "c" }
```

```fsharp
let filtered = dictionary { for k, v in kvs do if k > 1 then k, v }
```

```fsharp
let list = [1..100]
let stringMap = dictionary { for i in list -> string i, i }
```

### `sortedDictionary`

Enables efficiently constructing and transforming instances of `System.Collections.Generic.SortedDictionary<'TKey, 'TValue>`.

```fsharp
let m = sortedDictionary { 2, "b"; 3, "c"; 1, "a"; 3, "d" }
```

> `sortedDictionary [1, "a"; 2, "b"; 3, "d"]`

## F# collections

The `set'`[^2] and `map` builders enable ergonomically constructing immutable F# sets and maps
without requiring intermediate collections or the ceremony of folds or recursive functions.

### `set'`

```fsharp
let xs = set' { 1; 2; 3 }
```

> `set [1; 2; 3]`

```fsharp
let ys = set' { 1; 2; 3; 3 }
```

> `set [1; 2; 3]`

```fsharp
let xs = [5; 1; 1; 1; 3; 3; 2; 2; 5; 5; 5]
let ys = set' { for x in xs do if x &&& 1 <> 0 then x }
```

> `set [1; 3; 5]`

### `map`

```fsharp
let m = map { 2, "b"; 3, "c"; 1, "a"; 3, "d" }
```

> `map [1, "a"; 2, "b"; 3, "d"]`

```fsharp
let m = map { for x in 1..100 -> x, x * x }
```

> Equivalent to
>
> ```fsharp
> let m = Map.ofSeq {1..100}
> ```
>
> or
>
> ```fsharp
> let m = (Map.empty, {1..100}) ||> Map.fold (fun m x -> m.Add (x, x * x))
> ```

## Immutable collections from `System.Collections.Immutable`

### `immutableArray`

Enables efficiently constructing and transforming instances of `System.Collections.Immutable.ImmutableArray<'T>`.

```fsharp
let xs = immutableArray { 1; 2; 3 }
```

> Compiles to:
>
> ```fsharp
> let xs =
>     let builder = ImmutableArray.CreateBuilder ()
>     builder.Add 1
>     builder.Add 2
>     builder.Add 3
>     builder.ToImmutable ()
> ```

```fsharp
let ys = immutableArray { for x in xs -> string x }
```

### `immutableList`

```fsharp
let xs = immutableList { 1; 2; 3 }
```

> `immutableList [1; 2; 3]`

### `immutableHashSet`

```fsharp
let xs = immutableHashSet { 3; 1; 2; 3 }
```

> `immutableHashSet [3; 1; 2]`

### `immutableSortedSet`

```fsharp
let xs = immutableSortedSet { 3; 1; 2; 3 }
```

> `immutableSortedSet [1; 2; 3]`

### `immutableDictionary`

```fsharp
let kvs = immutableDictionary { 1, "a"; 2, "b"; 3, "c" }
```

> `immutableDictionary [1, "a"; 2, "b"; 3, "c"]`

### `immutableSortedDictionary`

```fsharp
let kvs = immutableSortedDictionary { 2, "b"; 3, "c"; 1, "a"; 3, "d" }
```

> `immutableSortedDictionary [1, "a"; 2, "b"; 3, "d"]`

### Summation with `sum`, `Σ`

Given

```fsharp
let xs = [1..100]
```

Instead of

```fsharp
let s =
    xs
    |> List.filter (fun x -> x % 3 = 0)
    |> List.sum
```

`sum` enables:

```fsharp
let s = sum { for x in xs do if x % 3 = 0 then x }
```

The Greek capital letter sigma `Σ` (easily enterable via <kbd>⊞ Win</kbd> + <kbd>.</kbd> on Windows) may read better in certain domains:

```fsharp
Σ { for item in items -> item.Subtotal } <= 0.10 * total
```

### `FSharp.Collections.Builders.Specialized`

To enable specialized overloads of `for` and `yield!` that give increased iteration performance
at the cost of requiring that the type of the collection being iterated be statically known:

```fsharp
open FSharp.Collections.Builders.Specialized
```

```fsharp
let f (xs : int list) =
    resizeArray {
        for x in xs -> x * x
    }
```

> Compiles down to a fast `'T list` iteration.

```fsharp
let g xs =
    let len = Array.length xs
    resizeArray {
        for x in xs -> x * len
    }
```

> Compiles down to a fast integer `for`-loop.

## Motivation

F#'s built-in [list](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/lists#creating-and-initializing-lists),
[array](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/arrays#create-arrays),
and [sequence](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/sequences#sequence-expressions) expressions
make initializing and transforming `'T list`, `'T array`, and `seq<'T>` quite nice:

#### List literal

```fsharp
let nums = [1; 2; 3]
```

#### List expression with `for` and `->`

```fsharp
let doubled = [ for num in nums -> float num * 2.0 ]
```

#### Array expression with `for` and `->`

```fsharp
let doubledArr = [| for num in nums -> float num * 2.0 |]
```

#### Sequence expression with `for` and `->`

```fsharp
let doubledSeq = seq { for num in nums -> float num * 2.0 }
```

But when it comes time to work with one of the common mutable collection types from `System.Collections.Generic`,
whether to interoperate with other .NET libraries or for specific performance or modeling reasons, F# doesn't
provide the same syntactic sugar, forcing you you to switch modes from expression-based to statement-based style:

```fsharp
let nums = ResizeArray ()
nums.Add 1
nums.Add 2
nums.Add 3
```

Or, to keep the ergonomics of sequence expressions, you must instantiate and iterate over intermediate collections:

#### Instantiates and iterates over an intermediate `int list`

```fsharp
let nums = ResizeArray [1; 2; 3]
```

#### Instantiates and iterates over an intermediate `seq<int>`

```fsharp
let nums = ResizeArray (seq { 1; 2; 3 })
```

### Ergonomic collection initialization beyond lists, arrays, and sequences

C# offers collection initialization syntax for types that implement `IEnumerable<T>` and have a public `Add` method:

```csharp
var nums = new List<int> { 1, 2, 3 };
var nums = new HashSet<int> { 1, 2, 3 };
```

Or, with target-typed `new()`:

```csharp
List<int> nums = new() { 1, 2, 3 };
HashSet<int> nums = new() { 1, 2, 3 };
```

F# 6's [resumable code](https://github.com/fsharp/fslang-design/blob/main/FSharp-6.0/FS-1087-resumable-code.md) feature
makes it straightforward to implement efficient, ergonomic computation expression builders
for collection types that aren't special-cased by the F# compiler.

This library implements such builders for several common mutable and immutable collections from `System.Collections.Generic` and `System.Collections.Immutable`, and `FSharp.Collections`,
as well as offering generic `collection` and `dict'` builders that support any collection type with a default constructor and an appropriate `Add` method.

## Future

There are a couple language suggestions that might someday (happily!) make parts of this library obsolete:

- https://github.com/fsharp/fslang-suggestions/issues/1086
- https://github.com/fsharp/fslang-suggestions/issues/619

Additional potential development directions:

- Add versions that take initial capacities, equality comparers, etc.
- Add a vectorized version of the `sum` expression.

[^1]: It is not yet possible to provide custom implementations of the range operator `(..)` for computation expression builders—although there is an [approved language suggestion](https://github.com/fsharp/fslang-suggestions/issues/1116) for it.
[^2]: Alas, unlike `seq`, which is special-cased by the compiler and can act as both a function and computation expression builder, it is impossible to do the same with the built-in `set` function (without it being similarly special-cased in the compiler).
