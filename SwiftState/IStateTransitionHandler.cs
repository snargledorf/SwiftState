using System.Diagnostics.CodeAnalysis;

namespace SwiftState;

public interface IStateTransitionHandler<TInput, TId>
{
    State<TInput, TId>? DefaultState { get; }
    bool HasTransitions { get; }
    bool TryTransition(TInput input, [NotNullWhen(true)] out State<TInput, TId>? nextState);
    bool TryGetDefault([NotNullWhen(true)] out State<TInput, TId>? defaultState);
}