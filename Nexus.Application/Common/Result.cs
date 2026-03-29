namespace Nexus.Application.Common
{
    public enum ResultStatus
    {
        Success = 0,
        ValidationError = 1,
        NotFound = 2,
        Conflict = 3
    }

    public sealed class ResultError
    {
        public ResultError(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public string Code { get; }

        public string Message { get; }
    }

    public sealed class Result<T>
    {
        private Result(ResultStatus status, T? value, IReadOnlyList<ResultError> errors)
        {
            Status = status;
            Value = value;
            Errors = errors;
        }

        public ResultStatus Status { get; }

        public T? Value { get; }

        public IReadOnlyList<ResultError> Errors { get; }

        public bool IsSuccess => Status == ResultStatus.Success;

        public static Result<T> Success(T value)
        {
            return new Result<T>(ResultStatus.Success, value, Array.Empty<ResultError>());
        }

        public static Result<T> ValidationError(params ResultError[] errors)
        {
            return new Result<T>(ResultStatus.ValidationError, default, errors);
        }

        public static Result<T> ValidationError(IEnumerable<ResultError> errors)
        {
            return new Result<T>(ResultStatus.ValidationError, default, errors.ToArray());
        }

        public static Result<T> NotFound(string code, string message)
        {
            return new Result<T>(ResultStatus.NotFound, default, new[] { new ResultError(code, message) });
        }

        public static Result<T> Conflict(string code, string message)
        {
            return new Result<T>(ResultStatus.Conflict, default, new[] { new ResultError(code, message) });
        }
    }
}
