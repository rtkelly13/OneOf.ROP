﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OneOf.ROP
{
    /*
     * Introduce a IPlus<T> interface which has T, T method which is a local method which can be used to define abstract plus operations on a type.
     * This will be useful for introducing a custom plus type without loosing any power in the implementation.
     * But the number of overloads grows at an unmanageable rate especially with Result<T ,TError>
     *
     * This can be used against the Plus Function, for Result<T, TError> this will mean 4 cases of the generic implementation but should make it more useful.
     * This will also affect the Fold Function.
     * Every other main type should have fewer overloads
     */

    public static partial class Result
    {
        public static T Id<T>(this T value) => value;

        //Builder for Result<T, TError>
        public static Result<T, IEnumerable<TError>> Fail<T, TError>(this IEnumerable<TError> errors)
            => new Result<T, IEnumerable<TError>>(errors);

        public static Result<T, IEnumerable<TError>> Fail<T, TError>(params TError[] errors)
            => new Result<T, IEnumerable<TError>>(errors);

        public static Result<T, TError> Fail<T, TError>(this TError value)
            => new Result<T, TError>(value);

        public static Result<T, TError> Ok<T, TError>(this T value)
            => new Result<T, TError>(value);

        //Builder for Result<T>
        public static Result<T> Fail<T>(this IEnumerable<string> errors)
            => new Result<T>(errors);

        public static Result<T> Fail<T>(params string[] errors)
            => new Result<T>(errors);

        public static Result<T> Ok<T>(this T value)
            => new Result<T>(value);


        //Plus on Result<T, TError>
        public static Result<(TLeft, TRight), TError> Plus<TLeft, TRight, TError>(this Result<TLeft, TError> left, Result<TRight, TError> right, Func<TError, TError, TError> mergeFunc)
            => left.Match(
                leftValue => right.Match(rightValue => Ok<(TLeft, TRight), TError>((leftValue, rightValue)), Fail<(TLeft, TRight), TError>),
                errors => right.Match(rightValue => Fail<(TLeft, TRight), TError>(errors), otherErrors => Fail<(TLeft, TRight), TError>(mergeFunc(errors, otherErrors)))
            );

        public static Result<(TLeft, TRight), TError> Plus<TLeft, TRight, TError>(this Result<TLeft, TError> left,
            Result<TRight, TError> right) where TError : IPlus<TError>
            => left.Plus(right, (leftError, rightError) => leftError.Plus(rightError));

        public static Result<TResult, TError> Plus<TLeft, TRight, TResult, TError>(this Result<TLeft, TError> left, Result<TRight, TError> right, Func<TLeft, TRight, TResult> plusFunc, Func<TError, TError, TError> mergeFunc)
            => left.Plus(right, mergeFunc).Map(result =>
            {
                var (leftValue, rightValue) = result;
                return plusFunc(leftValue, rightValue);
            });

        public static Result<T, TError> Plus<T, TError>(this Result<T, TError> left, Result<T, TError> right, Func<TError, TError, TError> mergeFunc) where T : IPlus<T>
            => left.Plus<T, T, TError>(right, mergeFunc).Map(result =>
            {
                var (leftValue, rightValue) = result;
                return leftValue.Plus(rightValue);
            });

        public static Result<T, TError> Plus<T, TError>(this Result<T, TError> left, Result<T, TError> right, Func<T, T, T> plusFunc) where TError : IPlus<TError>
            => left.Plus<T, T, TError>(right, (leftError, rightError) => leftError.Plus(rightError)).Map(result =>
            {
                var (leftValue, rightValue) = result;
                return plusFunc(leftValue, rightValue);
            });

        public static Result<T, TError> Plus<T, TError>(this Result<T, TError> left, Result<T, TError> right) where T : IPlus<T> where TError : IPlus<TError>
            => left.Plus<T, T, TError>(right, (leftError, rightError) => leftError.Plus(rightError)).Map(result =>
            {
                var (leftValue, rightValue) = result;
                return leftValue.Plus(rightValue);
            });

        //Plus on Result<T>
        public static Result<(TLeft, TRight)> Plus<TLeft, TRight>(this Result<TLeft> left, Result<TRight> right)
            => left.Match(
                leftValue => right.Match(rightValue => Ok((leftValue, rightValue)), Fail<(TLeft, TRight)>),
                errors => right.Match(rightValue => Fail<(TLeft, TRight)>(errors), otherErrors => Fail<(TLeft, TRight)>(errors.Concat(otherErrors)))
            );

        public static Result<TResult> Plus<TLeft, TRight, TResult>(this Result<TLeft> left, Result<TRight> right,Func<TLeft, TRight, TResult> plusFunc)
            => left.Plus(right).Map(result =>
            {
                var(leftValue, rightValue) = result;
                return plusFunc(leftValue, rightValue);
            });

        public static Result<T> Plus<T>(this Result<T> left, Result<T> right) where T : IPlus<T>
            => left.Plus<T, T>(right).Map(result =>
            {
                var (leftValue, rightValue) = result;
                return leftValue.Plus(rightValue);
            });

        //Fold on Result<T, TError>
        public static Result<T, TError> Fold<T, TError>(this IEnumerable<Result<T, TError>> values, Func<T, T, T> plusFunc, Func<TError, TError, TError> mergeFunc)
            => values.Aggregate((seed, input) => seed.Plus(input, plusFunc, mergeFunc));

        public static Result<T, TError> Fold<T, TError>(this IEnumerable<Result<T, TError>> values, Func<TError, TError, TError> mergeFunc) where T : IPlus<T>
            => values.Aggregate((seed, input) => seed.Plus<T, TError>(input, mergeFunc));

        public static Result<T, TError> Fold<T, TError>(this IEnumerable<Result<T, TError>> values, Func<T, T, T> plusFunc) where TError : IPlus<TError>
            => values.Aggregate((seed, input) => seed.Plus(input, plusFunc));

        public static Result<T, TError> Fold<T, TError>(this IEnumerable<Result<T, TError>> values) where T : IPlus<T> where TError : IPlus<TError>
            => values.Aggregate((seed, input) => seed.Plus<T, TError>(input));

        public static Result<TResult, TError> Fold<TResult, T, TError>(this IEnumerable<Result<T, TError>> values,
            Result<TResult, TError> seed, Func<TResult, T, TResult> aggrFunc, Func<TError, TError, TError> mergeFunc)
            => values.Aggregate(seed, (acc, value) => acc.Plus(value, mergeFunc).Map(x =>
            {
                var (finalAcc, finalVal) = x;
                return aggrFunc(finalAcc, finalVal);
            }));

        public static Result<TResult, TError> Fold<TResult, T, TError>(this IEnumerable<Result<T, TError>> values,
            TResult seed, Func<TResult, T, TResult> aggrFunc, Func<TError, TError, TError> mergeFunc)
            => values.Fold(seed.Ok<TResult, TError>(), aggrFunc, mergeFunc);

        public static Result<TResult, TError> Fold<TResult, T, TError>(this IEnumerable<Result<T, TError>> values,
            Result<TResult, TError> seed, Func<TResult, T, TResult> aggrFunc) where TError : IPlus<TError>
            => values.Aggregate(seed, (acc, value) => acc.Plus(value).Map(x =>
            {
                var (finalAcc, finalVal) = x;
                return aggrFunc(finalAcc, finalVal);
            }));

        public static Result<TResult, TError> Fold<TResult, T, TError>(this IEnumerable<Result<T, TError>> values,
            TResult seed, Func<TResult, T, TResult> aggrFunc) where TError : IPlus<TError>
            => values.Fold(seed.Ok<TResult, TError>(), aggrFunc);

        //Fold on Result<T>
        public static Result<T> Fold<T>(this IEnumerable<Result<T>> values, Func<T, T, T> plusFunc)
            => values.Aggregate((acc, item) => acc.Plus(item, plusFunc));

        public static Result<T> Fold<T>(this IEnumerable<Result<T>> values) where T : IPlus<T>
            => values.Aggregate((acc, item) => acc.Plus<T>(item));

        public static Result<TResult> Fold<TResult, T>(this IEnumerable<Result<T>> values,
            Result<TResult> seed, Func<TResult, T, TResult> aggrFunc)
            => values.Aggregate(seed, (acc, value) => acc.Plus(value).Map(x =>
            {
                var (finalAcc, finalVal) = x;
                return aggrFunc(finalAcc, finalVal);
            }));

        public static Result<TResult> Fold<TResult, T>(this IEnumerable<Result<T>> values,
            TResult seed, Func<TResult, T, TResult> aggrFunc)
            => values.Fold(seed.Ok(), aggrFunc);

        //Unroll on IEnumerable<Result<T>>
        public static Result<IEnumerable<T>> Unroll<T>(this IEnumerable<Result<T>> items)
            => items.Fold(new T[] { }.Ok<IEnumerable<T>>(), (acc, item) => acc.Concat(new[] { item }));

        //Unroll on IEnumerable<Result<T, TError>>
        public static Result<IEnumerable<T>, TError> Unroll<T, TError>(this IEnumerable<Result<T, TError>> values, Func<TError, TError, TError> mergeFunc)
            => values.Fold(new T[] { }.Ok<IEnumerable<T>, TError>(), (acc, item) => acc.Concat(new[] { item }), mergeFunc);

        //Bind on Result<T, TError>
        public static Result<TResult, TError> Bind<TResult, T, TError>(this Result<T, TError> value, Func<T, Result<TResult, TError>> bindFunc)
            => value.Match(bindFunc, errors => errors.Fail<TResult, TError>());

        public static Result<TResult> Bind<TResult, T>(this Result<T, IEnumerable<string>> value, Func<T, Result<TResult>> bindFunc)
            => value.Match(bindFunc, error => error.Fail<TResult>());

        public static VoidResult<TError> Bind<T, TError>(this Result<T, TError> value, Func<T, VoidResult<TError>> bindFunc)
            => value.Match(bindFunc, error => error.Fail());

        public static VoidResult Bind<T>(this Result<T, IEnumerable<string>> value, Func<T, VoidResult> bindFunc)
            => value.Match(bindFunc, error => error.Fail());

        //Bind on Result<T>
        public static Result<TResult> Bind<TResult, T>(this Result<T> value, Func<T, Result<TResult>> bindFunc)
            => value.Match(bindFunc, error => error.Fail<TResult>());

        public static VoidResult Bind<T>(this Result<T> value, Func<T, VoidResult> bindFunc)
            => value.Match(bindFunc, error => error.Fail());


        //Map on Result<T, TError>
        public static Result<TResult, TError> Map<TResult, T, TError>(this Result<T, TError> value, Func<T, TResult> mapFunc)
            => value.Map2(mapFunc, Id);

        public static Result<TResult, TErrorResult> Map2<TResult, T, TError, TErrorResult>(this Result<T, TError> value, Func<T, TResult> mapFunc, Func<TError, TErrorResult> errorMapFunc)
            => value.Match(
                success => mapFunc(success).Ok<TResult, TErrorResult>(),
                errors => errorMapFunc(errors).Fail<TResult, TErrorResult>());

        //Map on Result<T>
        public static Result<TResult> Map<T, TResult>(this Result<T> value, Func<T, TResult> mapFunc)
            => value.Map2(mapFunc, Id);

        public static Result<TResult, TError> Map2<TResult, T, TError>(this Result<T> value, Func<T, TResult> mapFunc, Func<IEnumerable<string>, TError> errorMapFunc)
            => value.Match(
                    success => mapFunc(success).Ok<TResult, TError>(),
                    errors => errorMapFunc(errors).Fail<TResult, TError>());

        //Flatten on Result<T, TError>
        public static Result<T, TError> Flatten<T, TError>(this Result<Result<T, TError>, TError> value)
            => value.Match(Id, errors => errors.Fail<T, TError>());

        public static VoidResult<TError> Flatten<TError>(this Result<VoidResult<TError>, TError> value)
            => value.Match(Id, errors => errors.Fail());

        //Flatten on Result<T>
        public static Result<T> Flatten<T>(this Result<Result<T>> value)
            => value.Match(Id, errors => errors.Fail<T>());

        public static VoidResult Flatten(this Result<VoidResult> value)
            => value.Match(Id, errors => errors.Fail());

        //Tee on Result<T, TError>
        public static Result<T, TError> Tee<T, TError>(this Result<T, TError> value, Action<T> teeAction)
            => value.Map(x =>
            {
                teeAction(x);
                return x;
            });

        //Tee on Result<T>
        public static Result<T> Tee<T>(this Result<T> value, Action<T> teeAction)
            => value.Map(x =>
            {
                teeAction(x);
                return x;
            });

        //BindAsync on Result<T, TError>
        public static Task<Result<TResult, TError>> BindAsync<TResult, T, TError>(this Result<T, TError> value, Func<T, Task<Result<TResult, TError>>> bindFunc)
            => value.Match(bindFunc, errors => Task.FromResult(errors.Fail<TResult, TError>()));

        public static Task<Result<TResult>> BindAsync<TResult, T>(this Result<T, IEnumerable<string>> value, Func<T, Task<Result<TResult>>> bindFunc)
            => value.Match(bindFunc, error => Task.FromResult(error.Fail<TResult>()));

        public static Task<VoidResult<TError>> BindAsync<T, TError>(this Result<T, TError> value, Func<T, Task<VoidResult<TError>>> bindFunc)
            => value.Match(bindFunc, error => Task.FromResult(error.Fail()));

        public static Task<VoidResult> BindAsync<T>(this Result<T, IEnumerable<string>> value, Func<T, Task<VoidResult>> bindFunc)
            => value.Match(bindFunc, error => Task.FromResult(error.Fail()));

        public static Task<Result<TResult, TError>> BindAsync<TResult, T, TError>(this Task<Result<T, TError>> value,
            Func<T, Result<TResult, TError>> bindFunc)
            => value.WrapAsync(item => item.Bind(bindFunc));

        public static Task<Result<TResult>> BindAsync<TResult, T>(this Task<Result<T, IEnumerable<string>>> value, Func<T, Result<TResult>> bindFunc)
            => value.WrapAsync(item => item.Bind(bindFunc));

        public static Task<VoidResult<TError>> BindAsync<T, TError>(this Task<Result<T, TError>> value, Func<T, VoidResult<TError>> bindFunc)
            => value.WrapAsync(item => item.Bind(bindFunc));

        public static Task<VoidResult> BindAsync<T>(this Task<Result<T, IEnumerable<string>>> value, Func<T, VoidResult> bindFunc)
            => value.WrapAsync(item => item.Bind(bindFunc));

        public static Task<Result<TResult, TError>> BindAsync<TResult, T, TError>(this Task<Result<T, TError>> value, Func<T, Task<Result<TResult, TError>>> bindFunc)
            => value.WrapAsync(item => item.BindAsync(bindFunc));

        public static Task<Result<TResult>> BindAsync<TResult, T>(this Task<Result<T, IEnumerable<string>>> value, Func<T, Task<Result<TResult>>> bindFunc)
            => value.WrapAsync(item => item.BindAsync(bindFunc));

        public static Task<VoidResult<TError>> BindAsync<T, TError>(this Task<Result<T, TError>> value, Func<T, Task<VoidResult<TError>>> bindFunc)
            => value.WrapAsync(item => item.BindAsync(bindFunc));

        public static Task<VoidResult> BindAsync<T>(this Task<Result<T, IEnumerable<string>>> value, Func<T, Task<VoidResult>> bindFunc)
            => value.WrapAsync(item => item.BindAsync(bindFunc));

        //BindAsync on Result<T>
        public static Task<Result<TResult>> BindAsync<TResult, T>(this Result<T> value, Func<T, Task<Result<TResult>>> bindFunc)
            => value.Match(bindFunc, error => Task.FromResult(error.Fail<TResult>()));

        public static Task<VoidResult> BindAsync<T>(this Result<T> value, Func<T, Task<VoidResult>> bindFunc)
            => value.Match(bindFunc, error => Task.FromResult(error.Fail()));

        public static Task<Result<TResult>> BindAsync<TResult, T>(this Task<Result<T>> value, Func<T, Result<TResult>> bindFunc)
            => value.WrapAsync(item => item.Bind(bindFunc));

        public static Task<VoidResult> BindAsync<T>(this Task<Result<T>> value, Func<T, VoidResult> bindFunc)
            => value.WrapAsync(item => item.Bind(bindFunc));

        public static Task<Result<TResult>> BindAsync<TResult, T>(this Task<Result<T>> value, Func<T, Task<Result<TResult>>> bindFunc)
            => value.WrapAsync(item => item.BindAsync(bindFunc));

        public static Task<VoidResult> BindAsync<T>(this Task<Result<T>> value, Func<T, Task<VoidResult>> bindFunc)
            => value.WrapAsync(item => item.BindAsync(bindFunc));

        //MapAsync on Result<T, TError>
        public static Task<Result<TResult, TError>> MapAsync<TResult, T, TError>(this Result<T, TError> value, Func<T, Task<TResult>> mapFunc)
            => value.Map2Async(mapFunc, Task.FromResult);

        public static Task<Result<TResult, TErrorResult>> Map2Async<TResult, T, TError, TErrorResult>(
            this Result<T, TError> value, Func<T, Task<TResult>> mapFunc, Func<TError, Task<TErrorResult>> errorMapFunc)
            => value.Match(
                async success => (await mapFunc(success).ConfigureAwait(false)).Ok<TResult, TErrorResult>(),
                async error => (await errorMapFunc(error).ConfigureAwait(false)).Fail<TResult, TErrorResult>());

        public static Task<Result<TResult, TError>> MapAsync<TResult, T, TError>(this Task<Result<T, TError>> value, Func<T, TResult> mapFunc)
            => value.WrapAsync(item => item.Map(mapFunc));

        public static Task<Result<TResult, TErrorResult>> Map2Async<TResult, T, TError, TErrorResult>(this Task<Result<T, TError>> value, Func<T, TResult> mapFunc, Func<TError, TErrorResult> errorMapFunc)
            => value.WrapAsync(item => item.Map2(mapFunc, errorMapFunc));

        public static Task<Result<TResult, TError>> MapAsync<TResult, T, TError>(this Task<Result<T, TError>> value, Func<T, Task<TResult>> mapFunc)
            => value.WrapAsync(item => item.MapAsync(mapFunc));

        public static Task<Result<TResult, TErrorResult>> Map2Async<TResult, T, TError, TErrorResult>(
            this Task<Result<T, TError>> value, Func<T, Task<TResult>> mapFunc, Func<TError, Task<TErrorResult>> errorMapFunc)
            => value.WrapAsync(item => item.Map2Async(mapFunc, errorMapFunc));

        //MapAsync on Result<T>
        public static async Task<Result<TResult>> MapAsync<T, TResult>(this Result<T> value, Func<T, Task<TResult>> mapFunc)
            => await value.Map2Async(mapFunc, Task.FromResult).ConfigureAwait(false);

        public static Task<Result<TResult, TError>> Map2Async<TResult, T, TError>(this Result<T> value, Func<T, Task<TResult>> mapFunc, Func<IEnumerable<string>, Task<TError>> errorMapFunc)
            => value.Match(
                async success => (await mapFunc(success).ConfigureAwait(false)).Ok<TResult, TError>(),
                async errors => (await errorMapFunc(errors).ConfigureAwait(false)).Fail<TResult, TError>());

        public static Task<Result<TResult, TError>> Map2Async<TResult, TError>(this VoidResult value, Func<Task<TResult>> mapFunc, Func<IEnumerable<string>, Task<TError>> errorMapFunc)
            => value.Match(
                async success => (await mapFunc().ConfigureAwait(false)).Ok<TResult, TError>(),
                async errors => (await errorMapFunc(errors).ConfigureAwait(false)).Fail<TResult, TError>());

        public static Task<Result<TResult>> MapAsync<T, TResult>(this Task<Result<T>> value, Func<T, TResult> mapFunc)
            => value.WrapAsync(item => item.Map(mapFunc));

        public static Task<Result<TResult, TError>> Map2Async<TResult, T, TError>(this Task<Result<T>> value, Func<T, TResult> mapFunc, Func<IEnumerable<string>, TError> errorMapFunc)
            => value.WrapAsync(item => item.Map2(mapFunc, errorMapFunc));

        public static Task<Result<TResult>> MapAsync<T, TResult>(this Task<Result<T>> value, Func<T, Task<TResult>> mapFunc)
            => value.WrapAsync(item => item.MapAsync(mapFunc));

        public static Task<Result<TResult, TError>> Map2Async<TResult, T, TError>(this Task<Result<T>> value, Func<T, Task<TResult>> mapFunc, Func<IEnumerable<string>, Task<TError>> errorMapFunc)
            => value.WrapAsync(item => item.Map2Async(mapFunc, errorMapFunc));

        //TeeAsync on Result<T, TError>
        public static async Task<Result<T, TError>> TeeAsync<T, TError>(this Result<T, TError> value, Func<T, Task> asyncFunc)
            => await value.MapAsync(async x =>
            {
                await asyncFunc(x).ConfigureAwait(false);
                return x;
            }).ConfigureAwait(false);

        public static Task<Result<T, TError>> TeeAsync<T, TError>(this Task<Result<T, TError>> value, Action<T> action)
            => value.WrapAsync(item => item.Tee(action));

        public static Task<Result<T, TError>> TeeAsync<T, TError>(this Task<Result<T, TError>> value, Func<T, Task> asyncFunc)
            => value.WrapAsync(item => item.TeeAsync(asyncFunc));

        //TeeAsync on Result<T>
        public static async Task<Result<T>> TeeAsync<T>(this Result<T> value, Func<T, Task> asyncFunc)
            => await value.MapAsync(async x =>
            {
                await asyncFunc(x).ConfigureAwait(false);
                return x;
            }).ConfigureAwait(false);

        public static Task<Result<T>> TeeAsync<T>(this Task<Result<T>> value, Action<T> action)
            => value.WrapAsync(item => item.Tee(action));

        public static Task<Result<T>> TeeAsync<T>(this Task<Result<T>> value, Func<T, Task> asyncFunc)
            => value.WrapAsync(item => item.TeeAsync(asyncFunc).AsTask());

        //FlattenAsync on Result<T, TError>
        public static Task<Result<T, TError>> FlattenAsync<T, TError>(this Task<Result<Result<T, TError>, TError>> value)
            => value.WrapAsync(item => item.Flatten());

        public static Task<VoidResult<TError>> FlattenAsync<TError>(this Task<Result<VoidResult<TError>, TError>> value)
            => value.WrapAsync(item => item.Flatten());

        //FlattenAsync on Result<T>
        public static Task<Result<T>> FlattenAsync<T>(this Task<Result<Result<T>>> value)
            => value.WrapAsync(item => item.Flatten());

        public static Task<VoidResult> FlattenAsync(this Task<Result<VoidResult>> value)
            => value.WrapAsync(item => item.Flatten());

        //UnwrapAsync on Result<T, TError>
        public static Task<Result<T, TError>> UnwrapAsync<T, TError>(this Result<Task<T>, TError> value)
            => value.Match(
                async item => (await item.ConfigureAwait(false)).Ok<T, TError>(),
                errors => Task.FromResult(errors.Fail<T, TError>()));

        public static Task<Result<T, TError>> UnwrapAsync<T, TError>(this Result<T, Task<TError>> value)
            => value.Match(
                item => Task.FromResult(item.Ok<T, TError>()),
                async errors => (await errors.ConfigureAwait(false)).Fail<T, TError>());


        //UnwrapAsync on Result<T>
        public static Task<Result<T>> UnwrapAsync<T>(this Result<Task<T>> value)
            => value.Match(
                async item => (await item.ConfigureAwait(false)).Ok(),
                errors => Task.FromResult(errors.Fail<T>()));
    }
}
