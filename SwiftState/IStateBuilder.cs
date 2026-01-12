using System.Linq.Expressions;

namespace SwiftState;

public interface IStateBuilder<TInput, TId>
{
    TId Id { get; }
    
    bool Terminal { get; set; }
    
    bool HasTransitions { get; }
    
    IStateBuilder<TInput, TId> When(TInput input, TId id, bool terminal = false);

    IStateBuilder<TInput, TId> When(Expression<Func<TInput, bool>> condition, TId id, bool terminal = false);

    IStateBuilder<TInput, TId> GotoWhen(TId id, params TInput[] inputs) => GotoWhen(id, false, inputs);

    IStateBuilder<TInput, TId> GotoWhen(TId id, bool terminal, params TInput[] inputs);

    IStateBuilder<TInput, TId> Default(TId id, bool terminal = false);
    
    IStateBuilder<TInput, TId> GetBuilderForState(TId id, bool terminal = false);

    void ClearTransitions();

    State<TInput, TId> Build();
}