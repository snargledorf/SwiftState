using System.Linq.Expressions;

namespace SwiftState;

public interface IStateBuilder<TInput, TData>
{
    IStateBuilder<TInput, TData> When(TInput input, TData data);
    void When(TInput input, IStateBuilder<TInput, TData> stateBuilder);

    IStateBuilder<TInput, TData> When(Expression<Func<TInput, bool>> condition, TData data);

    void When(Expression<Func<TInput, bool>> condition, IStateBuilder<TInput, TData> stateBuilder);

    IStateBuilder<TInput, TData> Default(TData data);

    void Default(IStateBuilder<TInput, TData> defaultStateBuilder);

    State<TInput, TData> Build();
}