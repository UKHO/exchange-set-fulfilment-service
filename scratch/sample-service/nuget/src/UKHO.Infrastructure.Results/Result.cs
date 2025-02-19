using System.Diagnostics.CodeAnalysis;

namespace UKHO.Infrastructure.Results
{
    /// <summary>Represents a result.</summary>
    public readonly struct Result : IEquatable<Result>, IActionableResult<Result>
    {
        /// <inheritdoc />
        public IReadOnlyCollection<IError> Errors
        {
            get
            {
                if (_errors is not null)
                {
                    return _errors;
                }

                if (_isSuccess)
                {
                    return Error.EmptyErrorList;
                }

                return Error.DefaultErrorList;
            }
        }

        private static readonly Result SuccessResult = new(true);
        internal static readonly Result FailureResult = new(Error.Empty);
        private readonly bool _isSuccess = false;
        private readonly IReadOnlyList<IError>? _errors;

        private Result(bool isSuccess) => _isSuccess = isSuccess;

        private Result(IError error) => _errors = [error];

        private Result(IEnumerable<IError> errors) => _errors = errors.ToArray();

        internal Result(IReadOnlyList<IError> errors) => _errors = errors;

        /// <summary>Creates a success result.</summary>
        /// <returns>A new instance of <see cref="Result" /> representing a success result with the specified value.</returns>
        public static Result Success() => SuccessResult;

        /// <summary>Creates a success result with the specified value.</summary>
        /// <param name="value">The value to include in the result.</param>
        /// <typeparam name="TValue">The type of the value of the result.</typeparam>
        /// <returns>A new instance of <see cref="Result{TValue}" /> representing a success result with the specified value.</returns>
        public static Result<TValue> Success<TValue>(TValue value) => new(value);

        /// <inheritdoc />
        public bool IsSuccess() => _isSuccess;

        /// <summary>Creates a failure result.</summary>
        /// <returns>A new instance of <see cref="Result" /> representing a failure result.</returns>
        public static Result Failure() => FailureResult;

        /// <summary>Creates a failure result with the given error message.</summary>
        /// <param name="errorMessage">The error message associated with the failure.</param>
        /// <returns>A new instance of <see cref="Result" /> representing a failure result with the specified error message.</returns>
        public static Result Failure(string errorMessage)
        {
            Error error = new Error(errorMessage);
            return new Result(error);
        }

        /// <summary>Creates a failure result with the given error message and metadata.</summary>
        /// <param name="metadata">The metadata associated with the failure.</param>
        /// <param name="errorMessage">The error message associated with the failure.</param>
        /// <returns>
        ///     A new instance of <see cref="Result" /> representing a failure result with the specified error message and
        ///     metadata.
        /// </returns>
        public static Result Failure(string errorMessage, (string Key, object Value) metadata)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>(1) { { metadata.Key, metadata.Value } };
            Error error = new Error(errorMessage, dictionary);
            return new Result(error);
        }

        /// <summary>Creates a failure result with the given error message and metadata.</summary>
        /// <param name="metadata">The metadata associated with the failure.</param>
        /// <param name="errorMessage">The error message associated with the failure.</param>
        /// <returns>
        ///     A new instance of <see cref="Result" /> representing a failure result with the specified error message and
        ///     metadata.
        /// </returns>
        public static Result Failure(string errorMessage, KeyValuePair<string, object> metadata)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>(1) { { metadata.Key, metadata.Value } };
            Error error = new Error(errorMessage, dictionary);
            return new Result(error);
        }

        /// <summary>Creates a failure result with the given error message and metadata.</summary>
        /// <param name="metadata">The metadata associated with the failure.</param>
        /// <param name="errorMessage">The error message associated with the failure.</param>
        /// <returns>
        ///     A new instance of <see cref="Result" /> representing a failure result with the specified error message and
        ///     metadata.
        /// </returns>
        public static Result Failure(string errorMessage, IReadOnlyDictionary<string, object> metadata)
        {
            Error error = new Error(errorMessage, metadata);
            return new Result(error);
        }

        /// <summary>Creates a failure result with the given exception.</summary>
        /// <param name="ex">The <see cref="Exception" /> associated with the failure.</param>
        /// <returns>A new instance of <see cref="Result" /> representing a failure result with the specified exception.</returns>
        /// <remarks>
        ///     The exception is added to the error <see cref="Error.Metadata" /> under the key of "Exception" and the error
        ///     <see cref="Error.Message" /> is set to that
        ///     of the exception.
        /// </remarks>
        public static Result Failure(Exception ex)
        {
            ArgumentNullException.ThrowIfNull(ex);

            Dictionary<string, object> metadata = new Dictionary<string, object>(1) { { "Exception", ex } };

            Error error = new Error(ex.Message, metadata);
            return new Result(error);
        }

        /// <summary>Creates a failure result with the given error message and exception.</summary>
        /// <param name="errorMessage">The error message associated with the failure.</param>
        /// <param name="ex">The <see cref="Exception" /> associated with the failure.</param>
        /// <returns>
        ///     A new instance of <see cref="Result" /> representing a failure result with the specified error message and
        ///     exception.
        /// </returns>
        /// <remarks>The exception is added to the error <see cref="Error.Metadata" /> under the key of "Exception".</remarks>
        public static Result Failure(string errorMessage, Exception ex)
        {
            ArgumentNullException.ThrowIfNull(ex);

            Dictionary<string, object> metadata = new Dictionary<string, object>(1) { { "Exception", ex } };
            Error error = new Error(errorMessage, metadata);
            return new Result(error);
        }

        /// <summary>Creates a failure result with the given error.</summary>
        /// <param name="error">The error associated with the failure.</param>
        /// <returns>A new instance of <see cref="Result" /> representing a failure result with the specified error.</returns>
        public static Result Failure(IError error) => new(error);

        /// <summary>Creates a failure result with the given errors.</summary>
        /// <param name="errors">A collection of errors associated with the failure.</param>
        /// <returns>A new instance of <see cref="Result" /> representing a failure result with the specified errors.</returns>
        public static Result Failure(IEnumerable<IError> errors) => new(errors);

        /// <summary>Creates a failure result with the given errors.</summary>
        /// <param name="errors">A collection of errors associated with the failure.</param>
        /// <returns>A new instance of <see cref="Result" /> representing a failure result with the specified errors.</returns>
        public static Result Failure(IReadOnlyList<IError> errors) => new(errors);

        /// <summary>Creates a failure result.</summary>
        /// <typeparam name="TValue">The type of the value of the result.</typeparam>
        /// <returns>A new instance of <see cref="Result{TValue}" /> representing a failure result.</returns>
        public static Result<TValue> Failure<TValue>() => Result<TValue>.FailureResult;

        /// <summary>Creates a failure result with the given error message.</summary>
        /// <param name="errorMessage">The error message associated with the failure.</param>
        /// <typeparam name="TValue">The type of the value of the result.</typeparam>
        /// <returns>
        ///     A new instance of <see cref="Result{TValue}" /> representing a failure result with the specified error
        ///     message.
        /// </returns>
        public static Result<TValue> Failure<TValue>(string errorMessage)
        {
            Error error = new Error(errorMessage);
            return new Result<TValue>(error);
        }

        /// <summary>Creates a failure result with the given error message and metadata.</summary>
        /// <param name="errorMessage">The error message associated with the failure.</param>
        /// <param name="metadata">The metadata associated with the failure.</param>
        /// <typeparam name="TValue">The type of the value of the result.</typeparam>
        /// <returns>
        ///     A new instance of <see cref="Result{TValue}" /> representing a failure result with the specified error message
        ///     and metadata.
        /// </returns>
        public static Result<TValue> Failure<TValue>(string errorMessage, (string Key, object Value) metadata)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>(1) { { metadata.Key, metadata.Value } };
            Error error = new Error(errorMessage, dictionary);
            return new Result<TValue>(error);
        }

        /// <summary>Creates a failure result with the given error message and metadata.</summary>
        /// <param name="errorMessage">The error message associated with the failure.</param>
        /// <param name="metadata">The metadata associated with the failure.</param>
        /// <typeparam name="TValue">The type of the value of the result.</typeparam>
        /// <returns>
        ///     A new instance of <see cref="Result{TValue}" /> representing a failure result with the specified error message
        ///     and metadata.
        /// </returns>
        public static Result<TValue> Failure<TValue>(string errorMessage, KeyValuePair<string, object> metadata)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>(1) { { metadata.Key, metadata.Value } };
            Error error = new Error(errorMessage, dictionary);
            return new Result<TValue>(error);
        }

        /// <summary>Creates a failure result with the given error message and metadata.</summary>
        /// <param name="errorMessage">The error message associated with the failure.</param>
        /// <param name="metadata">The metadata associated with the failure.</param>
        /// <typeparam name="TValue">The type of the value of the result.</typeparam>
        /// <returns>
        ///     A new instance of <see cref="Result{TValue}" /> representing a failure result with the specified error message
        ///     and metadata.
        /// </returns>
        public static Result<TValue> Failure<TValue>(string errorMessage, IReadOnlyDictionary<string, object> metadata)
        {
            Error error = new Error(errorMessage, metadata);
            return new Result<TValue>(error);
        }

        /// <summary>Creates a failure result with the given exception.</summary>
        /// <param name="ex">The <see cref="Exception" /> associated with the failure.</param>
        /// <returns>A new instance of <see cref="Result{TValue}" /> representing a failure result with the specified exception.</returns>
        /// <remarks>
        ///     The exception is added to the error <see cref="Error.Metadata" /> under the key of "Exception" and the error
        ///     <see cref="Error.Message" /> is set to that
        ///     of the exception.
        /// </remarks>
        public static Result<TValue> Failure<TValue>(Exception ex)
        {
            ArgumentNullException.ThrowIfNull(ex);

            Dictionary<string, object> metadata = new Dictionary<string, object>(1) { { "Exception", ex } };
            Error error = new Error(ex.Message, metadata);
            return new Result<TValue>(error);
        }

        /// <summary>Creates a failure result with the given error message and exception.</summary>
        /// <param name="errorMessage">The error message associated with the failure.</param>
        /// <param name="ex">The <see cref="Exception" /> associated with the failure.</param>
        /// <returns>
        ///     A new instance of <see cref="Result{TValue}" /> representing a failure result with the specified error message
        ///     and exception.
        /// </returns>
        /// <remarks>The exception is added to the error <see cref="Error.Metadata" /> under the key of "Exception".</remarks>
        public static Result<TValue> Failure<TValue>(string errorMessage, Exception ex)
        {
            ArgumentNullException.ThrowIfNull(ex);

            Dictionary<string, object> metadata = new Dictionary<string, object>(1) { { "Exception", ex } };
            Error error = new Error(errorMessage, metadata);
            return new Result<TValue>(error);
        }

        /// <summary>Creates a failure result with the given error.</summary>
        /// <param name="error">The error associated with the failure.</param>
        /// <typeparam name="TValue">The type of the value of the result.</typeparam>
        /// <returns>A new instance of <see cref="Result{TValue}" /> representing a failure result with the specified error.</returns>
        public static Result<TValue> Failure<TValue>(IError error) => new(error);

        /// <summary>Creates a failure result with the given errors.</summary>
        /// <param name="errors">A collection of errors associated with the failure.</param>
        /// <typeparam name="TValue">The type of the value of the result.</typeparam>
        /// <returns>A new instance of <see cref="Result{TValue}" /> representing a failure result with the specified errors.</returns>
        public static Result<TValue> Failure<TValue>(IEnumerable<IError> errors) => new(errors);

        /// <summary>Creates a failure result with the given errors.</summary>
        /// <param name="errors">A collection of errors associated with the failure.</param>
        /// <typeparam name="TValue">The type of the value of the result.</typeparam>
        /// <returns>A new instance of <see cref="Result{TValue}" /> representing a failure result with the specified errors.</returns>
        public static Result<TValue> Failure<TValue>(IReadOnlyList<IError> errors) => new(errors);

        /// <inheritdoc />
        public bool IsFailure() => !_isSuccess;

        /// <inheritdoc />
        public bool IsFailure([MaybeNullWhen(false)] out IError error)
        {
            // ReSharper disable once PreferConcreteValueOverDefault
            // Easier to refactor if this changes.
            if (_isSuccess)
            {
                error = default;
            }
            else if (_errors is not null)
            {
                error = _errors[0];
            }
            else
            {
                error = Error.Empty;
            }

            return !_isSuccess;
        }

        /// <inheritdoc />
        public bool HasError<TError>()
            where TError : IError
        {
            if (_isSuccess)
            {
                return false;
            }

            if (_errors is not null)
                // Do not convert to LINQ, this creates unnecessary heap allocations.
                // For is the most efficient way to loop. It is the fastest and does not allocate.
                // ReSharper disable once ForCanBeConvertedToForeach
                // ReSharper disable once LoopCanBeConvertedToQuery
            {
                for (int index = 0; index < _errors.Count; index++)
                {
                    if (_errors[index] is TError)
                    {
                        return true;
                    }
                }
            }

            return typeof(TError) == typeof(Error);
        }

        /// <inheritdoc />
        public bool HasError<TError>([MaybeNullWhen(false)] out TError error)
            where TError : IError
        {
            if (_isSuccess)
            {
                error = default;
                return false;
            }

            if (_errors is not null)
                // Do not convert to LINQ, this creates unnecessary heap allocations.
                // For is the most efficient way to loop. It is the fastest and does not allocate.
                // ReSharper disable once ForCanBeConvertedToForeach
                // ReSharper disable once LoopCanBeConvertedToQuery
            {
                for (int index = 0; index < _errors.Count; index++)
                {
                    if (_errors[index] is not TError tError)
                    {
                        continue;
                    }

                    error = tError;
                    return true;
                }
            }

            if (typeof(TError) == typeof(Error))
            {
                error = (TError)Error.Empty;
                return true;
            }

            error = default;
            return false;
        }

        /// <summary>Implicitly converts an <see cref="Error" /> to a failure <see cref="Result{TValue}" />.</summary>
        /// <param name="error">The error to convert into a failure result.</param>
        /// <returns>A new instance of <see cref="Result{TValue}" /> representing a failure result with the specified error.</returns>
        [SuppressMessage("Usage", "CA2225: Operator overloads have named alternates", Justification = "We don't want to expose named alternates in this case.")]
        public static implicit operator Result(Error error) => new(error);

        /// <summary>Determines whether two <see cref="Result" /> instances are equal.</summary>
        /// <param name="other">The <see cref="Result" /> instance to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="Result" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public bool Equals(Result other) => Equals(_errors, other._errors);

        /// <summary>Determines whether the specified object is equal to this instance.</summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns><c>true</c> if the specified object is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object? obj) => obj is Result other && Equals(other);

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode() => _errors?.GetHashCode() ?? 0;

        /// <summary>Determines whether two <see cref="Result" /> instances are equal.</summary>
        /// <param name="left">The first <see cref="Result" /> instance to compare.</param>
        /// <param name="right">The second <see cref="Result" /> instance to compare.</param>
        /// <returns><c>true</c> if the specified <see cref="Result" /> instances are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(Result left, Result right) => left.Equals(right);

        /// <summary>Determines whether two <see cref="Result" /> instances are not equal.</summary>
        /// <param name="left">The first <see cref="Result" /> instance to compare.</param>
        /// <param name="right">The second <see cref="Result" /> instance to compare.</param>
        /// <returns><c>true</c> if the specified <see cref="Result" /> instances are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(Result left, Result right) => !left.Equals(right);

        /// <inheritdoc />
        public override string ToString()
        {
            if (_isSuccess)
            {
                return $"{nameof(Result)} {{ IsSuccess = True }}";
            }

            if (_errors is not null && _errors[0].Message.Length > 0)
            {
                return StringUtilities.GetResultErrorString(_errors[0].Message);
            }

            return $"{nameof(Result)} {{ IsSuccess = False }}";
        }
    }
}
