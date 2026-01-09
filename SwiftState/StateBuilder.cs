using System.Collections.Frozen;
using System.Linq.Expressions;

namespace SwiftState;

public class StateBuilder<TInput, TData>(TData stateData, bool terminal = false) : IStateBuilder<TInput, TData>
    where TInput : notnull
{
    private readonly Dictionary<TInput, IStateBuilder<TInput, TData>> _inputToStateBuilder = new();
    private readonly HashSet<ConditionToStateBuilder> _conditionToStateBuilders = [];

    private IStateBuilder<TInput, TData>? _defaultStateBuilder;

    private State<TInput, TData>? _state;
    
    private bool _terminal = terminal;

    public bool Terminal
    {
        get => _terminal;
        set
        {
            CheckIfAlreadyBuilt();
            if (HasTransitions)
                throw new InvalidOperationException("Cannot change terminal state after transitions are defined");
            _terminal = value;
        }
    }

    public bool HasTransitions => _inputToStateBuilder.Count > 0 || _conditionToStateBuilders.Count > 0 || _defaultStateBuilder is not null;

    public IStateBuilder<TInput, TData> When(TInput input, TData data, bool terminal = false)
    {
        IStateBuilder<TInput, TData> stateBuilder = CreateStateBuilder(data, terminal);
        When(input, stateBuilder);
        return stateBuilder;
    }

    public void When(TInput input, IStateBuilder<TInput, TData> stateBuilder)
    {
        CheckIfAlreadyBuilt();
        CheckIfTerminal();
        _inputToStateBuilder[input] = stateBuilder;
    }

    public IStateBuilder<TInput, TData> When(Expression<Func<TInput, bool>> condition, TData data, bool terminal = false)
    {
        IStateBuilder<TInput, TData> tokenTypeStateBuilder = CreateStateBuilder(data, terminal);
        When(condition, tokenTypeStateBuilder);
        return tokenTypeStateBuilder;
    }

    public void When(Expression<Func<TInput, bool>> condition, IStateBuilder<TInput, TData> stateBuilder)
    {
        CheckIfAlreadyBuilt();
        CheckIfTerminal();
        _conditionToStateBuilders.Add(new ConditionToStateBuilder(condition, stateBuilder));
    }

    public IStateBuilder<TInput, TData> Default(TData data, bool terminal = false)
    {
        IStateBuilder<TInput, TData> defaultTokenStateBuilder = CreateStateBuilder(data, terminal);
        Default(defaultTokenStateBuilder);
        return defaultTokenStateBuilder;
    }

    public void Default(IStateBuilder<TInput, TData> defaultStateBuilder)
    {
        CheckIfAlreadyBuilt();
        CheckIfTerminal();
        _defaultStateBuilder = defaultStateBuilder;
    }

    public void ClearTransitions()
    {
        CheckIfAlreadyBuilt();
        
        _defaultStateBuilder = null;
        _inputToStateBuilder.Clear();
        _conditionToStateBuilders.Clear();
    }

    public State<TInput, TData> Build()
    {
        if (_state is { } state)
            return state;
        
        _state = new State<TInput, TData>(stateData, Terminal);

        if (Terminal)
            return _state;

        TryConditionalTransitionsDelegate<TInput, TData>? tryConditionalTransitions =
            _conditionToStateBuilders.Count > 0 ? BuildTryConditionalTransitionsDelegate() : null;

        FrozenDictionary<TInput, State<TInput, TData>> directInputTransitions =
            _inputToStateBuilder.ToFrozenDictionary(kvp => kvp.Key, kvp => kvp.Value.Build());

        State<TInput, TData>? defaultTransitionState = _defaultStateBuilder?.Build();
        
        var transitions =
            new Transitions<TInput, TData>(directInputTransitions, tryConditionalTransitions,
                defaultTransitionState);

        _state.Transitions = transitions;

        return _state;
    }

    private TryConditionalTransitionsDelegate<TInput, TData> BuildTryConditionalTransitionsDelegate()
    {
        ParameterExpression inputParameterExpression = Expression.Parameter(typeof(TInput), "input");
        ParameterExpression stateParameterExpression = Expression.Parameter(typeof(State<TInput, TData>).MakeByRefType(), "newState");
        LabelTarget returnTarget = Expression.Label(typeof(bool));

        IEnumerable<Expression> conditionIfStatements = _conditionToStateBuilders.Select(csb =>
        {
            State<TInput, TData> transitionState = csb.TokenizerStateBuilder.Build();
            return BuildConditionalTransitionIfStatement(csb.ConditionExpression, transitionState, inputParameterExpression, stateParameterExpression, returnTarget);
        });

        IEnumerable<Expression> bodyStatements = conditionIfStatements
            .Append(Expression.Assign(stateParameterExpression, Expression.Constant(null, typeof(State<TInput, TData>))))
            .Append(Expression.Label(returnTarget, Expression.Constant(false)));
        
        BlockExpression body = Expression.Block(typeof(bool), bodyStatements);

        return Expression.Lambda<TryConditionalTransitionsDelegate<TInput, TData>>(body, inputParameterExpression,
            stateParameterExpression).Compile();
    }

    private static IStateBuilder<TInput, TData> CreateStateBuilder(TData data, bool terminal)
    {
        var stateBuilder = new StateBuilder<TInput, TData>(data)
        {
            Terminal = terminal
        };
        return stateBuilder;
    }

    private static Expression BuildConditionalTransitionIfStatement(Expression<Func<TInput, bool>> checkCondition,
        State<TInput, TData> transitionState,
        ParameterExpression inputParameter,
        ParameterExpression outParameter, 
        LabelTarget returnTarget)
    {
        InvocationExpression checkConditionInvocationExpression = Expression.Invoke(checkCondition, inputParameter);
        
        Expression[] thenBodyStatements =
        [
            Expression.Assign(outParameter, Expression.Constant(transitionState)),
            Expression.Return(returnTarget, Expression.Constant(true))
        ];
        
        BlockExpression thenBody = Expression.Block(thenBodyStatements);
        
        return Expression.IfThen(checkConditionInvocationExpression, thenBody);
    }

    private void CheckIfAlreadyBuilt()
    {
        if (_state is not null)
            throw new InvalidOperationException("Builder has already been built");
    }

    private void CheckIfTerminal()
    {
        if (Terminal)
            throw new InvalidOperationException("Cannot define transitions on a terminal state");
    }

    private readonly record struct ConditionToStateBuilder(
        Expression<Func<TInput, bool>> ConditionExpression,
        IStateBuilder<TInput, TData> TokenizerStateBuilder);
}