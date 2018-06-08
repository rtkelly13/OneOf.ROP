﻿using System;
using System.Threading.Tasks;
using OneOf.ROP.Utils;
using OneOf.Types;

namespace OneOf.ROP
{
    public struct Option<T>
    {
        //Private Members
        private OneOf<T, None> _value;

        //Constructors
        private Option(T value)
            => _value = value;

        private Option(None value)
            => _value = value;

        //Implicit Converters
        public static implicit operator Option<T>(T value)
            => new Option<T>(value);

        public static implicit operator Option<T>(None value)
            => new Option<T>(value);

        //Builder
        public static Option<T> None
            => new Option<T>(new None());

        //Local Methods
        public void Switch(Action<T> successfulFunc, Action<None> errorFunc)
            => _value.Switch(successfulFunc.ThrowIfDefault(nameof(successfulFunc)), errorFunc.ThrowIfDefault(nameof(errorFunc)));

        public TResult Match<TResult>(Func<T, TResult> successfulFunc, Func<None, TResult> errorFunc) =>
            _value.Match(successfulFunc.ThrowIfDefault(nameof(successfulFunc)), errorFunc.ThrowIfDefault(nameof(errorFunc)));

        public OneOf<T, None> ToOneOf() => _value;

        public Option<TResult> Bind<TResult>(Func<T, Option<TResult>> bindFunc)
            => Match(bindFunc.ThrowIfDefault(nameof(bindFunc)), none => none);

        public Task<Option<TResult>> BindAsync<TResult>(Func<T, Task<Option<TResult>>> bindFunc)
            => Match(bindFunc.ThrowIfDefault(nameof(bindFunc)), none => Task.FromResult<Option<TResult>>(none));

        public Option<TResult> Map<TResult>(Func<T, TResult> bindFunc)
            => Match(value => bindFunc.ThrowIfDefault(nameof(bindFunc))(value).Some(), none => none);

        public Task<Option<TResult>> MapAsync<TResult>(Func<T, Task<TResult>> bindFunc)
            => Match(
                async item2 => (await bindFunc.ThrowIfDefault(nameof(bindFunc))(item2).ConfigureAwait(false)).Some(),
                none => Task.FromResult<Option<TResult>>(none));

        public Option<T> Tee(Action<T> teeAction)
            => Map(value =>
            {
                teeAction.ThrowIfDefault(nameof(teeAction))(value);
                return value;
            });

        public Task<Option<T>> TeeAsync(Func<T, Task> asyncFunc)
            => MapAsync(async x =>
            {
                await asyncFunc.ThrowIfDefault(nameof(asyncFunc))(x).ConfigureAwait(false);
                return x;
            });
    }
}
