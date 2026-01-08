using SwiftState;

namespace SwiftState.Tests;

public class StateTests
{
    [Test]
    public async Task State_WithDirectTransition_ShouldTransitionCorrectly()
    {
        // Arrange
        var builder = new StateBuilder<char, string>("Initial");
        builder.When('a', "StateA");
        State<char, string> initialState = builder.Build();

        // Act
        bool result = initialState.TryTransition('a', out State<char, string>? nextState);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(nextState).IsNotNull();
        await Assert.That(nextState!.Id).IsEqualTo("StateA");
    }

    [Test]
    public async Task State_WithNoMatchingTransition_ShouldReturnFalse()
    {
        // Arrange
        var builder = new StateBuilder<char, string>("Initial");
        builder.When('a', "StateA");
        State<char, string> initialState = builder.Build();

        // Act
        bool result = initialState.TryTransition('b', out State<char, string>? nextState);

        // Assert
        await Assert.That(result).IsFalse();
        await Assert.That(nextState).IsNull();
    }

    [Test]
    public async Task State_WithConditionalTransition_ShouldTransitionCorrectly()
    {
        // Arrange
        var builder = new StateBuilder<int, string>("Initial");
        builder.When(i => i > 10, "GreaterThanTen");
        State<int, string> initialState = builder.Build();

        // Act
        bool result = initialState.TryTransition(15, out State<int, string>? nextState);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(nextState).IsNotNull();
        await Assert.That(nextState!.Id).IsEqualTo("GreaterThanTen");
    }

    [Test]
    public async Task State_WithDefaultTransition_ShouldTransitionToDefault()
    {
        // Arrange
        var builder = new StateBuilder<char, string>("Initial");
        builder.Default("DefaultState");
        State<char, string> initialState = builder.Build();

        // Act
        bool result = initialState.TryTransition('z', out State<char, string>? nextState);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(nextState).IsNotNull();
        await Assert.That(nextState!.Id).IsEqualTo("DefaultState");
    }

    [Test]
    public async Task State_WithDirectAndConditionalTransitions_ShouldPrioritizeDirect()
    {
        // Arrange
        var builder = new StateBuilder<int, string>("Initial");
        builder.When(5, "DirectFive");
        builder.When(i => i == 5, "ConditionalFive");
        State<int, string> initialState = builder.Build();

        // Act
        bool result = initialState.TryTransition(5, out State<int, string>? nextState);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(nextState).IsNotNull();
        await Assert.That(nextState!.Id).IsEqualTo("DirectFive");
    }

    [Test]
    public async Task State_WithConditionalAndDefaultTransitions_ShouldPrioritizeConditional()
    {
        // Arrange
        var builder = new StateBuilder<int, string>("Initial");
        builder.When(i => i > 10, "GreaterThanTen");
        builder.Default("DefaultState");
        State<int, string> initialState = builder.Build();

        // Act
        bool result = initialState.TryTransition(15, out State<int, string>? nextState);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(nextState).IsNotNull();
        await Assert.That(nextState!.Id).IsEqualTo("GreaterThanTen");
    }
    
    [Test]
    public async Task State_ChainedTransitions_ShouldWorkCorrectly()
    {
        // Arrange
        var builder = new StateBuilder<char, string>("Start");
        IStateBuilder<char, string> stateA = builder.When('a', "StateA");
        stateA.When('b', "StateB");
        
        State<char, string> startState = builder.Build();

        // Act & Assert
        // Transition 1: Start -> StateA
        bool result1 = startState.TryTransition('a', out State<char, string>? nextState1);
        await Assert.That(result1).IsTrue();
        await Assert.That(nextState1!.Id).IsEqualTo("StateA");

        // Transition 2: StateA -> StateB
        bool result2 = nextState1.TryTransition('b', out State<char, string>? nextState2);
        await Assert.That(result2).IsTrue();
        await Assert.That(nextState2!.Id).IsEqualTo("StateB");
    }

    [Test]
    public async Task State_TryGetDefault_ShouldReturnDefaultState()
    {
        // Arrange
        var builder = new StateBuilder<char, string>("Initial");
        builder.Default("DefaultState");
        State<char, string> initialState = builder.Build();

        // Act
        bool result = initialState.TryGetDefault(out State<char, string>? defaultState);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(defaultState).IsNotNull();
        await Assert.That(defaultState!.Id).IsEqualTo("DefaultState");
    }

    [Test]
    public async Task State_TryGetDefault_ShouldReturnFalseWhenNoDefault()
    {
        // Arrange
        var builder = new StateBuilder<char, string>("Initial");
        State<char, string> initialState = builder.Build();

        // Act
        bool result = initialState.TryGetDefault(out State<char, string>? defaultState);

        // Assert
        await Assert.That(result).IsFalse();
        await Assert.That(defaultState).IsNull();
    }
    
    [Test]
    public async Task StateBuilder_WhenCalledAfterBuild_ShouldThrowException()
    {
        // Arrange
        var builder = new StateBuilder<char, string>("Initial");
        builder.Build();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => 
        {
            builder.When('a', "StateA");
            await Task.CompletedTask;
        });
    }

    [Test]
    public async Task State_WithDirectTransitionAndDefault_ShouldFallbackToDefaultWhenTransitionNotSetup()
    {
        // Arrange
        var builder = new StateBuilder<char, string>("Initial");
        builder.When('a', "StateA");
        builder.Default("DefaultState");
        State<char, string> initialState = builder.Build();

        // Act
        bool result = initialState.TryTransition('b', out State<char, string>? nextState);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(nextState).IsNotNull();
        await Assert.That(nextState!.Id).IsEqualTo("DefaultState");
    }

    [Test]
    public async Task State_WithCircularDependency_ShouldBuildAndTransitionCorrectly()
    {
        // Arrange
        var builderA = new StateBuilder<char, string>("StateA");
        var builderB = new StateBuilder<char, string>("StateB");

        // A -> 'b' -> B
        builderA.When('b', builderB);
        // B -> 'a' -> A
        builderB.When('a', builderA);

        State<char, string> stateA = builderA.Build();

        // Act
        bool toB = stateA.TryTransition('b', out State<char, string>? stateB);
        bool backToA = stateB!.TryTransition('a', out State<char, string>? stateAAgain);

        // Assert
        await Assert.That(toB).IsTrue();
        await Assert.That(stateB.Id).IsEqualTo("StateB");
        await Assert.That(backToA).IsTrue();
        await Assert.That(stateAAgain!.Id).IsEqualTo("StateA");
        await Assert.That(stateAAgain).IsEqualTo(stateA);
    }

    [Test]
    public async Task State_HasInputTransitions_ShouldReturnTrue_WhenDirectTransitionsExist()
    {
        var builder = new StateBuilder<char, string>("Init");
        builder.When('a', "A");
        State<char, string> state = builder.Build();
        
        await Assert.That(state.HasInputTransitions).IsTrue();
    }

    [Test]
    public async Task State_HasInputTransitions_ShouldReturnTrue_WhenConditionalTransitionsExist()
    {
        var builder = new StateBuilder<int, string>("Init");
        builder.When(i => i > 0, "A");
        State<int, string> state = builder.Build();
        
        await Assert.That(state.HasInputTransitions).IsTrue();
    }

    [Test]
    public async Task State_HasInputTransitions_ShouldReturnFalse_WhenNoTransitionsExist()
    {
        var builder = new StateBuilder<char, string>("Init");
        State<char, string> state = builder.Build();
        
        await Assert.That(state.HasInputTransitions).IsFalse();
    }

    [Test]
    public async Task State_HasDefaultTransition_ShouldReturnTrue_WhenDefaultExists()
    {
        var builder = new StateBuilder<char, string>("Init");
        builder.Default("Default");
        State<char, string> state = builder.Build();
        
        await Assert.That(state.HasDefaultTransition).IsTrue();
    }

    [Test]
    public async Task State_HasDefaultTransition_ShouldReturnFalse_WhenNoDefaultExists()
    {
        var builder = new StateBuilder<char, string>("Init");
        State<char, string> state = builder.Build();
        
        await Assert.That(state.HasDefaultTransition).IsFalse();
    }

    [Test]
    public async Task State_HasInputAndDefaultTransitions_ShouldReturnTrueForBoth()
    {
        var builder = new StateBuilder<char, string>("Init");
        builder.When('a', "A");
        builder.Default("Default");
        State<char, string> state = builder.Build();
        
        await Assert.That(state.HasInputTransitions).IsTrue();
        await Assert.That(state.HasDefaultTransition).IsTrue();
    }
}