using System;
using System.Collections.Generic;
using System.Linq;

namespace RestApiDdd.Service.Exceptions;

public sealed class ApplicationValidationException : Exception
{
    public ApplicationValidationException(string message)
        : base(message)
    {
        Errors = new[] { message ?? string.Empty };
    }

    public ApplicationValidationException(IEnumerable<string> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = (errors ?? Enumerable.Empty<string>()).ToArray();
    }

    public IReadOnlyCollection<string> Errors { get; }

    public override string Message =>
        (Errors != null && Errors.Count > 0)
            ? string.Join(Environment.NewLine, Errors)
            : base.Message;
}
