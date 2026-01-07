using System.Diagnostics.CodeAnalysis;

namespace SwiftState;

public class State<TInput, TId>(TId id, Transitions<TInput, TId> transitions):IStateTransitionHandler<TInput, TId>
{
    public TId Id { get; } = id;
    
    public State<TInput, TId>? DefaultState { get; } = transitions.DefaultState;

    public bool HasTransitions { get; } = transitions.HasTransitions;

    public bool TryTransition(TInput input, [NotNullWhen(true)] out State<TInput, TId>? nextState)
    {
        if (transitions.DirectInputTransitions.TryGetValue(input, out nextState))
            return true;

        if (transitions.TryConditionalTransitions?.Invoke(input, out nextState) ?? false)
            return true;
        
        nextState = DefaultState;
        return nextState is not null;
    }

    public bool TryGetDefault([NotNullWhen(true)] out State<TInput, TId>? defaultState)
    {
        defaultState = DefaultState;
        return defaultState is not null;
    }
}