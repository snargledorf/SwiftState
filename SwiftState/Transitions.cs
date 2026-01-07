namespace SwiftState;

public record Transitions<TInput, TData>(
    IReadOnlyDictionary<TInput, State<TInput, TData>> DirectInputTransitions,
    TryConditionalTransitionsDelegate<TInput, TData>? TryConditionalTransitions,
    State<TInput, TData>? DefaultState)
{
    public bool HasTransitions { get; } = DirectInputTransitions.Count > 0 || 
                                          TryConditionalTransitions is not null ||
                                          DefaultState is not null;
}