using PredicateMap;

namespace SwiftState;

internal record Transitions<TInput, TId>(
    IPredicateMap<TInput, State<TInput, TId>>? TryConditionalTransitions,
    State<TInput, TId>? DefaultState) where TInput : notnull
{
    public bool HasTransitions { get; } = TryConditionalTransitions is not null ||
                                          DefaultState is not null;

    public bool HasInputTransitions { get; } = TryConditionalTransitions is not null;
    
    public bool HasDefaultTransition { get; } = DefaultState is not null;
}