using System.Diagnostics.CodeAnalysis;

namespace SwiftState;

public delegate bool TryConditionalTransitionsDelegate<TInput, TData>(
    TInput input, 
    [NotNullWhen(true)] out State<TInput, TData>? newState);