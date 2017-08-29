# SwitchCoverageAnalyzer
An analyzer to improve safety and convenience of switch statements in C#.

## Features
- Reports a *warning* for each switch statement on enum value that doesn't contain `case` labels for all enum members.
- Provides codefixes to insert missing `case` labels to such switch statements, *preserving order*.
    - NOTE: Although Visual Studio 2017 provides the codefix to insert missing `case` labels by default, it doesn't care order of labels.

## Example
In the following sample, a warning is reported on the switch statement because of missing the case for ``Priority.Middle``.

```csharp
public enum Priority
{
    Low,
    Middle,
    High,
}

// WARNING: Missing the case for Priority.Middle.
switch (priority)
{
    case Priority.Low:
        // ...
    case Priority.High:
        // ...
}
```

Therefore the codefix generates the case in the middle:

```csharp
switch (priority)
{
    case Priority.Low:
        // ...

    // *GENERATED*
    case Priority.Middle:
        throw new NotImplementedException();

    case Priority.High:
        // ...
}
```

Note that if the labels aren't sorted by value then generated labels are added before default label (if exists) or on bottom of switch statement.
