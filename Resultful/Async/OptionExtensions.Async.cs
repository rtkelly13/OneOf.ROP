﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OneOf;
using OneOf.Types;
using Resultful.Utils;
using TaskExt;

namespace Resultful
{
    public static partial class Option
    {
        //ToOneOf
        public static Task<OneOf<T, None>> ToOneOf<T>(this Task<Option<T>> value)
            => value.Map(x => x.ToOneOf()); 

        //Builder for Option<T> from Task<T> value
        public static Task<Option<T>> Some<T>(this Task<T> value)
            => value.Map(item => item.Some());

        //Switch on Task<Option<T>>
        public static Task Switch<T>(this Task<Option<T>> value, Action<T> someFunc, Action<None> noneFunc)
            => value.Discard(item => item.Switch(
                someFunc.ThrowIfDefault(nameof(someFunc)),
                noneFunc.ThrowIfDefault(nameof(noneFunc))));

        public static Task SwitchAsync<T>(this Task<Option<T>> value, Func<T, Task> someFunc, Func<None, Task> noneFunc)
            => value.DiscardAsync(item => item.Match(
                someFunc.ThrowIfDefault(nameof(someFunc)),
                noneFunc.ThrowIfDefault(nameof(noneFunc))));

        //Match on Task<Option<T>>
        public static Task<TResult> MatchAsync<T, TResult>(this Task<Option<T>> value, Func<T, Task<TResult>> someFunc, Func<None, Task<TResult>> noneFunc)
            => value.Bind(item => item.Match(
                someFunc.ThrowIfDefault(nameof(someFunc)),
                noneFunc.ThrowIfDefault(nameof(noneFunc))));

        public static Task<TResult> Match<T, TResult>(this Task<Option<T>> value, Func<T, TResult> someFunc, Func<None, TResult> noneFunc)
            => value.Map(item => item.Match(
                someFunc.ThrowIfDefault(nameof(someFunc)),
                noneFunc.ThrowIfDefault(nameof(noneFunc))));

        //Unwrap on Option<Task<T>>
        public static Task<Option<T>> UnwrapAsync<T>(this Option<Task<T>> value)
            => value.Match(
                async item => (await item.ConfigureAwait(false)).Some(),
                none => Task.FromResult<Option<T>>(none));

        //WhenAll on IEnumerable<Task<Option<T>>>
        public static Task<Option<IEnumerable<T>>> WhenAll<T>(this IEnumerable<Option<Task<T>>> values)
            => Task.WhenAll(values.ThrowIfDefault(nameof(values)).Select(x => x.UnwrapAsync()))
                .Map(value => value.Unroll());

        //BindAsync on Option<T>
        public static Task<Option<TResult>> Bind<T, TResult>(this Task<Option<T>> value, Func<T, Option<TResult>> bindFunc)
            => value.Map(item => item.Bind(bindFunc));

        public static Task<Option<TResult>> BindAsync<T, TResult>(this Task<Option<T>> value, Func<T, Task<Option<TResult>>> bindFunc)
            => value.Bind(item => item.BindAsync(bindFunc));

        //MapAsync on Option<T>
        public static Task<Option<TResult>> Map<T, TResult>(this Task<Option<T>> value, Func<T, TResult> bindFunc)
            => value.Map(item2 => item2.Map(bindFunc));

        public static Task<Option<TResult>> MapAsync<T, TResult>(this Task<Option<T>> value, Func<T, Task<TResult>> bindFunc)
            => value.Bind(item => item.MapAsync(bindFunc));

        //TeeAsync on Option<T>
        public static Task<Option<T>> TeeAsync<T>(this Task<Option<T>> value, Func<T, Task> asyncFunc)
            => value.Bind(item => item.TeeAsync(asyncFunc));

        public static Task<Option<T>> Tee<T>(this Task<Option<T>> value, Action<T> teeAction)
            => value.Map(item => item.Tee(teeAction));

        //FoldAsync on Option<T>
        public static Task<TResult> Fold<T, TResult>(this Task<Option<T>> value, TResult seed, Func<TResult, T, TResult> func)
            => value.Map(x => x.Fold(seed, func));

        public static Task<TResult> FoldAsync<T, TResult>(this Task<Option<T>> value, TResult seed, Func<TResult, T, Task<TResult>> func)
            => value.Bind(x => x.FoldAsync(seed, func));

        //FoldUntilAsync on Option<T>
        public static Task<Option<TResult>> FoldUntil<T, TResult>(this Task<Option<T>> value, TResult seed, Func<TResult, T, Option<TResult>> func)
            => value.Map(x => x.FoldUntil(seed, func));

        public static Task<Option<TResult>> FoldUntilAsync<T, TResult>(this Task<Option<T>> value, TResult seed, Func<TResult, T, Task<Option<TResult>>> func)
            => value.Bind(x => x.FoldUntilAsync(seed, func));


        //OrAsync on Option<T>

        public static Task<T> Or<T>(this Task<Option<T>> value, T otherValue)
            => value.Map(item => item.Or(otherValue));

        public static Task<T> OrAsync<T>(this Task<Option<T>> value, Task<T> otherValue)
            => value.Bind(item => item.OrAsync(otherValue));

        public static Task<T> Or<T>(this Task<Option<T>> value, Func<T> otherFunc)
            => value.Map(item => item.Or(otherFunc));

        public static Task<T> OrAsync<T>(this Task<Option<T>> value, Func<Task<T>> otherFunc)
            => value.Bind(item => item.OrAsync(otherFunc));

        public static Task<Option<T>> Or<T>(this Task<Option<T>> value, Option<T> other)
            => value.Map(item => item.Or(other));

        public static Task<Option<T>> OrAsync<T>(this Task<Option<T>> value, Task<Option<T>> other)
            => value.Bind(item => item.OrAsync(other));

        public static Task<Option<T>> Or<T>(this Task<Option<T>> value, Func<Option<T>> otherFunc)
            => value.Map(item => item.Or(otherFunc));

        public static Task<Option<T>> OrAsync<T>(this Task<Option<T>> value, Func<Task<Option<T>>> otherFunc)
            => value.Bind(item => item.OrAsync(otherFunc));

        //Flatten
        public static Task<Option<T>> Flatten<T>(this Task<Option<Option<T>>> value)
            => value.Map(x => x.Flatten());

        //ToOptionAsync
        public static Task<Option<T>> ToOption<T>(this Task<T> value)
            => value.Map(item => item.ToOption());

        //Reduce on List<Option<T>>
        public static async Task<Option<T>> ReduceAsync<T>(this IEnumerable<Option<T>> values, Func<T, T, Task<T>> aggrFunc)
        {
            using (var enumerator = values.ThrowIfDefault(nameof(values)).GetEnumerator())
            {
                return await InternalList.ReduceInternalAsync(enumerator,
                    (acc, val) => AggrHelper.FuncAsync(acc, val, aggrFunc))
                    .ConfigureAwait(false);
            }
        }

        //ReduceUntil on List<Option<T>>
        public static async Task<Option<T>> ReduceUntil<T>(this IEnumerable<Option<T>> values,
            Func<T, T, Task<Option<T>>> aggrFunc)
        {
            using (var enumerator = values.ThrowIfDefault(nameof(values)).GetEnumerator())
            {
                return await InternalList.ReduceUntilInternalAsync(enumerator,
                    (acc, val) => AggrHelper.FuncUntilAsync(acc, val, aggrFunc))
                    .ConfigureAwait(false);
            }
        }

        //Fold on List<Option<T>>
        public static async Task<TResult> FoldAsync<TResult, T>(this IEnumerable<Option<T>> values, TResult seed,
            Func<TResult, T, Task<TResult>> aggrFunc)
        {
            using (var enumerator = values.ThrowIfDefault(nameof(values)).GetEnumerator())
            {
                return await InternalList.FoldInternalAsync(enumerator, seed,
                    (acc, val) => val.FoldAsync(acc, aggrFunc))
                    .ConfigureAwait(false);
            }
        }

        public static Task<Option<TResult>> FoldAsync<TResult, T>(this IEnumerable<Option<T>> values, Option<TResult> seed,
            Func<TResult, T, Task<TResult>> aggrFunc)
            => seed.MapAsync(x => FoldAsync(values.ThrowIfDefault(nameof(values)), x, aggrFunc));

        //FoldUntil on List<Option<T>>
        public static async Task<TResult> FoldUntilAsync<TResult, T>(this IEnumerable<Option<T>> values, TResult seed,
            Func<TResult, T, Task<Option<TResult>>> aggrFunc)
        {
            using (var enumerator = values.ThrowIfDefault(nameof(values)).GetEnumerator())
            {
                return await InternalList.FoldUntilInternalAsync(enumerator, seed,
                    (acc, val) => val.FoldUntilAsync(acc, aggrFunc))
                    .ConfigureAwait(false);
            }
        }

        public static Task<Option<TResult>> FoldUntilAsync<TResult, T>(this IEnumerable<Option<T>> values, Option<TResult> seed,
            Func<TResult, T, Task<Option<TResult>>> aggrFunc)
            => seed.MapAsync(x => FoldUntilAsync(values.ThrowIfDefault(nameof(values)), x, aggrFunc));


        //TryReduceUntil on List<Option<T>>
        public static async Task<Option<T>> TryReduceAsync<T>(this IEnumerable<Option<T>> values,
            Func<T, T, Task<T>> aggrFunc)
        {
            using (var enumerator = values.ThrowIfDefault(nameof(values)).GetEnumerator())
            {
                return await InternalList.TryReduceInternalAsync(enumerator,
                    (acc, val) => AggrHelper.FuncAsync(acc, val, aggrFunc))
                    .Flatten().ConfigureAwait(false);
            }
        }

        //TryReduceUntil on List<Option<T>>
        public static async Task<Option<T>> TryReduceUntilAsync<T>(this IEnumerable<Option<T>> values,
            Func<T, T, Task<Option<T>>> aggrFunc)
        {
            using (var enumerator = values.ThrowIfDefault(nameof(values)).GetEnumerator())
            {
                return await InternalList.TryReduceUntilInternalAsync(enumerator,
                    (acc, val) => AggrHelper.FuncUntilAsync(acc, val, aggrFunc))
                    .Flatten().ConfigureAwait(false);
            }
        }


    }
}
