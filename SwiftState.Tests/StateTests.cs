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

        // A -> 'b' -> B
        IStateBuilder<char, string> builderB = builderA.When('b', "StateB");
        // B -> 'a' -> A
        builderB.When('a', "StateA");

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

    [Test]
    public async Task State_TerminalState_ShouldNotAllowTransitions()
    {
        // Arrange
        var builder = new StateBuilder<char, string>("Terminal")
        {
            Terminal = true
        };
        State<char, string> state = builder.Build();

        // Act
        bool result = state.TryTransition('a', out State<char, string>? nextState);

        // Assert
        await Assert.That(state.IsTerminal).IsTrue();
        await Assert.That(result).IsFalse();
        await Assert.That(nextState).IsNull();
    }

    [Test]
    public async Task StateBuilder_TerminalState_ShouldThrowWhenAddingTransitions()
    {
        // Arrange
        var builder = new StateBuilder<char, string>("Terminal")
        {
            Terminal = true
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            builder.When('a', "StateA");
            await Task.CompletedTask;
        });
        
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            builder.Default("Default");
            await Task.CompletedTask;
        });
    }

    [Test]
    public async Task StateBuilder_SettingTerminalToTrue_ShouldThrowIfTransitionsExist()
    {
        // Arrange
        var builder = new StateBuilder<char, string>("Initial");
        builder.When('a', "StateA");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            builder.Terminal = true;
            await Task.CompletedTask;
        });
    }

    [Test]
    public async Task State_TransitionToTerminalState_ShouldWork()
    {
        // Arrange
        var builder = new StateBuilder<char, string>("Start");
        builder.When('a', "End", terminal: true);
        State<char, string> startState = builder.Build();

        // Act
        bool result = startState.TryTransition('a', out State<char, string>? endState);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(endState!.Id).IsEqualTo("End");
        await Assert.That(endState.IsTerminal).IsTrue();
    }

    [Test]
    public async Task StateBuilder_ClearTransitions_ShouldRemoveAllTransitions()
    {
        // Arrange
        var builder = new StateBuilder<char, string>("Initial");
        builder.When('a', "StateA");
        builder.When(c => c == 'b', "StateB");
        builder.Default("Default");

        // Act
        builder.ClearTransitions();
        State<char, string> state = builder.Build();

        // Assert
        await Assert.That(state.HasTransitions).IsFalse();
        await Assert.That(state.HasInputTransitions).IsFalse();
        await Assert.That(state.HasDefaultTransition).IsFalse();
    }

    [Test]
    public async Task StateBuilder_ClearTransitions_ShouldAllowAddingNewTransitions()
    {
        // Arrange
        var builder = new StateBuilder<char, string>("Initial");
        builder.When('a', "StateA");
        builder.ClearTransitions();

        // Act
        builder.When('b', "StateB");
        State<char, string> state = builder.Build();
        bool result = state.TryTransition('b', out State<char, string>? nextState);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(nextState!.Id).IsEqualTo("StateB");
    }

    [Test]
    public async Task StateBuilder_ClearTransitions_ShouldThrowIfAlreadyBuilt()
    {
        // Arrange
        var builder = new StateBuilder<char, string>("Initial");
        builder.Build();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            builder.ClearTransitions();
            await Task.CompletedTask;
        });
    }

    [Test]
    public async Task StateBuilder_ConstructorWithTerminalTrue_ShouldSetTerminalProperty()
    {
        // Arrange
        var builder = new StateBuilder<char, string>("Terminal", terminal: true);

        // Assert
        await Assert.That(builder.Terminal).IsTrue();
    }

    [Test]
    public async Task StateBuilder_ConstructorWithTerminalTrue_ShouldCreateTerminalState()
    {
        // Arrange
        var builder = new StateBuilder<char, string>("Terminal", terminal: true);
        State<char, string> state = builder.Build();

        // Assert
        await Assert.That(state.IsTerminal).IsTrue();
    }

    [Test]
    public async Task StateBuilder_ConstructorWithTerminalTrue_ShouldThrowWhenAddingTransitions()
    {
        // Arrange
        var builder = new StateBuilder<char, string>("Terminal", terminal: true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            builder.When('a', "StateA");
            await Task.CompletedTask;
        });
    }

    [Test]
    public async Task State_GotoWhen_ShouldTransitionCorrectlyForAllInputs()
    {
        // Arrange
        var builder = new StateBuilder<char, string>("Initial");
        builder.GotoWhen("Target", false, 'a', 'b', 'c');
        State<char, string> initialState = builder.Build();

        // Act & Assert
        foreach (char input in new[] { 'a', 'b', 'c' })
        {
            bool result = initialState.TryTransition(input, out State<char, string>? nextState);
            await Assert.That(result).IsTrue();
            await Assert.That(nextState).IsNotNull();
            await Assert.That(nextState!.Id).IsEqualTo("Target");
        }
        
        // Verify other input doesn't transition
        bool resultD = initialState.TryTransition('d', out State<char, string>? nextStateD);
        await Assert.That(resultD).IsFalse();
    }

    [Test]
    public async Task State_GotoWhen_ShouldReturnTargetStateBuilder()
    {
        // Arrange
        var builder = new StateBuilder<char, string>("Initial");
        IStateBuilder<char, string> targetBuilder = builder.GotoWhen("Target", false, 'a');
        
        // Act
        targetBuilder.When('b', "NextTarget");
        State<char, string> initialState = builder.Build();
        
        // Assert
        initialState.TryTransition('a', out State<char, string>? targetState);
        bool result = targetState!.TryTransition('b', out State<char, string>? nextTargetState);
        
        await Assert.That(result).IsTrue();
        await Assert.That(nextTargetState!.Id).IsEqualTo("NextTarget");
    }

    [Test]
    public async Task State_GotoWhen_WithTerminalState_ShouldTransitionToTerminalState()
    {
        // Arrange
        var builder = new StateBuilder<char, string>("Initial");
        builder.GotoWhen("TerminalTarget", true, 'a', 'b');
        State<char, string> initialState = builder.Build();

        // Act
        bool result = initialState.TryTransition('a', out State<char, string>? nextState);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(nextState!.Id).IsEqualTo("TerminalTarget");
        await Assert.That(nextState.IsTerminal).IsTrue();
    }

    [Test]
    public async Task StateBuilder_When_WithDifferentTerminalFlag_ShouldThrowException()
    {
        // Arrange
        var builder = new StateBuilder<char, string>("Initial");
        builder.When('a', "Target", terminal: false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            builder.When('b', "Target", terminal: true);
            await Task.CompletedTask;
        });
    }

    [Test]
    public async Task StateBuilder_GotoWhen_WithDifferentTerminalFlag_ShouldThrowException()
    {
        // Arrange
        var builder = new StateBuilder<char, string>("Initial");
        builder.GotoWhen("Target", false, 'a');

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            builder.GotoWhen("Target", true, 'b');
            await Task.CompletedTask;
        });
    }

    [Test]
    public async Task StateBuilder_Default_WithDifferentTerminalFlag_ShouldThrowException()
    {
        // Arrange
        var builder = new StateBuilder<char, string>("Initial");
        builder.When('a', "Target", terminal: false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            builder.Default("Target", terminal: true);
            await Task.CompletedTask;
        });
    }

    [Test]
    public async Task State_GotoWhen_WithConditions_ShouldTransitionCorrectly()
    {
        // Arrange
        var builder = new StateBuilder<int, string>("Initial");
        builder.GotoWhen("Target", false, i => i > 10, i => i < 5);
        State<int, string> initialState = builder.Build();

        // Act & Assert
        // Case 1: i > 10
        bool result1 = initialState.TryTransition(15, out State<int, string>? nextState1);
        await Assert.That(result1).IsTrue();
        await Assert.That(nextState1!.Id).IsEqualTo("Target");

        // Case 2: i < 5
        bool result2 = initialState.TryTransition(2, out State<int, string>? nextState2);
        await Assert.That(result2).IsTrue();
        await Assert.That(nextState2!.Id).IsEqualTo("Target");

        // Case 3: No match
        bool result3 = initialState.TryTransition(7, out State<int, string>? nextState3);
        await Assert.That(result3).IsFalse();
    }

    [Test]
    public async Task State_GotoWhen_WithConditions_ShouldReturnTargetStateBuilder()
    {
        // Arrange
        var builder = new StateBuilder<int, string>("Initial");
        IStateBuilder<int, string> targetBuilder = builder.GotoWhen("Target", false, i => i > 10);
        
        // Act
        targetBuilder.When(i => i == 0, "NextTarget");
        State<int, string> initialState = builder.Build();
        
        // Assert
        initialState.TryTransition(15, out State<int, string>? targetState);
        bool result = targetState!.TryTransition(0, out State<int, string>? nextTargetState);
        
        await Assert.That(result).IsTrue();
        await Assert.That(nextTargetState!.Id).IsEqualTo("NextTarget");
    }

    [Test]
    public async Task State_GotoWhen_WithConditions_AndTerminalState_ShouldTransitionToTerminalState()
    {
        // Arrange
        var builder = new StateBuilder<int, string>("Initial");
        builder.GotoWhen("TerminalTarget", true, i => i > 10, i => i < 5);
        State<int, string> initialState = builder.Build();

        // Act
        bool result = initialState.TryTransition(15, out State<int, string>? nextState);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(nextState!.Id).IsEqualTo("TerminalTarget");
        await Assert.That(nextState.IsTerminal).IsTrue();
    }

    [Test]
    public async Task StateBuilder_GotoWhen_WithConditions_AndDifferentTerminalFlag_ShouldThrowException()
    {
        // Arrange
        var builder = new StateBuilder<int, string>("Initial");
        builder.GotoWhen("Target", false, i => i > 10);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            builder.GotoWhen("Target", true, i => i < 5);
            await Task.CompletedTask;
        });
    }
}