using System.Linq.Expressions;

namespace SwiftState;

public interface IStateBuilder<TInput, TData>
{
    bool Terminal { get; set; }
    bool HasTransitions { get; }
    
    IStateBuilder<TInput, TData> When(TInput input, TData data, bool terminal = false);
    void When(TInput input, IStateBuilder<TInput, TData> stateBuilder);

    IStateBuilder<TInput, TData> When(Expression<Func<TInput, bool>> condition, TData data, bool terminal = false);

    void When(Expression<Func<TInput, bool>> condition, IStateBuilder<TInput, TData> stateBuilder);

    IStateBuilder<TInput, TData> Default(TData data, bool terminal = false);

    void Default(IStateBuilder<TInput, TData> defaultStateBuilder);

    void ClearTransitions();

    State<TInput, TData> Build();
}