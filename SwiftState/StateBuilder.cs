using System.Collections.Frozen;
using System.Linq.Expressions;
using PredicateMap;

namespace SwiftState;

public class StateBuilder<TInput, TId>(TId stateId, bool terminal = false) : IStateBuilder<TInput, TId>
    where TInput : notnull where TId : notnull
{
    private bool _terminal = terminal;
    
    private readonly List<ConditionToStateBuilder> _conditionToStateBuilders = [];

    private IStateBuilder<TInput, TId>? _defaultStateBuilder;

    private State<TInput, TId>? _state;
    private StateBuilderContext? _context;

    private StateBuilder(TId stateId, bool terminal, StateBuilderContext context) : this(stateId, terminal)
    {
        _context = context;
    }
    
    private StateBuilderContext Context => _context ??= new StateBuilderContext(this);

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
        IStateBuilder<TInput, TId> stateBuilder = Context.GetStateBuilder(id, terminal);
        When(input, stateBuilder);
        return stateBuilder;
    }

    private void When(TInput input, IStateBuilder<TInput, TId> stateBuilder)
    {
        When(i => Equals(i, input), stateBuilder);
    }

    public IStateBuilder<TInput, TId> When(Expression<Predicate<TInput>> condition, TId id, bool terminal = false)
    {
        IStateBuilder<TInput, TId> tokenTypeStateBuilder = Context.GetStateBuilder(id, terminal);
        When(condition, tokenTypeStateBuilder);
        return tokenTypeStateBuilder;
    }

    private void When(Expression<Predicate<TInput>> condition, IStateBuilder<TInput, TId> stateBuilder)
    {
        CheckIfAlreadyBuilt();
        CheckIfTerminal();
        _conditionToStateBuilders.Add(new ConditionToStateBuilder(condition, stateBuilder));
    }

    public IStateBuilder<TInput, TId> GotoWhen(TId id, bool terminal, params TInput[] inputs)
    {
        IStateBuilder<TInput, TId> stateBuilder = Context.GetStateBuilder(id, terminal);
        foreach (TInput input in inputs)
            When(input, stateBuilder);
        
        return stateBuilder;
    }

    public IStateBuilder<TInput, TId> GotoWhen(TId id, bool terminal, params Expression<Predicate<TInput>>[] conditions)
    {
        IStateBuilder<TInput, TId> stateBuilder = Context.GetStateBuilder(id, terminal);
        foreach (Expression<Predicate<TInput>> condition in conditions)
            When(condition, stateBuilder);
        
        return stateBuilder;
    }

    public IStateBuilder<TInput, TId> Default(TId id, bool terminal = false)
    {
        IStateBuilder<TInput, TId> defaultTokenStateBuilder = Context.GetStateBuilder(id, terminal);
        Default(defaultTokenStateBuilder);
        return defaultTokenStateBuilder;
    }

    public IStateBuilder<TInput, TId> GetBuilderForState(TId id, bool terminal = false)
    {
        return Context.GetStateBuilder(id, terminal);
    }

    private void Default(IStateBuilder<TInput, TId> defaultStateBuilder)
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

        IPredicateMap<TInput, State<TInput, TId>>? tryConditionalTransitions =
            _conditionToStateBuilders.Count > 0 ? BuildTryConditionalTransitionsPredicateMap() : null;

        State<TInput, TId>? defaultTransitionState = _defaultStateBuilder?.Build();
        
        var transitions = new Transitions<TInput, TId>(tryConditionalTransitions, defaultTransitionState);

        _state.Transitions = transitions;

        return _state;
    }

    private IPredicateMap<TInput, State<TInput, TId>> BuildTryConditionalTransitionsPredicateMap()
    {
        return _conditionToStateBuilders.ToPredicateMap(csb => csb.ConditionExpression,
            csb => csb.TokenizerStateBuilder.Build());
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
        Expression<Predicate<TInput>> ConditionExpression,
        IStateBuilder<TInput, TId> TokenizerStateBuilder);

    internal class StateBuilderContext(IStateBuilder<TInput, TId> sourceBuilder)
    {
        private readonly Dictionary<TId, IStateBuilder<TInput, TId>> _stateBuilders = new()
        {
            { sourceBuilder.Id, sourceBuilder }
        };

        public IStateBuilder<TInput, TId> GetStateBuilder(TId stateId, bool terminal)
        {
            IStateBuilder<TInput, TId> stateBuilder = _stateBuilders.GetOrAdd(stateId, (newStateId) => new StateBuilder<TInput, TId>(newStateId, terminal, this));
            
            if (stateBuilder.Terminal != terminal)
                throw new InvalidOperationException("Existing state builder has different terminal state than requested");
            
            return stateBuilder;
        }
    }
}