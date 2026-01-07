# SwiftState

SwiftState is a lightweight, fluent state machine library for .NET. It allows you to define states and transitions using a clean and expressive API.

## Features

- **Fluent API**: Define states and transitions using a readable, chainable syntax.
- **Direct Transitions**: Map inputs directly to target states.
- **Conditional Transitions**: Use predicates to determine transitions based on input values.
- **Default Transitions**: Specify a fallback state when no other transition matches.
- **Generic Support**: Works with any type for inputs and state IDs.
- **High Performance**: Uses compiled expression trees for conditional transitions and frozen dictionaries for direct lookups.

## Installation

NuGet package coming soon

## Usage

### Basic Example

Here's a simple example of a state machine that transitions based on character inputs:

```csharp
using SwiftState;

// Create a builder for a state machine with char inputs and string state IDs
var builder = new StateBuilder<char, string>("Initial");

// Define transitions
builder.When('a', "StateA");
builder.When('b', "StateB");

// Build the initial state
State<char, string> initialState = builder.Build();

// Transition
if (initialState.TryTransition('a', out State<char, string> nextState))
{
    Console.WriteLine($"Transitioned to: {nextState.Id}"); // Output: Transitioned to: StateA
}
```

### Conditional Transitions

You can define transitions based on conditions:

```csharp
var builder = new StateBuilder<int, string>("Start");

// Transition if input is greater than 10
builder.When(i => i > 10, "HighValue");

// Transition if input is even
builder.When(i => i % 2 == 0, "EvenValue");

State<int, string> state = builder.Build();

if (state.TryTransition(15, out State<int, string> nextState))
{
    Console.WriteLine(nextState.Id); // Output: HighValue
}
```

### Default Transitions

You can specify a default state to transition to if no other conditions are met:

```csharp
var builder = new StateBuilder<string, string>("Idle");

builder.When("run", "Running");
builder.Default("Error");

State<string, string> state = builder.Build();

// "jump" is not defined, so it transitions to Default
state.TryTransition("jump", out State<string, string> nextState); 
Console.WriteLine(nextState.Id); // Output: Error
```

### Chained Transitions

You can chain transitions to define complex state flows:

```csharp
var builder = new StateBuilder<char, string>("Start");

// Define a sequence: Start -> 'a' -> StateA -> 'b' -> StateB
builder.When('a', "StateA")
       .When('b', "StateB");

State<char, string> startState = builder.Build();

startState.TryTransition('a', out State<char, string> stateA);
stateA.TryTransition('b', out State<char, string> stateB);

Console.WriteLine(stateB.Id); // Output: StateB
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
