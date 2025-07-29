using System;
using Microsoft.Extensions.Logging;

namespace UKHO.ADDS.EFS.Trigger.Logging
{
    internal static partial class EFSTriggerLogging
    {
        private const int BaseEventId = 3000;
        private const int FunctionStarted = BaseEventId + 1;
        private const int SendingJobRequest = BaseEventId + 2;
        private const int JobApiSucceeded = BaseEventId + 3;
        private const int JobApiClientError = BaseEventId + 4;
        private const int JobApiServerError = BaseEventId + 5;
        private const int JobApiUnexpectedStatus = BaseEventId + 6;
        private const int JobApiException = BaseEventId + 7;
        private const int MissingOrchestratorApiUrl = BaseEventId + 8;
        private const int NextSchedule = BaseEventId + 9;

        public static readonly EventId OrchestratorFunctionStarted = new(FunctionStarted, nameof(FunctionStarted));
        public static readonly EventId OrchestratorSendingJobRequest = new(SendingJobRequest, nameof(SendingJobRequest));
        public static readonly EventId OrchestratorJobApiSucceeded = new(JobApiSucceeded, nameof(JobApiSucceeded));
        public static readonly EventId OrchestratorJobApiClientError = new(JobApiClientError, nameof(JobApiClientError));
        public static readonly EventId OrchestratorJobApiServerError = new(JobApiServerError, nameof(JobApiServerError));
        public static readonly EventId OrchestratorJobApiUnexpectedStatus = new(JobApiUnexpectedStatus, nameof(JobApiUnexpectedStatus));
        public static readonly EventId OrchestratorJobApiException = new(JobApiException, nameof(JobApiException));
        public static readonly EventId OrchestratorMissingApiUrl = new(MissingOrchestratorApiUrl, nameof(MissingOrchestratorApiUrl));
        public static readonly EventId OrchestratorNextSchedule = new(NextSchedule, nameof(NextSchedule));

        [LoggerMessage(FunctionStarted, LogLevel.Information, "Efs Function triggered at: {triggeredAt}", EventName = nameof(OrchestratorFunctionStarted))]
        public static partial void LogOrchestratorFunctionStarted(this ILogger logger, DateTime triggeredAt);

        [LoggerMessage(SendingJobRequest, LogLevel.Information, "Sending Job API request: {Url} CorrelationId: {CorrelationId} Payload: {Payload}", EventName = nameof(OrchestratorSendingJobRequest))]
        public static partial void LogOrchestratorSendingJobRequest(this ILogger logger, string url, string correlationId, string payload);

        [LoggerMessage(JobApiSucceeded, LogLevel.Information, "Job API succeeded. Status: {StatusCode} Response: {Response}", EventName = nameof(OrchestratorJobApiSucceeded))]
        public static partial void LogOrchestratorJobApiSucceeded(this ILogger logger, int statusCode, string response);

        [LoggerMessage(JobApiClientError, LogLevel.Warning, "Job API client error. Status: {StatusCode} Response: {Response}", EventName = nameof(OrchestratorJobApiClientError))]
        public static partial void LogOrchestratorJobApiClientError(this ILogger logger, int statusCode, string response);

        [LoggerMessage(JobApiServerError, LogLevel.Error, "Job API server error. Status: {StatusCode} Response: {Response}", EventName = nameof(OrchestratorJobApiServerError))]
        public static partial void LogOrchestratorJobApiServerError(this ILogger logger, int statusCode, string response);

        [LoggerMessage(JobApiUnexpectedStatus, LogLevel.Warning, "Job API unexpected status. Status: {StatusCode} Response: {Response}", EventName = nameof(OrchestratorJobApiUnexpectedStatus))]
        public static partial void LogOrchestratorJobApiUnexpectedStatus(this ILogger logger, int statusCode, string response);

        [LoggerMessage(JobApiException, LogLevel.Error, "Exception calling Orchestrator Job API. CorrelationId: {CorrelationId}", EventName = nameof(OrchestratorJobApiException))]
        public static partial void LogOrchestratorJobApiException(this ILogger logger, Exception exception, string correlationId);

        [LoggerMessage(MissingOrchestratorApiUrl, LogLevel.Error, "OrchestratorApiUrl is not configured.", EventName = nameof(OrchestratorMissingApiUrl))]
        public static partial void LogOrchestratorMissingApiUrl(this ILogger logger);

        [LoggerMessage(NextSchedule, LogLevel.Information, "Next request schedule at: {NextSchedule}", EventName = nameof(OrchestratorNextSchedule))]
        public static partial void LogOrchestratorNextSchedule(this ILogger logger, DateTime? nextSchedule);
    }
}
