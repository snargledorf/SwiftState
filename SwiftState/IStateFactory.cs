namespace SwiftState;

public interface IStateFactory<TInput, in TData, TState>
{
    static abstract TState CreateState(Transitions<TInput, TState> transitions, TData data);
}