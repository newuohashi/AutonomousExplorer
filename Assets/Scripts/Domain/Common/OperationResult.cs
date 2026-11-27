using System;

namespace AutonomousExplorer.Domain.Common
{
    public enum OperationErrorCode
    {
        Unspecified = 0,
        None,
        InvalidPhase,
        InvalidTarget,
        NoSpace,
        NotFound,
        Unavailable,
        FullHP,
        AlreadyHandled,
        InvalidOrigin,
        Reserved
    }

    /// <summary>A result value only; applying or rolling back state belongs to the caller.</summary>
    public readonly struct OperationResult
    {
        private readonly string message;

        private OperationResult(bool success, OperationErrorCode errorCode, string message, EntityRef? relatedEntity)
        {
            Success = success;
            ErrorCode = errorCode;
            this.message = message;
            RelatedEntity = relatedEntity;
        }

        public bool Success { get; }
        public OperationErrorCode ErrorCode { get; }
        public string Message => message ?? string.Empty;
        public EntityRef? RelatedEntity { get; }

        public static OperationResult Succeeded() => new OperationResult(true, OperationErrorCode.None, string.Empty, null);

        public static OperationResult Failed(OperationErrorCode errorCode, string message = null, EntityRef? relatedEntity = null)
        {
            if (errorCode == OperationErrorCode.None || !Enum.IsDefined(typeof(OperationErrorCode), errorCode))
                throw new ArgumentOutOfRangeException(nameof(errorCode), "A defined failure code is required.");
            if (relatedEntity.HasValue && !relatedEntity.Value.IsValid)
                throw new ArgumentException("A related entity must have a valid identity.", nameof(relatedEntity));

            return new OperationResult(false, errorCode, message, relatedEntity);
        }
    }
}
