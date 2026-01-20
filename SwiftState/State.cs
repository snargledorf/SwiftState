using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SwiftState;

public sealed record State<TInput, TId>(TId Id, bool IsTerminal) : IStateTransitionHandler<TInput, TId> where TInput : notnull
{
    private Transitions<TInput, TId>? _transitions;
    
    public State<TInput, TId>? DefaultState => Transitions?.DefaultState;

    public bool HasTransitions => Transitions?.HasTransitions ?? false;

    public bool HasInputTransitions => Transitions?.HasInputTransitions ?? false;
    
    public bool HasDefaultTransition => Transitions?.HasDefaultTransition ?? false;

    internal Transitions<TInput, TId>? Transitions
    {
        get => _transitions;
        set
        {
            if (IsTerminal)
                throw new InvalidOperationException("Terminal states cannot have transitions");
            
            _transitions = value;
        }
    }

    public bool TryTransition(TInput input, [NotNullWhen(true)] out State<TInput, TId>? nextState)
    {
        if (IsTerminal)
        {
            nextState = null;
            return false;
        }
        
        CheckTransitionsAreInitialized();

        if (Transitions.TryConditionalTransitions?.TryGet(input, out nextState) ?? false)
            return true;
        
        nextState = DefaultState;
        return nextState is not null;
    }

    public bool TryGetDefault([NotNullWhen(true)] out State<TInput, TId>? defaultState)
    {
        if (IsTerminal)
        {
            defaultState = null;
            return false;
        }

        CheckTransitionsAreInitialized();
        defaultState = DefaultState;
        return defaultState is not null;
    }
    
    [MemberNotNull(nameof(Transitions))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckTransitionsAreInitialized()
    {
        if (Transitions is null)
            throw new InvalidOperationException("State transitions not initialized");
    }
}