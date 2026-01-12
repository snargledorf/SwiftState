namespace SwiftState;

public record Transitions<TInput, TData>(
    TryConditionalTransitionsDelegate<TInput, TData>? TryConditionalTransitions,
    State<TInput, TData>? DefaultState)
{
    public bool HasTransitions { get; } = TryConditionalTransitions is not null ||
                                          DefaultState is not null;

    public bool HasInputTransitions { get; } = TryConditionalTransitions is not null;
    
    public bool HasDefaultTransition { get; } = DefaultState is not null;
}