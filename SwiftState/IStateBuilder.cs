using System.Linq.Expressions;

namespace SwiftState;

public interface IStateBuilder<TInput, TId>
{
    bool Terminal { get; set; }
    bool HasTransitions { get; }
    
    IStateBuilder<TInput, TId> When(TInput input, TId id, bool terminal = false);
    void When(TInput input, IStateBuilder<TInput, TId> stateBuilder);

    IStateBuilder<TInput, TId> When(Expression<Func<TInput, bool>> condition, TId id, bool terminal = false);

    void When(Expression<Func<TInput, bool>> condition, IStateBuilder<TInput, TId> stateBuilder);

    IStateBuilder<TInput, TId> Default(TId id, bool terminal = false);

    void Default(IStateBuilder<TInput, TId> defaultStateBuilder);

    void ClearTransitions();

    State<TInput, TId> Build();
}