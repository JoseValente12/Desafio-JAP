namespace JapCarRental.Web.Services;

/// <summary>
/// Result of a service operation. Rule violations are reported by field
/// instead of exceptions, so controllers can show them next to the right input.
/// </summary>
public class OperationResult
{
    // Field name -> list of messages. An empty key means a general error.
    private readonly Dictionary<string, List<string>> _errors = new();

    public bool Succeeded => _errors.Count == 0;

    public IReadOnlyDictionary<string, List<string>> Errors => _errors;

    public static OperationResult Success() => new();

    public static OperationResult Failure(string field, string message)
    {
        var result = new OperationResult();
        result.AddError(field, message);
        return result;
    }

    // General error, not tied to any form field (for example "vehicle not found").
    public static OperationResult Failure(string message) => Failure(string.Empty, message);

    public void AddError(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var messages))
        {
            messages = new List<string>();
            _errors[field] = messages;
        }

        messages.Add(message);
    }
}

/// <summary>
/// Same as <see cref="OperationResult"/>, but carries a value when the operation succeeds
/// (for example the Id of a newly created record).
/// </summary>
public class OperationResult<T> : OperationResult
{
    public T? Value { get; private set; }

    public static OperationResult<T> Success(T value) => new() { Value = value };

    // 'new' hides the base methods so failures are typed as OperationResult<T>.
    public static new OperationResult<T> Failure(string field, string message)
    {
        var result = new OperationResult<T>();
        result.AddError(field, message);
        return result;
    }

    public static new OperationResult<T> Failure(string message) => Failure(string.Empty, message);
}