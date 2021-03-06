﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Resultful.Utils;
using OneOf;

namespace Resultful
{
    public struct VoidResult
    {
        //Private Members
        private OneOf<Unit, IEnumerable<string>> _value;

        //Constructors
        internal VoidResult(Unit value)
            => _value = value;

        internal VoidResult(IEnumerable<string> value)
            => _value = OneOf<Unit, IEnumerable<string>>.FromT1(value);

        //Implicit Converters
        public static implicit operator VoidResult(Result<Unit, IEnumerable<string>> value)
            => value.Match(_ => Result.Ok(), Result.Fail);

        public static implicit operator VoidResult(Result<Unit> value)
            => value.Match(_ => Result.Ok(), Result.Fail);

        public static implicit operator VoidResult(VoidResult<IEnumerable<string>> value)
            => value.Match(_ => Result.Ok(), Result.Fail);

        public static implicit operator VoidResult(string[] value)
            => Result.Fail(value);

        public static implicit operator VoidResult(List<string> value)
            => Result.Fail(value.ToArray());

        public static implicit operator VoidResult(string value)
            => Result.Fail(value);

        public static implicit operator VoidResult(Unit value)
            => value.Ok();

        //Local Methods
        public TResult Match<TResult>(Func<Unit, TResult> successfulFunc, Func<IEnumerable<string>, TResult> errorFunc)
            => _value.Match(
                successfulFunc.ThrowIfDefault(nameof(successfulFunc)),
                errorFunc.ThrowIfDefault(nameof(errorFunc)));

        public void Switch(Action<Unit> successfulFunc, Action<IEnumerable<string>> errorFunc)
            => _value.Switch(
                successfulFunc.ThrowIfDefault(nameof(successfulFunc)),
                errorFunc.ThrowIfDefault(nameof(errorFunc)));

        public void SwitchAsync(Func<Unit, Task> successfulFunc, Func<IEnumerable<string>, Task> errorFunc)
            => _value.Match(
                successfulFunc.ThrowIfDefault(nameof(successfulFunc)),
                errorFunc.ThrowIfDefault(nameof(errorFunc)));

        public Task<TResult> MatchAsync<TResult>(Func<Unit, Task<TResult>> successfulFunc, Func<IEnumerable<string>, Task<TResult>> errorFunc) =>
            _value.Match(
                successfulFunc.ThrowIfDefault(nameof(successfulFunc)),
                errorFunc.ThrowIfDefault(nameof(errorFunc)));

        public OneOf<Unit, IEnumerable<string>> ToOneOf() => _value;

        public Result<TResult> Map<TResult>(Func<Unit, TResult> mapFunc)
            => Map2(Result.Id, mapFunc);

        public Task<Result<TResult>> MapAsync<TResult>(Func<Unit, Task<TResult>> mapFunc)
            => BindValueAsync(async _ => (await mapFunc.ThrowIfDefault(nameof(mapFunc))(_).ConfigureAwait(false)).Ok());

        public Result<T> Map<T>(T value)
            => Map(_ => value);

        public Task<Result<T>> MapAsync<T>(Task<T> value)
            => MapAsync(_ => value);

        public Result<TResult, TError> Map2<TResult, TError>(Func<IEnumerable<string>, TError> errorMapFunc, Func<Unit, TResult> mapFunc)
            => Match(
                success => mapFunc.ThrowIfDefault(nameof(mapFunc))(success).Ok<TResult, TError>(),
                errors => errorMapFunc.ThrowIfDefault(nameof(errorMapFunc))(errors).Fail<TResult, TError>());

        public Task<Result<TResult, TError>> Map2Async<TResult, TError>(Func<IEnumerable<string>, Task<TError>> errorMapFunc, Func<Task<TResult>> mapFunc)
            => Match(
                async success => (await mapFunc().ConfigureAwait(false)).Ok<TResult, TError>(),
                async errors => (await errorMapFunc(errors).ConfigureAwait(false)).Fail<TResult, TError>());

        public VoidResult<TErrorResult> MapError<TErrorResult>(Func<IEnumerable<string>, TErrorResult> errorMapFunc)
            => Map2(errorMapFunc, Result.Id);

        public Task<VoidResult<TErrorResult>> MapErrorAsync<TErrorResult>(Func<IEnumerable<string>, Task<TErrorResult>> errorMapFunc)
            => Match(
                success => Task.FromResult(Result.Ok<TErrorResult>()),
                async errors => (await errorMapFunc(errors).ConfigureAwait(false)).Fail());

        public VoidResult MapError(Func<IEnumerable<string>, IEnumerable<string>> errorMapFunc)
            => Map2(errorMapFunc, Result.Id);

        public Task<VoidResult> MapErrorAsync(Func<IEnumerable<string>, Task<IEnumerable<string>>> errorMapFunc)
            => Match(
                success => Task.FromResult(Result.Ok()),
                async errors => (await errorMapFunc(errors).ConfigureAwait(false)).Fail());



        public VoidResult Bind(Func<Unit, VoidResult> bindFunc)
            => Match(bindFunc.ThrowIfDefault(nameof(bindFunc)), Result.Fail);

        public Task<VoidResult> BindAsync(Func<Unit, Task<VoidResult>> bindFunc)
            => Match(
                x => bindFunc.ThrowIfDefault(nameof(bindFunc))(Unit.Value),
                error => Task.FromResult(error.Fail()));

        public Result<T> BindValue<T>(Func<Unit, Result<T>> bindFunc)
            => Match(bindFunc.ThrowIfDefault(nameof(bindFunc)), Result.Fail<T>);

        public Task<Result<T>> BindValueAsync<T>(Func<Unit, Task<Result<T>>> bindFunc)
            => Match(
                bindFunc.ThrowIfDefault(nameof(bindFunc)),
                error => Task.FromResult(error.Fail<T>()));

        public VoidResult Tee(Action<Unit> action)
            => Map(_ =>
            {
                action.ThrowIfDefault(nameof(action))(_);
                return _;
            });

        public async Task<VoidResult> TeeAsync(Func<Unit, Task> asyncFunc)
            => await MapAsync(async _ =>
            {
                await asyncFunc.ThrowIfDefault(nameof(asyncFunc))(_).ConfigureAwait(false);
                return Unit.Value;
            }).ConfigureAwait(false);

        public VoidResult TeeError(Action<IEnumerable<string>> action)
            => MapError(error =>
            {
                action.ThrowIfDefault(nameof(action))(error);
                return error;
            });

        public async Task<VoidResult> TeeErrorAsync(Func<IEnumerable<string>, Task> asyncFunc)
            => await MapErrorAsync(async error =>
            {
                await asyncFunc.ThrowIfDefault(nameof(asyncFunc))(error).ConfigureAwait(false);
                return error;
            }).ConfigureAwait(false);
    }
}
