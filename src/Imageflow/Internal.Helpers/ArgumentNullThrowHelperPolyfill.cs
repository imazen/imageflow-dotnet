using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Imageflow.Internal.Helpers;

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Imazen licenses any changes to th

internal static class Argument
{
    /// <summary>Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.</summary>
    /// <param name="argument">The reference type argument to validate as non-null.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
    public static void ThrowIfNull(
#if INTERNAL_NULLABLE_ATTRIBUTES || NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        [NotNull]
#endif
        object? argument, [CallerArgumentExpression("argument")] string? paramName = null)
    {
#if !NET7_0_OR_GREATER || NETSTANDARD || NETFRAMEWORK
        if (argument is null)
        {
            Throw(paramName);
        }
#else
        ArgumentNullException.ThrowIfNull(argument, paramName);
#endif
    }

#if !NET7_0_OR_GREATER || NETSTANDARD || NETFRAMEWORK
#if INTERNAL_NULLABLE_ATTRIBUTES || NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    [DoesNotReturn]
#endif
    internal static void Throw(string? paramName) =>
        throw new ArgumentNullException(paramName);
#endif
}

internal static class ObjectDisposedHelper
{
    [StackTraceHidden]
    public static void ThrowIf([DoesNotReturnIf(true)] bool condition, object instance)
    {
#if NET8_0_OR_GREATER

        ObjectDisposedException.ThrowIf(condition, instance);
#else
        if (condition)
        {
            throw new ObjectDisposedException(instance?.GetType().Name);
        }
#endif
    }
}
