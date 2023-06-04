namespace FSharp.Collections.Builders

open System
open System.Collections.Generic
open System.Collections.Immutable

#nowarn "77" // set_Item.

/// <namespacedoc>
/// <summary>
/// Contains computation expression builders for conviently and efficiently constructing common collections.
/// </summary>
/// </namespacedoc>
///
/// <summary>
/// Contains computation expression builders for common collection types.
/// <para>
/// See also: 
/// <seealso cref="T:FSharp.Collections.Builders.Default"/>, 
/// <seealso cref="T:FSharp.Collections.Builders.Specialized"/>.
/// </para>
/// </summary>
[<AutoOpen>]
module Core =
    /// A special compiler-recognized delegate for specifying blocks of code with access to the collection builder state.
    [<CompilerMessage("This construct is for use by compiled F# code and should not be used directly.", 1204, IsHidden=true)>]
    type CollectionBuilderCode<'T> = delegate of byref<'T> -> unit
    
    /// Contains methods to build collections using computation expression syntax.
    type CollectionBuilderBase () =
        member inline _.Combine ([<InlineIfLambda>] f1 : CollectionBuilderCode<_>, [<InlineIfLambda>] f2 : CollectionBuilderCode<_>) =
            CollectionBuilderCode (fun sm -> f1.Invoke &sm; f2.Invoke &sm)
    
        member inline _.Delay ([<InlineIfLambda>] f : unit -> CollectionBuilderCode<_>) =
            CollectionBuilderCode (fun sm -> (f ()).Invoke &sm)
    
        member inline _.Zero () = CollectionBuilderCode (fun _ -> ())
    
        member inline _.While ([<InlineIfLambda>] condition : unit -> bool, [<InlineIfLambda>] body : CollectionBuilderCode<_>) =
            CollectionBuilderCode (fun sm ->
                while condition () do
                    body.Invoke &sm)
    
        member inline _.TryWith ([<InlineIfLambda>] body : CollectionBuilderCode<_>, [<InlineIfLambda>] handle : exn -> CollectionBuilderCode<_>) =
            CollectionBuilderCode (fun sm ->
                try body.Invoke &sm
                with e -> (handle e).Invoke &sm)
    
        member inline _.TryFinally ([<InlineIfLambda>] body : CollectionBuilderCode<_>, compensation : unit -> unit) =
            CollectionBuilderCode (fun sm ->
                try body.Invoke &sm
                with _ ->
                    compensation ()
                    reraise ()
                compensation ())
    
        member inline builder.Using (disposable : #IDisposable, [<InlineIfLambda>] body : #IDisposable -> CollectionBuilderCode<_>) =
            builder.TryFinally ((fun sm -> (body disposable).Invoke &sm), (fun () -> if not (isNull (box disposable)) then disposable.Dispose ()))

    /// The base collection builder.
    type CollectionBuilder () =
        inherit CollectionBuilderBase ()
        member inline _.Yield x = CollectionBuilderCode (fun sm -> ignore (^a : (member Add : ^b -> _) (sm, x)))

    /// The base dictionary builder.
    type DictionaryBuilder () =
        inherit CollectionBuilderBase ()
        member inline _.Yield (k, v) = CollectionBuilderCode (fun sm -> ignore (^a : (member set_Item : ^b * ^c -> _) (sm, k, v)))

    [<Sealed>]
    type ResizeArrayBuilder<'T> () =
        inherit CollectionBuilder ()
        static member val Instance = ResizeArrayBuilder<'T> ()
        member inline _.Run ([<InlineIfLambda>] f : CollectionBuilderCode<_>) =
            let mutable sm = ResizeArray<'T> ()
            f.Invoke &sm
            sm

    [<Sealed>]
    type HashSetBuilder<'T> () =
        inherit CollectionBuilder ()
        static member val Instance = HashSetBuilder<'T> ()
        member inline _.Run ([<InlineIfLambda>] f : CollectionBuilderCode<_>) =
            let mutable sm = HashSet<'T> ()
            f.Invoke &sm
            sm

    [<Sealed>]
    type SortedSetBuilder<'T> () =
        inherit CollectionBuilder ()
        static member val Instance = SortedSetBuilder<'T> ()
        member inline _.Run ([<InlineIfLambda>] f : CollectionBuilderCode<_>) =
            let mutable sm = SortedSet<'T> ()
            f.Invoke &sm
            sm

    [<Sealed>]
    type GenericCollectionBuilder<'Collection when 'Collection : (new : unit -> 'Collection)> () =
        inherit CollectionBuilder ()
        static member val Instance = GenericCollectionBuilder<'Collection> ()
        member inline _.Run ([<InlineIfLambda>] f : CollectionBuilderCode<_>) =
            let mutable sm = new 'Collection ()
            f.Invoke &sm
            sm

    /// <exclude />
    [<Sealed>]
    type DictionaryBuilder<'Key, 'Value when 'Key : equality> () =
        inherit DictionaryBuilder ()
        static member val Instance = DictionaryBuilder<'Key, 'Value> ()
        member inline _.Run ([<InlineIfLambda>] f : CollectionBuilderCode<_>) =
            let mutable sm = Dictionary<'Key, 'Value> ()
            f.Invoke &sm
            sm

    [<Sealed>]
    type SortedDictionaryBuilder<'Key, 'Value when 'Key : equality> () =
        inherit DictionaryBuilder ()
        static member val Instance = SortedDictionaryBuilder<'Key, 'Value> ()
        member inline _.Run ([<InlineIfLambda>] f : CollectionBuilderCode<_>) =
            let mutable sm = SortedDictionary<'Key, 'Value> ()
            f.Invoke &sm
            sm

    [<Sealed>]
    type GenericDictionaryBuilder<'Dictionary when 'Dictionary : (new : unit -> 'Dictionary)> () =
        inherit DictionaryBuilder ()
        static member val Instance = GenericDictionaryBuilder<'Dictionary> ()
        member inline _.Run ([<InlineIfLambda>] f : CollectionBuilderCode<_>) =
            let mutable sm = new 'Dictionary ()
            f.Invoke &sm
            sm

    [<Sealed>]
    type SetBuilder<'T when 'T : comparison> () =
        inherit CollectionBuilderBase ()
        static member val Instance = SetBuilder<'T> ()
        member inline _.Yield x = CollectionBuilderCode (fun sm -> sm <- Set.add x sm)
        member inline _.Run ([<InlineIfLambda>] f : CollectionBuilderCode<_>) =
            let mutable sm = Set.empty<'T>
            f.Invoke &sm
            sm

    [<Sealed>]
    type MapBuilder<'Key, 'Value when 'Key : comparison> () =
        inherit CollectionBuilderBase ()
        static member val Instance = MapBuilder<'Key, 'Value> ()
        member inline _.Yield (k, v) = CollectionBuilderCode (fun sm -> sm <- Map.add k v sm)
        member inline _.Run ([<InlineIfLambda>] f : CollectionBuilderCode<_>) =
            let mutable sm = Map.empty<'Key, 'Value>
            f.Invoke &sm
            sm

    [<Sealed>]
    type ImmutableArrayBuilder<'T> () =
        inherit CollectionBuilder ()
        static member val Instance = ImmutableArrayBuilder<'T> ()
        member inline _.Run ([<InlineIfLambda>] f : CollectionBuilderCode<ImmutableArray<'T>.Builder>) =
            let mutable sm = ImmutableArray.CreateBuilder ()
            f.Invoke &sm
            sm.ToImmutable ()

    [<Sealed>]
    type ImmutableHashSetBuilder<'T> () =
        inherit CollectionBuilder ()
        static member val Instance = ImmutableHashSetBuilder<'T> ()
        member inline _.Run ([<InlineIfLambda>] f : CollectionBuilderCode<ImmutableHashSet<'T>.Builder>) =
            let mutable sm = ImmutableHashSet.CreateBuilder ()
            f.Invoke &sm
            sm.ToImmutable ()

    [<Sealed>]
    type ImmutableSortedSetBuilder<'T> () =
        inherit CollectionBuilder ()
        static member val Instance = ImmutableSortedSetBuilder<'T> ()
        member inline _.Run ([<InlineIfLambda>] f : CollectionBuilderCode<ImmutableSortedSet<'T>.Builder>) =
            let mutable sm = ImmutableSortedSet.CreateBuilder ()
            f.Invoke &sm
            sm.ToImmutable ()

    [<Sealed>]
    type ImmutableListBuilder<'T> () =
        inherit CollectionBuilder ()
        static member val Instance = ImmutableListBuilder<'T> ()
        member inline _.Run ([<InlineIfLambda>] f : CollectionBuilderCode<ImmutableList<'T>.Builder>) =
            let mutable sm = ImmutableList.CreateBuilder ()
            f.Invoke &sm
            sm.ToImmutable ()

    [<Sealed>]
    type ImmutableDictionaryBuilder<'Key, 'Value> () =
        inherit DictionaryBuilder ()
        static member val Instance = ImmutableDictionaryBuilder<'Key, 'Value> ()
        member inline _.Run ([<InlineIfLambda>] f : CollectionBuilderCode<ImmutableDictionary<'Key, 'Value>.Builder>) =
            let mutable sm = ImmutableDictionary.CreateBuilder ()
            f.Invoke &sm
            sm.ToImmutable ()

    [<Sealed>]
    type ImmutableSortedDictionaryBuilder<'Key, 'Value> () =
        inherit DictionaryBuilder ()
        static member val Instance = ImmutableSortedDictionaryBuilder<'Key, 'Value> ()
        member inline _.Run ([<InlineIfLambda>] f : CollectionBuilderCode<ImmutableSortedDictionary<'Key, 'Value>.Builder>) =
            let mutable sm = ImmutableSortedDictionary.CreateBuilder ()
            f.Invoke &sm
            sm.ToImmutable ()

    [<Sealed>]
    type SumBuilder () =
        inherit CollectionBuilderBase ()
        member inline _.Yield x = CollectionBuilderCode (fun sm -> sm <- sm + x)
        member inline _.Run ([<InlineIfLambda>] f : CollectionBuilderCode<_>) =
            let mutable sm = LanguagePrimitives.GenericZero
            f.Invoke &sm
            sm

/// <summary>
/// Contains specialized overloads of <c>for</c> and <c>yield!</c> that give increased iteration performance
/// for common collection types at the cost of requiring that the type of the collection being iterated be statically known.
/// </summary>
/// <example>
/// The type of <c>xs</c> must be annotated or otherwise known,
/// but once it is, the appropriate specialized iteration technique
/// will be used.
/// <code lang="fsharp">
/// let f (xs : int list) =
///     resizeArray {
///         for x in xs -> x * x
///     }
/// </code>
/// <c>xs</c> is known to be a list because of the call to <c>List.length</c>.
/// <code lang="fsharp">
/// let g xs =
///     let len = List.length xs
///     resizeArray {
///         for x in xs -> x * len
///     }
/// </code>
/// </example>
module Specialized =
    /// <summary>
    /// Augments <see cref="T:FSharp.Collections.Builders.Core.CollectionBuilderBase"/> with
    /// specialized <c>for</c> implementations.
    /// </summary>
    type CollectionBuilderBase with
        member inline builder.For (sequence : seq<_>, [<InlineIfLambda>] body : _ -> CollectionBuilderCode<_>) =
            builder.Using (sequence.GetEnumerator (), fun e -> builder.While ((fun () -> e.MoveNext ()), (fun sm -> (body e.Current).Invoke &sm)))

        member inline _.For (list : _ list, [<InlineIfLambda>] body : _ -> CollectionBuilderCode<_>) =
            CollectionBuilderCode (fun sm ->
                for x in list do
                    (body x).Invoke &sm)
    
        member inline _.For (array : _ array, [<InlineIfLambda>] body : _ -> CollectionBuilderCode<_>) =
            CollectionBuilderCode (fun sm ->
                for x in array do
                    (body x).Invoke &sm)
    
        member inline _.For (set : Set<_>, [<InlineIfLambda>] body : _ -> CollectionBuilderCode<_>) =
            CollectionBuilderCode (fun sm ->
                let mutable sm' = sm
                set |> Set.iter (fun x -> (body x).Invoke &sm')
                sm <- sm')
    
        member inline _.For (map : Map<_, _>, [<InlineIfLambda>] body : _ -> CollectionBuilderCode<_>) =
            CollectionBuilderCode (fun sm ->
                let mutable sm' = sm
                map |> Map.iter (fun k v -> (body (k, v)).Invoke &sm')
                sm <- sm')
    
        member inline _.For (resizeArray : ResizeArray<_>, [<InlineIfLambda>] body : _ -> CollectionBuilderCode<_>) =
            CollectionBuilderCode (fun sm ->
                for i in 0 .. resizeArray.Count - 1 do
                    (body resizeArray[i]).Invoke &sm)

        member inline _.For (immutableArray : ImmutableArray<_>, [<InlineIfLambda>] body : _ -> CollectionBuilderCode<_>) =
            CollectionBuilderCode (fun sm ->
                for i in 0 .. immutableArray.Length - 1 do
                    (body immutableArray[i]).Invoke &sm)

        member inline _.For (immutableArrayBuilder : ImmutableArray<_>.Builder, [<InlineIfLambda>] body : _ -> CollectionBuilderCode<_>) =
            CollectionBuilderCode (fun sm ->
                for i in 0 .. immutableArrayBuilder.Count - 1 do
                    (body immutableArrayBuilder[i]).Invoke &sm)

    /// <summary>
    /// Augments <see cref="T:FSharp.Collections.Builders.Core.CollectionBuilder"/> with
    /// specialized <c>yield!</c> implementations.
    /// </summary>
    type CollectionBuilder with
        member inline builder.YieldFrom (xs : seq<_>) = builder.For (xs, fun x -> builder.Yield x)
        member inline builder.YieldFrom (xs : _ list) = builder.For (xs, fun x -> builder.Yield x)
        member inline builder.YieldFrom (xs : _ array) = builder.For (xs, fun x -> builder.Yield x)
        member inline builder.YieldFrom (xs : ResizeArray<_>) = builder.For (xs, fun x -> builder.Yield x)
        member inline builder.YieldFrom (xs : Set<_>) = builder.For (xs, fun x -> builder.Yield x)
        member inline builder.YieldFrom (xs : Map<_, _>) = builder.For (xs, fun (k, v) -> builder.Yield (k, v))
        member inline builder.YieldFrom (xs : ImmutableArray<_>) = builder.For (xs, fun x -> builder.Yield x)

    /// <summary>
    /// Augments <see cref="T:FSharp.Collections.Builders.Core.DictionaryBuilder"/> with
    /// specialized <c>yield!</c> implementations.
    /// </summary>
    type DictionaryBuilder with
        member inline builder.YieldFrom (xs : seq<_>) = builder.For (xs, fun (k, v) -> builder.Yield (k, v))
        member inline builder.YieldFrom (xs : _ list) = builder.For (xs, fun (k, v) -> builder.Yield (k, v))
        member inline builder.YieldFrom (xs : _ array) = builder.For (xs, fun (k, v) -> builder.Yield (k, v))
        member inline builder.YieldFrom (xs : ResizeArray<_>) = builder.For (xs, fun (k, v) -> builder.Yield (k, v))
        member inline builder.YieldFrom (xs : Set<_>) = builder.For (xs, fun (k, v) -> builder.Yield (k, v))
        member inline builder.YieldFrom (xs : Map<_, _>) = builder.For (xs, fun (k, v) -> builder.Yield (k, v))
        member inline builder.YieldFrom (xs : ImmutableArray<_>) = builder.For (xs, fun (k, v) -> builder.Yield (k, v))

    /// <summary>
    /// Augments <see cref="T:FSharp.Collections.Builders.Core.SumBuilder"/> with
    /// specialized <c>for</c> implementations.
    /// </summary>
    type SumBuilder with
        member inline _.For (span : Span<_>, [<InlineIfLambda>] body : _ -> CollectionBuilderCode<_>) =
            let mutable acc = LanguagePrimitives.GenericZero
            for x in span do
                (body x).Invoke &acc
            CollectionBuilderCode (fun sm -> sm <- sm + acc)

        member inline _.For (span : ReadOnlySpan<_>, [<InlineIfLambda>] body : _ -> CollectionBuilderCode<_>) =
            let mutable acc = LanguagePrimitives.GenericZero
            for x in span do
                (body x).Invoke &acc
            CollectionBuilderCode (fun sm -> sm <- sm + acc)

    /// <summary>
    /// Builds a <see cref="T:System.Collections.Generic.List`1"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let f (xs : int list) =
    ///     resizeArray {
    ///         for x in xs -> x * x
    ///     }
    /// </code>
    /// <code lang="fsharp">
    /// let a = 1
    /// let xs = [|2..100|]
    /// let ys = resizeArray { 0; 1; yield! xs }
    /// </code>
    /// </example>
    let resizeArray<'T> = ResizeArrayBuilder<'T>.Instance

    /// <summary>
    /// Builds a <see cref="T:System.Collections.Generic.HashSet`1"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let xs = hashSet { 1; 2; 3 }
    /// </code>
    /// </example>
    let hashSet<'T> = HashSetBuilder<'T>.Instance

    /// <summary>
    /// Builds a <see cref="T:System.Collections.Generic.SortedSet`1"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let xs = sortedSet { 1; 2; 3 }
    /// </code>
    /// </example>
    let sortedSet<'T> = SortedSetBuilder<'T>.Instance

    /// <summary>
    /// Builds a <see cref="T:FSharp.Collections.FSharpSet`1"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let xs = set' { 1; 2; 3 }
    /// </code>
    /// </example>
    let set'<'T when 'T : comparison> = SetBuilder<'T>.Instance

    /// <summary>
    /// Builds a collection of the inferred or specified type using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let xs = [|2..100|]
    /// let ys = collection&lt;ResizeArray&lt;int&gt;&gt; { 0; 1; yield! xs }
    /// let zs = collection&lt;HashSet&lt;int&gt;&gt; { 0; 1; yield! xs }
    /// </code>
    /// <code lang="fsharp">
    /// let xs = [|2..100|]
    /// let ys : ResizeArray&lt;int&gt; = collection { 0; 1; yield! xs }
    /// let zs : HashSet&lt;int&gt; = collection { 0; 1; yield! xs }
    /// </code>
    /// </example>
    let collection<'Collection when 'Collection : (new : unit -> 'Collection)> = GenericCollectionBuilder<'Collection>.Instance

    /// <summary>
    /// Builds a <see cref="T:System.Collections.Generic.Dictionary`2"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let m = dictionary { 1, "a"; 2, "b"; 3, "c" }
    /// </code>
    /// </example>
    let dictionary<'Key, 'Value when 'Key : equality> = DictionaryBuilder<'Key, 'Value>.Instance

    /// <summary>
    /// Builds a <see cref="T:System.Collections.Generic.SortedDictionary`2"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let m = sortedDictionary { 1, "a"; 2, "b"; 3, "c" }
    /// </code>
    /// </example>
    let sortedDictionary<'Key, 'Value when 'Key : equality> = SortedDictionaryBuilder<'Key, 'Value>.Instance

    /// <summary>
    /// Builds a <see cref="T:FSharp.Collections.FSharpMap`2"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let m = map { 1, "a"; 2, "b"; 3, "c" }
    /// </code>
    /// </example>
    let map<'Key, 'Value when 'Key : comparison> = MapBuilder<'Key, 'Value>.Instance

    /// <summary>
    /// Builds a dictionary of the inferred or specified type using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let m = dict'&lt;Dictionary&lt;int, int&gt;&gt; { 0, 0; 1, 1 }
    /// let m = dict'&lt;SortedDictionary&lt;int, int&gt;&gt; { 0, 0; 1, 1 }
    /// </code>
    /// <code lang="fsharp">
    /// let m : Dictionary&lt;int, int&gt; = dict' { 0, 0; 1, 1 }
    /// let m : SortedDictionary&lt;int, int&gt; = dict' { 0, 0; 1, 1 }
    /// </code>
    /// </example>
    let dict'<'Dictionary when 'Dictionary : (new : unit -> 'Dictionary)> = GenericDictionaryBuilder<'Dictionary>.Instance

    /// <summary>
    /// Builds an <see cref="T:System.Collections.Immutable.ImmutableArray`1"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let xs = immutableArray { 1; 2; 3 }
    /// </code>
    /// </example>
    let immutableArray<'T> = ImmutableArrayBuilder<'T>.Instance

    /// <summary>
    /// Builds an <see cref="T:System.Collections.Immutable.ImmutableHashSet`1"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let xs = immutableHashSet { 1; 2; 3 }
    /// </code>
    /// </example>
    let immutableHashSet<'T> = ImmutableHashSetBuilder<'T>.Instance

    /// <summary>
    /// Builds an <see cref="T:System.Collections.Immutable.ImmutableSortedSet`1"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let xs = immutableSortedSet { 1; 2; 3 }
    /// </code>
    /// </example>
    let immutableSortedSet<'T> = ImmutableSortedSetBuilder<'T>.Instance

    /// <summary>
    /// Builds an <see cref="T:System.Collections.Immutable.ImmutableList`1"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let xs = immutableList { 1; 2; 3 }
    /// </code>
    /// </example>
    let immutableList<'T> = ImmutableListBuilder<'T>.Instance

    /// <summary>
    /// Builds an <see cref="T:System.Collections.Immutable.ImmutableDictionary`2"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let m = immutableDictionary { 1, "a"; 2, "b"; 3, "c" }
    /// </code>
    /// </example>
    let immutableDictionary<'Key, 'Value> = ImmutableDictionaryBuilder<'Key, 'Value>.Instance

    /// <summary>
    /// Builds an <see cref="T:System.Collections.Immutable.ImmutableSortedDictionary`2"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let m = immutableSortedDictionary { 1, "a"; 2, "b"; 3, "c" }
    /// </code>
    /// </example>
    let immutableSortedDictionary<'Key, 'Value> = ImmutableSortedDictionaryBuilder<'Key, 'Value>.Instance

    /// <summary>
    /// Computes a sum using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let s = sum { 1; 2; 3 }
    /// </code>
    /// <code lang="fsharp">
    /// let xs = [1..100]
    /// let s = sum { for x in xs -> x }
    /// </code>
    /// <code lang="fsharp">
    /// let xs = [4..100]
    /// let s = sum { 1; 2; 3; yield! xs }
    /// </code>
    /// </example>
    let sum = SumBuilder ()

    /// <summary>
    /// Computes a sum using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let s = Σ { 1; 2; 3 }
    /// </code>
    /// <code lang="fsharp">
    /// let xs = [1..100]
    /// let s = Σ { for x in xs -> x }
    /// </code>
    /// <code lang="fsharp">
    /// let xs = [4..100]
    /// let s = Σ { 1; 2; 3; yield! xs }
    /// </code>
    /// </example>
    let Σ = sum

/// <summary>
/// Exposes type-inference-friendly collection builders.
/// </summary>
/// <example>
/// The type of <c>xs</c> is inferred to be <c>seq&lt;int&gt;</c>.
/// <code lang="fsharp">
/// let f xs =
///     resizeArray {
///         for x in xs -> x * x
///     }
/// </code>
/// </example>
[<AutoOpen>]
module Default =
    /// <summary>
    /// Augments <see cref="T:FSharp.Collections.Builders.Core.CollectionBuilderBase"/> with
    /// <c>for</c> implementations for <see cref="T:FSharp.Collections.seq`1"/>.
    /// </summary>
    type CollectionBuilderBase with
        member inline builder.For (sequence : seq<_>, [<InlineIfLambda>] body : _ -> CollectionBuilderCode<_>) =
            builder.Using (sequence.GetEnumerator (), fun e -> builder.While ((fun () -> e.MoveNext ()), (fun sm -> (body e.Current).Invoke &sm)))

    /// <summary>
    /// Augments <see cref="T:FSharp.Collections.Builders.Core.CollectionBuilder"/> with
    /// <c>yield!</c> implementations for <see cref="T:FSharp.Collections.seq`1"/>.
    /// </summary>
    type CollectionBuilder with
        member inline builder.YieldFrom (xs : seq<_>) = builder.For (xs, fun x -> builder.Yield x)

    /// <summary>
    /// Augments <see cref="T:FSharp.Collections.Builders.Core.Dictionary"/> with
    /// <c>yield!</c> implementations for <see cref="T:FSharp.Collections.seq`1"/>.
    /// </summary>
    type DictionaryBuilder with
        member inline builder.YieldFrom (xs : seq<_>) = builder.For (xs, fun (k, v) -> builder.Yield (k, v))
        member inline builder.YieldFrom (xs : seq<_>) = builder.For (xs, fun (KeyValue (k, v)) -> builder.Yield (k, v))

    /// <summary>
    /// Augments <see cref="T:FSharp.Collections.Builders.Collections.FSharpSetBuilder`1"/> with
    /// <c>yield!</c> implementations for <see cref="T:FSharp.Collections.seq`1"/>.
    /// </summary>
    type SetBuilder<'T when 'T : comparison> with
        member inline builder.YieldFrom (xs : seq<_>) = builder.For (xs, fun x -> builder.Yield x)

    /// <summary>
    /// Augments <see cref="T:FSharp.Collections.Builders.Collections.FSharpMapBuilder`2"/> with
    /// <c>yield!</c> implementations for <see cref="T:FSharp.Collections.seq`1"/>.
    /// </summary>
    type MapBuilder<'Key, 'Value when 'Key : comparison> with
        member inline builder.YieldFrom (xs : seq<_>) = builder.For (xs, fun (k, v) -> builder.Yield (k, v))
        member inline builder.YieldFrom (xs : seq<_>) = builder.For (xs, fun (KeyValue (k, v)) -> builder.Yield (k, v))

    /// <summary>
    /// Builds a <see cref="T:System.Collections.Generic.List`1"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let f xs =
    ///     resizeArray {
    ///         for x in xs -> x * x
    ///     }
    /// </code>
    /// <code lang="fsharp">
    /// let a = 1
    /// let xs = [|2..100|]
    /// let ys = resizeArray { 0; 1; yield! xs }
    /// </code>
    /// </example>
    let resizeArray<'T> = ResizeArrayBuilder<'T>.Instance

    /// <summary>
    /// Builds a <see cref="T:System.Collections.Generic.HashSet`1"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let xs = hashSet { 1; 2; 3 }
    /// </code>
    /// </example>
    let hashSet<'T> = HashSetBuilder<'T>.Instance

    /// <summary>
    /// Builds a <see cref="T:System.Collections.Generic.SortedSet`1"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let xs = sortedSet { 1; 2; 3 }
    /// </code>
    /// </example>
    let sortedSet<'T> = SortedSetBuilder<'T>.Instance

    /// <summary>
    /// Builds a <see cref="T:FSharp.Collections.FSharpSet`1"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let xs = set' { 1; 2; 3 }
    /// </code>
    /// </example>
    let set'<'T when 'T : comparison> = SetBuilder<'T>.Instance

    /// <summary>
    /// Builds a collection of the inferred or specified type using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let xs = [|2..100|]
    /// let ys = collection&lt;ResizeArray&lt;int&gt;&gt; { 0; 1; yield! xs }
    /// let zs = collection&lt;HashSet&lt;int&gt;&gt; { 0; 1; yield! xs }
    /// </code>
    /// <code lang="fsharp">
    /// let xs = [|2..100|]
    /// let ys : ResizeArray&lt;int&gt; = collection { 0; 1; yield! xs }
    /// let zs : HashSet&lt;int&gt; = collection { 0; 1; yield! xs }
    /// </code>
    /// </example>
    let collection<'Collection when 'Collection : (new : unit -> 'Collection)> = GenericCollectionBuilder<'Collection>.Instance

    /// <summary>
    /// Builds a <see cref="T:System.Collections.Generic.Dictionary`2"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let m = dictionary { 1, "a"; 2, "b"; 3, "c" }
    /// </code>
    /// </example>
    let dictionary<'Key, 'Value when 'Key : equality> = DictionaryBuilder<'Key, 'Value>.Instance

    /// <summary>
    /// Builds a <see cref="T:System.Collections.Generic.SortedDictionary`2"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let m = sortedDictionary { 1, "a"; 2, "b"; 3, "c" }
    /// </code>
    /// </example>
    let sortedDictionary<'Key, 'Value when 'Key : equality> = SortedDictionaryBuilder<'Key, 'Value>.Instance

    /// <summary>
    /// Builds a <see cref="T:FSharp.Collections.FSharpMap`2"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let m = map { 1, "a"; 2, "b"; 3, "c" }
    /// </code>
    /// </example>
    let map<'Key, 'Value when 'Key : comparison> = MapBuilder<'Key, 'Value>.Instance

    /// <summary>
    /// Builds a dictionary of the inferred or specified type using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let m = dict'&lt;Dictionary&lt;int, int&gt;&gt; { 0, 0; 1, 1 }
    /// let m = dict'&lt;SortedDictionary&lt;int, int&gt;&gt; { 0, 0; 1, 1 }
    /// </code>
    /// <code lang="fsharp">
    /// let m : Dictionary&lt;int, int&gt; = dict' { 0, 0; 1, 1 }
    /// let m : SortedDictionary&lt;int, int&gt; = dict' { 0, 0; 1, 1 }
    /// </code>
    /// </example>
    let dict'<'Dictionary when 'Dictionary : (new : unit -> 'Dictionary)> = GenericDictionaryBuilder<'Dictionary>.Instance

    /// <summary>
    /// Builds an <see cref="T:System.Collections.Immutable.ImmutableArray`1"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let xs = immutableArray { 1; 2; 3 }
    /// </code>
    /// </example>
    let immutableArray<'T> = ImmutableArrayBuilder<'T>.Instance

    /// <summary>
    /// Builds an <see cref="T:System.Collections.Immutable.ImmutableHashSet`1"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let xs = immutableHashSet { 1; 2; 3 }
    /// </code>
    /// </example>
    let immutableHashSet<'T> = ImmutableHashSetBuilder<'T>.Instance

    /// <summary>
    /// Builds an <see cref="T:System.Collections.Immutable.ImmutableSortedSet`1"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let xs = immutableSortedSet { 1; 2; 3 }
    /// </code>
    /// </example>
    let immutableSortedSet<'T> = ImmutableSortedSetBuilder<'T>.Instance

    /// <summary>
    /// Builds an <see cref="T:System.Collections.Immutable.ImmutableList`1"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let xs = immutableList { 1; 2; 3 }
    /// </code>
    /// </example>
    let immutableList<'T> = ImmutableListBuilder<'T>.Instance

    /// <summary>
    /// Builds an <see cref="T:System.Collections.Immutable.ImmutableDictionary`2"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let m = immutableDictionary { 1, "a"; 2, "b"; 3, "c" }
    /// </code>
    /// </example>
    let immutableDictionary<'Key, 'Value> = ImmutableDictionaryBuilder<'Key, 'Value>.Instance

    /// <summary>
    /// Builds an <see cref="T:System.Collections.Immutable.ImmutableSortedDictionary`2"/> using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let m = immutableSortedDictionary { 1, "a"; 2, "b"; 3, "c" }
    /// </code>
    /// </example>
    let immutableSortedDictionary<'Key, 'Value> = ImmutableSortedDictionaryBuilder<'Key, 'Value>.Instance

    /// <summary>
    /// Computes a sum using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let s = sum { 1; 2; 3 }
    /// </code>
    /// <code lang="fsharp">
    /// let xs = [1..100]
    /// let s = sum { for x in xs -> x }
    /// </code>
    /// <code lang="fsharp">
    /// let xs = [4..100]
    /// let s = sum { 1; 2; 3; yield! xs }
    /// </code>
    /// </example>
    let sum = SumBuilder ()

    /// <summary>
    /// Computes a sum using computation expression syntax.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let s = Σ { 1; 2; 3 }
    /// </code>
    /// <code lang="fsharp">
    /// let xs = [1..100]
    /// let s = Σ { for x in xs -> x }
    /// </code>
    /// <code lang="fsharp">
    /// let xs = [4..100]
    /// let s = Σ { 1; 2; 3; yield! xs }
    /// </code>
    /// </example>
    let Σ = sum
