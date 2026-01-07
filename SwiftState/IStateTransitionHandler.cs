using System.Diagnostics.CodeAnalysis;

namespace SwiftState;

public interface IStateTransitionHandler<TInput, TData>
{
    State<TInput, TData>? DefaultState { get; }
    bool HasTransitions { get; }
    bool TryTransition(TInput input, [NotNullWhen(true)] out State<TInput, TData>? nextState);
    bool TryGetDefault([NotNullWhen(true)] out State<TInput, TData>? defaultState);
}