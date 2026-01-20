using System.Linq.Expressions;

namespace SwiftState;

public interface IStateBuilder<TInput, TId> where TInput : notnull
{
    TId Id { get; }
    
    bool Terminal { get; set; }
    
    bool HasTransitions { get; }
    
    IStateBuilder<TInput, TId> When(TInput input, TId id, bool terminal = false);

    IStateBuilder<TInput, TId> When(Expression<Predicate<TInput>> condition, TId id, bool terminal = false);

    IStateBuilder<TInput, TId> GotoWhen(TId id, params TInput[] inputs) => GotoWhen(id, false, inputs);

    IStateBuilder<TInput, TId> GotoWhen(TId id, bool terminal, params TInput[] inputs);

    IStateBuilder<TInput, TId> GotoWhen(TId id, params Expression<Predicate<TInput>>[] conditions) =>
        GotoWhen(id, false, conditions);
    
    IStateBuilder<TInput, TId> GotoWhen(TId id, bool terminal, params Expression<Predicate<TInput>>[] conditions);

    IStateBuilder<TInput, TId> Default(TId id, bool terminal = false);
    
    IStateBuilder<TInput, TId> GetBuilderForState(TId id, bool terminal = false);

    void ClearTransitions();

    State<TInput, TId> Build();
}