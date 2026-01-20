using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using SwiftState;
using SwiftState.Benchmark;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<StateBenchmark>();
    }
}

[MemoryDiagnoser]
public class StateBenchmark
{
    private State<char, StateId> _state;
    private State<int, StateId> _conditionalState;
    
    private State<Input, StateId> _recordInputState;
    private Input _inputA;
    private Input _inputB;

    [GlobalSetup]
    public void Setup()
    {
        var builder = new StateBuilder<char, StateId>(new StateId("Initial"));
        builder.When('a', new StateId("StateA"));
        builder.When('b', new StateId("StateB"));
        builder.Default(new StateId("Default"));
        _state = builder.Build();

        var conditionalBuilder = new StateBuilder<int, StateId>(new StateId("Initial"));
        conditionalBuilder.When(i => i > 10, new StateId("GreaterThanTen"));
        conditionalBuilder.When(i => i < 5, new StateId("LessThanFive"));
        conditionalBuilder.Default(new StateId("Default"));
        _conditionalState = conditionalBuilder.Build();

        _inputA = new Input("A");
        _inputB = new Input("B");

        var recordInputStateBuilder = new StateBuilder<Input, StateId>(new StateId("Initial"));
        recordInputStateBuilder.When(_inputA, new StateId("StateA"));
        recordInputStateBuilder.When(_inputB, new StateId("StateB"));
        recordInputStateBuilder.Default(new StateId("Default"));
        _recordInputState = recordInputStateBuilder.Build();
    }

    [Benchmark]
    public void DirectTransition()
    {
        _ = _state.TryTransition('a', out State<char, StateId>? _);
    }

    [Benchmark]
    public void DefaultTransition()
    {
        _ = _state.TryTransition('z', out State<char, StateId>? _);
    }

    [Benchmark]
    public void ConditionalTransition_Match()
    {
        _ = _conditionalState.TryTransition(15, out State<int, StateId>? _);
    }

    [Benchmark]
    public void ConditionalTransition_NoMatch_Default()
    {
        _ = _conditionalState.TryTransition(7, out State<int, StateId>? _);
    }

    [Benchmark]
    public void RecordInputType_DirectTransition()
    {
        _ = _recordInputState.TryTransition(_inputA, out State<Input, StateId>? _);
    }
}