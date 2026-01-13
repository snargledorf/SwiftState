using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using SwiftState;

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
    private State<char, string> _state;
    private State<int, string> _conditionalState;

    [GlobalSetup]
    public void Setup()
    {
        var builder = new StateBuilder<char, string>("Initial");
        builder.When('a', "StateA");
        builder.When('b', "StateB");
        builder.Default("Default");
        _state = builder.Build();

        var conditionalBuilder = new StateBuilder<int, string>("Initial");
        conditionalBuilder.When(i => i > 10, "GreaterThanTen");
        conditionalBuilder.When(i => i < 5, "LessThanFive");
        conditionalBuilder.Default("Default");
        _conditionalState = conditionalBuilder.Build();
    }

    [Benchmark]
    public void DirectTransition()
    {
        _state.TryTransition('a', out State<char, string>? _);
    }

    [Benchmark]
    public void DefaultTransition()
    {
        _state.TryTransition('z', out State<char, string>? _);
    }

    [Benchmark]
    public void ConditionalTransition_Match()
    {
        _conditionalState.TryTransition(15, out State<int, string>? _);
    }

    [Benchmark]
    public void ConditionalTransition_NoMatch_Default()
    {
        _conditionalState.TryTransition(7, out State<int, string>? _);
    }
}