using System.Diagnostics.CodeAnalysis;

namespace SwiftState;

public class State<TInput, TId>(TId id) : IStateTransitionHandler<TInput, TId>
{
    private Transitions<TInput, TId>? _transitions;
    
    public TId Id { get; } = id;
    
    public State<TInput, TId>? DefaultState => Transitions.DefaultState;

    public bool HasTransitions => Transitions.HasTransitions;

    internal Transitions<TInput, TId> Transitions
    {
        get => _transitions ?? throw new InvalidOperationException("State transitions not initialized");
        set => _transitions = value;
    }

    public bool TryTransition(TInput input, [NotNullWhen(true)] out State<TInput, TId>? nextState)
    {
        if (Transitions.DirectInputTransitions.TryGetValue(input, out nextState))
            return true;

        if (Transitions.TryConditionalTransitions?.Invoke(input, out nextState) ?? false)
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