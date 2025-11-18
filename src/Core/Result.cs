using System;

namespace Faster.EventBus.Core
{
    /// <summary>
    /// Represents the outcome of an operation without a value.
    /// </summary>
    public readonly struct Result
    {
        public bool IsSuccess { get; }
        public string Error { get; }

        private Result(bool isSuccess, string error)
        {
            IsSuccess = isSuccess;
            Error = error ?? string.Empty;
        }

        public static Result Success() => new Result(true, string.Empty);

        public static Result Failure(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                throw new ArgumentException("Error message must not be empty", nameof(error));

            return new Result(false, error);
        }

        /// <summary>
        /// Pattern matching on Result outcomes.
        /// </summary>
        public void Match(Action success, Action<string> failure)
        {
            if (IsSuccess) success();
            else failure(Error);
        }

        public Result<T> To<T>(T value = default) =>
            IsSuccess ? Result<T>.Success(value) : Result<T>.Failure(Error);

        public override string ToString() =>
            IsSuccess ? "Success" : $"Failure: {Error}";
    }

    /// <summary>
    /// Represents the outcome of an operation with a value.
    /// </summary>
    public readonly struct Result<T>
    {
        public bool IsSuccess { get; }
        public string Error { get; }
        public T Value { get; }

        private Result(bool isSuccess, T value, string error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error ?? string.Empty;
        }

        public static Result<T> Success(T value) =>
            new Result<T>(true, value, string.Empty);

        public static Result<T> Failure(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                throw new ArgumentException("Error message must not be empty", nameof(error));

            return new Result<T>(false, default!, error);
        }

        /// <summary>
        /// Pattern matching for value results.
        /// </summary>
        public void Match(Action<T> success, Action<string> failure)
        {
            if (IsSuccess) success(Value);
            else failure(Error);
        }

        public Result ToResult() =>
            IsSuccess ? Result.Success() : Result.Failure(Error);

        public override string ToString() =>
            IsSuccess ? $"Success: {Value}" : $"Failure: {Error}";
    }
}
