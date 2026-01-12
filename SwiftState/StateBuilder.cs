using System.Collections.Frozen;
using System.Linq.Expressions;

namespace SwiftState;

public class StateBuilder<TInput, TId>(TId stateId, bool terminal = false) : IStateBuilder<TInput, TId>
    where TInput : notnull
{
    private bool _terminal = terminal;
    
    private readonly List<ConditionToStateBuilder> _conditionToStateBuilders = [];

    private IStateBuilder<TInput, TId>? _defaultStateBuilder;

    private State<TInput, TId>? _state;

    public TId Id { get; } = stateId;

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

    public bool HasTransitions => _conditionToStateBuilders.Count > 0 || _defaultStateBuilder is not null;

    public IStateBuilder<TInput, TId> When(TInput input, TId id, bool terminal = false)
    {
        IStateBuilder<TInput, TId> stateBuilder = CreateStateBuilder(id, terminal);
        When(input, stateBuilder);
        return stateBuilder;
    }

    public void When(TInput input, IStateBuilder<TInput, TId> stateBuilder)
    {
        CheckIfAlreadyBuilt();
        CheckIfTerminal();
        When(i => Equals(i, input), stateBuilder);
    }

    public IStateBuilder<TInput, TId> When(Expression<Func<TInput, bool>> condition, TId id, bool terminal = false)
    {
        IStateBuilder<TInput, TId> tokenTypeStateBuilder = CreateStateBuilder(id, terminal);
        When(condition, tokenTypeStateBuilder);
        return tokenTypeStateBuilder;
    }

    public void When(Expression<Func<TInput, bool>> condition, IStateBuilder<TInput, TId> stateBuilder)
    {
        CheckIfAlreadyBuilt();
        CheckIfTerminal();
        _conditionToStateBuilders.Add(new ConditionToStateBuilder(condition, stateBuilder));
    }

    public IStateBuilder<TInput, TId> Default(TId id, bool terminal = false)
    {
        IStateBuilder<TInput, TId> defaultTokenStateBuilder = CreateStateBuilder(id, terminal);
        Default(defaultTokenStateBuilder);
        return defaultTokenStateBuilder;
    }

    public void Default(IStateBuilder<TInput, TId> defaultStateBuilder)
    {
        CheckIfAlreadyBuilt();
        CheckIfTerminal();
        _defaultStateBuilder = defaultStateBuilder;
    }

    public void ClearTransitions()
    {
        CheckIfAlreadyBuilt();
        
        _defaultStateBuilder = null;
        _conditionToStateBuilders.Clear();
    }

    public State<TInput, TId> Build()
    {
        if (_state is { } state)
            return state;
        
        _state = new State<TInput, TId>(Id, Terminal);

        if (Terminal)
            return _state;

        TryConditionalTransitionsDelegate<TInput, TId>? tryConditionalTransitions =
            _conditionToStateBuilders.Count > 0 ? BuildTryConditionalTransitionsDelegate() : null;

        State<TInput, TId>? defaultTransitionState = _defaultStateBuilder?.Build();
        
        var transitions = new Transitions<TInput, TId>(tryConditionalTransitions, defaultTransitionState);

        _state.Transitions = transitions;

        return _state;
    }

    private TryConditionalTransitionsDelegate<TInput, TId> BuildTryConditionalTransitionsDelegate()
    {
        ParameterExpression inputParameterExpression = Expression.Parameter(typeof(TInput), "input");
        ParameterExpression stateParameterExpression = Expression.Parameter(typeof(State<TInput, TId>).MakeByRefType(), "newState");
        LabelTarget returnTarget = Expression.Label(typeof(bool));

        IEnumerable<Expression> conditionIfStatements = _conditionToStateBuilders.Select(csb =>
        {
            State<TInput, TId> transitionState = csb.TokenizerStateBuilder.Build();
            return BuildConditionalTransitionIfStatement(csb.ConditionExpression, transitionState, inputParameterExpression, stateParameterExpression, returnTarget);
        });

        IEnumerable<Expression> bodyStatements = conditionIfStatements
            .Append(Expression.Assign(stateParameterExpression, Expression.Constant(null, typeof(State<TInput, TId>))))
            .Append(Expression.Label(returnTarget, Expression.Constant(false)));
        
        BlockExpression body = Expression.Block(typeof(bool), bodyStatements);

        return Expression.Lambda<TryConditionalTransitionsDelegate<TInput, TId>>(body, inputParameterExpression,
            stateParameterExpression).Compile();
    }

    private static IStateBuilder<TInput, TId> CreateStateBuilder(TId data, bool terminal)
    {
        var stateBuilder = new StateBuilder<TInput, TId>(data)
        {
            Terminal = terminal
        };
        return stateBuilder;
    }

    private static Expression BuildConditionalTransitionIfStatement(Expression<Func<TInput, bool>> checkCondition,
        State<TInput, TId> transitionState,
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
        IStateBuilder<TInput, TId> TokenizerStateBuilder);
}