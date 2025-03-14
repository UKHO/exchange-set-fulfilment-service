﻿using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace UKHO.Infrastructure.Results.Tests
{
    public sealed class ResultTests
    {
        private static readonly Error EmptyError = new();

        [Fact]
        public void DefaultStruct_ShouldBeFailureResult()
        {
            // Arrange
            Result result = default;

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure(out IError? resultError)
                    .Should()
                    .BeTrue();
                resultError.Should()
                    .BeEquivalentTo(EmptyError);
                result.Errors
                    .Should()
                    .ContainSingle();
                result.Errors
                    .First()
                    .Should()
                    .BeOfType<Error>();
                result.HasError<Error>()
                    .Should()
                    .BeTrue();
                result.HasError<Error>(out Error? error)
                    .Should()
                    .BeTrue();
                error.Should()
                    .BeEquivalentTo(EmptyError);
                result.HasError<ValidationError>()
                    .Should()
                    .BeFalse();
                result.HasError<ValidationError>(out ValidationError? validationError)
                    .Should()
                    .BeFalse();
                validationError.Should()
                    .BeNull();
            }
        }

        [Fact]
        public void IsSuccess_WhenResultIsSuccess()
        {
            // Arrange
            Result result = Result.Success();

            // Assert
            result.IsSuccess()
                .Should()
                .BeTrue();
        }

        [Fact]
        public void IsFailure_WhenResultIsFailure()
        {
            // Arrange
            Result result = Result.Failure();

            // Assert
            result.IsFailure()
                .Should()
                .BeTrue();
        }

        [Fact]
        public void IsFailure_WhenResultIsFailure_ShouldReturnFirstError()
        {
            // Arrange
            Error firstError = new("Error 1");
            List<IError> errors = new() { firstError, new Error("Error 2") };
            Result result = Result.Failure(errors);

            // Act
            bool isFailure = result.IsFailure(out IError? resultError);

            // Assert
            using (new AssertionScope())
            {
                isFailure.Should()
                    .BeTrue();
                resultError.Should()
                    .Be(firstError);
            }
        }

        [Fact]
        public void IsFailure_WhenResultIsSuccess_ShouldReturnDefaultValue()
        {
            // Arrange
            Result result = Result.Success();

            // Act
            bool isFailure = result.IsFailure(out IError? resultError);

            // Assert
            using (new AssertionScope())
            {
                isFailure.Should()
                    .BeFalse();
                resultError.Should()
                    .BeNull();
            }
        }

        [Fact]
        public void IsFailure_WhenResultIsSuccess_ShouldReturnNullValue()
        {
            // Arrange
            Result result = Result.Success();

            // Act
            bool isFailure = result.IsFailure(out IError? resultError);

            // Assert
            using (new AssertionScope())
            {
                isFailure.Should()
                    .BeFalse();
                resultError.Should()
                    .BeNull();
            }
        }

        [Fact]
        public void Success_ShouldCreateSuccessResult()
        {
            // Act
            Result result = Result.Success();

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeTrue();
                result.IsFailure()
                    .Should()
                    .BeFalse();
                result.IsFailure(out IError? resultError)
                    .Should()
                    .BeFalse();
                resultError.Should()
                    .BeNull();
                result.Errors
                    .Should()
                    .BeEmpty();
            }
        }

        [Fact]
        public void SuccessTValue_WithValue_ShouldCreateSuccessResultWithValue()
        {
            // Arrange
            const int value = 42;

            // Act
            Result<int> result = Result.Success(value);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeTrue();
                result.IsSuccess(out int resultValue)
                    .Should()
                    .BeTrue();
                resultValue.Should()
                    .Be(value);
                result.IsFailure()
                    .Should()
                    .BeFalse();
                result.IsFailure(out IError? resultError)
                    .Should()
                    .BeFalse();
                resultError.Should()
                    .BeNull();
                result.Errors
                    .Should()
                    .BeEmpty();
            }
        }

        [Fact]
        public void Failure_ShouldCreateFailureResultWithSingleError()
        {
            // Act
            Result result = Result.Failure();

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure(out _)
                    .Should()
                    .BeTrue();
                result.Errors
                    .Should()
                    .ContainSingle()
                    .Which
                    .Message
                    .Should()
                    .Be("");
            }
        }

        [Fact]
        public void Failure_WithErrorMessage_ShouldCreateFailureResultWithSingleError()
        {
            // Arrange
            const string errorMessage = "Sample error message";

            // Act
            Result result = Result.Failure(errorMessage);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure(out _)
                    .Should()
                    .BeTrue();
                result.Errors
                    .Should()
                    .ContainSingle()
                    .Which
                    .Message
                    .Should()
                    .Be(errorMessage);
            }
        }

        [Fact]
        public void Failure_WithErrorMessageAndTupleMetadata_ShouldCreateFailureResultWithSingleError()
        {
            // Arrange
            const string errorMessage = "Sample error message";
            (string Key, object Value) metadata = ("Key", 0);

            // Act
            Result result = Result.Failure(errorMessage, metadata);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure(out _)
                    .Should()
                    .BeTrue();
                IError? error = result.Errors
                    .Should()
                    .ContainSingle()
                    .Which;
                error.Message
                    .Should()
                    .Be(errorMessage);
                error.Metadata
                    .Should()
                    .ContainSingle()
                    .Which
                    .Should()
                    .BeEquivalentTo(new KeyValuePair<string, object>("Key", 0));
            }
        }

        [Fact]
        public void Failure_WithErrorMessageAndKeyValuePairMetadata_ShouldCreateFailureResultWithSingleError()
        {
            // Arrange
            const string errorMessage = "Sample error message";
            KeyValuePair<string, object> metadata = new("Key", 0);

            // Act
            Result result = Result.Failure(errorMessage, metadata);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure(out _)
                    .Should()
                    .BeTrue();
                IError? error = result.Errors
                    .Should()
                    .ContainSingle()
                    .Which;
                error.Message
                    .Should()
                    .Be(errorMessage);
                error.Metadata
                    .Should()
                    .ContainSingle()
                    .Which
                    .Should()
                    .BeEquivalentTo(new KeyValuePair<string, object>("Key", 0));
            }
        }

        [Fact]
        public void Failure_WithErrorMessageAndDictionaryMetadata_ShouldCreateFailureResultWithSingleError()
        {
            // Arrange
            const string errorMessage = "Sample error message";
            IReadOnlyDictionary<string, object> metadata = new Dictionary<string, object> { { "Key", 0 } };

            // Act
            Result result = Result.Failure(errorMessage, metadata);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure(out _)
                    .Should()
                    .BeTrue();
                IError? error = result.Errors
                    .Should()
                    .ContainSingle()
                    .Which;
                error.Message
                    .Should()
                    .Be(errorMessage);
                error.Metadata
                    .Should()
                    .ContainSingle()
                    .Which
                    .Should()
                    .BeEquivalentTo(new KeyValuePair<string, object>("Key", 0));
            }
        }

        [Fact]
        public void Failure_WithException_ShouldCreateFailureResultWithSingleError()
        {
            // Arrange
            const string exceptionMessage = "Sample exception message";
            InvalidOperationException exception = new(exceptionMessage);

            // Act
            Result result = Result.Failure(exception);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                IError? error = result.Errors
                    .Should()
                    .ContainSingle()
                    .Which;
                error.Message
                    .Should()
                    .Be(exceptionMessage);
                KeyValuePair<string, object> metadata = error.Metadata
                    .Should()
                    .ContainSingle()
                    .Which;
                metadata.Key
                    .Should()
                    .Be("Exception");
                metadata.Value
                    .Should()
                    .Be(exception);
            }
        }

        [Fact]
        public void Failure_WithMessageAndException_ShouldCreateFailureResultWithSingleError()
        {
            // Arrange
            const string errorMessage = "Sample error message";
            const string exceptionMessage = "Sample exception message";
            InvalidOperationException exception = new(exceptionMessage);

            // Act
            Result result = Result.Failure(errorMessage, exception);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                IError? error = result.Errors
                    .Should()
                    .ContainSingle()
                    .Which;
                error.Message
                    .Should()
                    .Be(errorMessage);
                KeyValuePair<string, object> metadata = error.Metadata
                    .Should()
                    .ContainSingle()
                    .Which;
                metadata.Key
                    .Should()
                    .Be("Exception");
                metadata.Value
                    .Should()
                    .Be(exception);
            }
        }

        [Fact]
        public void Failure_WithNullException_ShouldThrow()
        {
            // Arrange
            Exception? exception = null;

            // Act
            Func<Result> act = () => Result.Failure(exception!);

            // Assert
            act.Should()
                .Throw<ArgumentNullException>();
        }

        [Fact]
        public void Failure_WithMessageAndNullException_ShouldThrow()
        {
            // Arrange
            Exception? exception = null;

            // Act
            Func<Result> act = () => Result.Failure("", exception!);

            // Assert
            act.Should()
                .Throw<ArgumentNullException>();
        }

        [Fact]
        public void Failure_WithErrorObject_ShouldCreateFailureResultWithSingleError()
        {
            // Arrange
            Error error = new("Sample error");

            // Act
            Result result = Result.Failure(error);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure(out _)
                    .Should()
                    .BeTrue();
                result.Errors
                    .Should()
                    .ContainSingle()
                    .Which
                    .Should()
                    .BeEquivalentTo(error);
            }
        }

        [Fact]
        public void Failure_WithErrorsEnumerable_ShouldCreateFailureResultWithMultipleErrors()
        {
            // Arrange
            List<IError> errors = new() { new Error("Error 1"), new Error("Error 2") };

            // Act
            Result result = Result.Failure(errors.AsEnumerable());

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure(out _)
                    .Should()
                    .BeTrue();
                result.Errors
                    .Should()
                    .HaveCount(2)
                    .And
                    .BeEquivalentTo(errors);
            }
        }

        [Fact]
        public void Failure_WithErrorsReadOnlyList_ShouldCreateFailureResultWithMultipleErrors()
        {
            // Arrange
            List<IError> errors = new() { new Error("Error 1"), new Error("Error 2") };

            // Act
            Result result = Result.Failure(errors);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure(out _)
                    .Should()
                    .BeTrue();
                result.Errors
                    .Should()
                    .HaveCount(2)
                    .And
                    .BeEquivalentTo(errors);
            }
        }

        [Fact]
        public void FailureTValue_ShouldCreateFailureResultWithSingleError()
        {
            // Act
            Result<object> result = Result.Failure<object>();

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsSuccess(out _)
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure(out _)
                    .Should()
                    .BeTrue();
                result.Errors
                    .Should()
                    .ContainSingle()
                    .Which
                    .Message
                    .Should()
                    .Be("");
            }
        }

        [Fact]
        public void FailureTValue_WithErrorMessage_ShouldCreateFailureResultWithSingleError()
        {
            // Arrange
            const string errorMessage = "Sample error message";

            // Act
            Result<object> result = Result.Failure<object>(errorMessage);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsSuccess(out _)
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure(out _)
                    .Should()
                    .BeTrue();
                result.Errors
                    .Should()
                    .ContainSingle()
                    .Which
                    .Message
                    .Should()
                    .Be(errorMessage);
            }
        }

        [Fact]
        public void FailureTValue_WithErrorMessageAndTupleMetadata_ShouldCreateFailureResultWithSingleError()
        {
            // Arrange
            const string errorMessage = "Sample error message";
            (string Key, object Value) metadata = ("Key", 0);

            // Act
            Result<object> result = Result.Failure<object>(errorMessage, metadata);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsSuccess(out _)
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure(out _)
                    .Should()
                    .BeTrue();
                IError? error = result.Errors
                    .Should()
                    .ContainSingle()
                    .Which;
                error.Message
                    .Should()
                    .Be(errorMessage);
                error.Metadata
                    .Should()
                    .ContainSingle()
                    .Which
                    .Should()
                    .BeEquivalentTo(new KeyValuePair<string, object>("Key", 0));
            }
        }

        [Fact]
        public void FailureTValue_WithErrorMessageAndKeyValuePairMetadata_ShouldCreateFailureResultWithSingleError()
        {
            // Arrange
            const string errorMessage = "Sample error message";
            KeyValuePair<string, object> metadata = new("Key", 0);

            // Act
            Result<object> result = Result.Failure<object>(errorMessage, metadata);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsSuccess(out _)
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure(out _)
                    .Should()
                    .BeTrue();
                IError? error = result.Errors
                    .Should()
                    .ContainSingle()
                    .Which;
                error.Message
                    .Should()
                    .Be(errorMessage);
                error.Metadata
                    .Should()
                    .ContainSingle()
                    .Which
                    .Should()
                    .BeEquivalentTo(new KeyValuePair<string, object>("Key", 0));
            }
        }

        [Fact]
        public void FailureTValue_WithErrorMessageAndDictionaryMetadata_ShouldCreateFailureResultWithSingleError()
        {
            // Arrange
            const string errorMessage = "Sample error message";
            IReadOnlyDictionary<string, object> metadata = new Dictionary<string, object> { { "Key", 0 } };

            // Act
            Result<object> result = Result.Failure<object>(errorMessage, metadata);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsSuccess(out _)
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure(out _)
                    .Should()
                    .BeTrue();
                IError? error = result.Errors
                    .Should()
                    .ContainSingle()
                    .Which;
                error.Message
                    .Should()
                    .Be(errorMessage);
                error.Metadata
                    .Should()
                    .ContainSingle()
                    .Which
                    .Should()
                    .BeEquivalentTo(new KeyValuePair<string, object>("Key", 0));
            }
        }

        [Fact]
        public void FailureTValue_WithException_ShouldCreateFailureResultWithSingleError()
        {
            // Arrange
            const string exceptionMessage = "Sample exception message";
            InvalidOperationException exception = new(exceptionMessage);

            // Act
            Result<object> result = Result.Failure<object>(exception);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                IError? error = result.Errors
                    .Should()
                    .ContainSingle()
                    .Which;
                error.Message
                    .Should()
                    .Be(exceptionMessage);
                KeyValuePair<string, object> metadata = error.Metadata
                    .Should()
                    .ContainSingle()
                    .Which;
                metadata.Key
                    .Should()
                    .Be("Exception");
                metadata.Value
                    .Should()
                    .Be(exception);
            }
        }

        [Fact]
        public void FailureTValue_WithMessageAndException_ShouldCreateFailureResultWithSingleError()
        {
            // Arrange
            const string errorMessage = "Sample error message";
            const string exceptionMessage = "Sample exception message";
            InvalidOperationException exception = new(exceptionMessage);

            // Act
            Result<object> result = Result.Failure<object>(errorMessage, exception);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                IError? error = result.Errors
                    .Should()
                    .ContainSingle()
                    .Which;
                error.Message
                    .Should()
                    .Be(errorMessage);
                KeyValuePair<string, object> metadata = error.Metadata
                    .Should()
                    .ContainSingle()
                    .Which;
                metadata.Key
                    .Should()
                    .Be("Exception");
                metadata.Value
                    .Should()
                    .Be(exception);
            }
        }

        [Fact]
        public void FailureTValue_WithNullException_ShouldThrow()
        {
            // Arrange
            Exception? exception = null;

            // Act
            Func<Result<object>> act = () => Result.Failure<object>(exception!);

            // Assert
            act.Should()
                .Throw<ArgumentNullException>();
        }

        [Fact]
        public void FailureTValue_WithMessageAndNullException_ShouldThrow()
        {
            // Arrange
            Exception? exception = null;

            // Act
            Func<Result<object>> act = () => Result.Failure<object>("", exception!);

            // Assert
            act.Should()
                .Throw<ArgumentNullException>();
        }

        [Fact]
        public void FailureTValue_WithErrorObject_ShouldCreateFailureResultWithSingleError()
        {
            // Arrange
            Error error = new("Sample error");

            // Act
            Result<object> result = Result.Failure<object>(error);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsSuccess(out _)
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure(out _)
                    .Should()
                    .BeTrue();
                result.Errors
                    .Should()
                    .ContainSingle()
                    .Which
                    .Should()
                    .BeEquivalentTo(error);
            }
        }

        [Fact]
        public void FailureTValue_WithErrorsEnumerable_ShouldCreateFailureResultWithMultipleErrors()
        {
            // Arrange
            List<IError> errors = new() { new Error("Error 1"), new Error("Error 2") };

            // Act
            Result<object> result = Result.Failure<object>(errors);

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsSuccess(out _)
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure(out _)
                    .Should()
                    .BeTrue();
                result.Errors
                    .Should()
                    .HaveCount(2)
                    .And
                    .BeEquivalentTo(errors);
            }
        }

        [Fact]
        public void HasError_WithMatchingErrorType_ShouldReturnTrue()
        {
            // Arrange
            Result result = Result.Failure(new ValidationError("Validation error"));

            // Assert
            result.HasError<ValidationError>()
                .Should()
                .BeTrue();
        }

        [Fact]
        public void HasError_WithMatchingErrorType_ShouldOutFirstMatch()
        {
            // Arrange
            ValidationError firstError = new("Validation error");
            List<IError> errors = new() { firstError, new ValidationError("Validation error 2") };
            Result result = Result.Failure(errors);

            // Act
            bool hasError = result.HasError<ValidationError>(out ValidationError? error);

            // Assert
            using (new AssertionScope())
            {
                hasError.Should()
                    .BeTrue();
                error.Should()
                    .Be(firstError);
            }
        }

        [Fact]
        public void HasError_WithNonMatchingErrorType_ShouldReturnFalse()
        {
            // Arrange
            Result result = Result.Failure(new Error("Generic error"));

            // Assert
            result.HasError<ValidationError>()
                .Should()
                .BeFalse();
        }

        [Fact]
        public void HasError_WithNonMatchingErrorType_ShouldOutDefaultError()
        {
            // Arrange
            Result result = Result.Failure(new Error("Generic error"));

            // Act
            bool hasError = result.HasError<ValidationError>(out ValidationError? error);

            // Assert
            using (new AssertionScope())
            {
                hasError.Should()
                    .BeFalse();
                error.Should()
                    .BeNull();
            }
        }

        [Fact]
        public void HasError_WhenIsSuccess_ShouldReturnFalse()
        {
            // Arrange
            Result result = Result.Success();

            // Assert
            result.HasError<ValidationError>()
                .Should()
                .BeFalse();
        }

        [Fact]
        public void HasError_WhenIsSuccess_ShouldOutDefaultError()
        {
            // Arrange
            Result result = Result.Success();

            // Act
            bool hasError = result.HasError<ValidationError>(out ValidationError? error);

            // Assert
            using (new AssertionScope())
            {
                hasError.Should()
                    .BeFalse();
                error.Should()
                    .BeNull();
            }
        }

        [Fact]
        public void ImplicitCast_ShouldCreateFailureResultFromError()
        {
            // Arrange
            Error error = new("Sample error");

            // Act
            Result result = error;

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure(out IError? resultError)
                    .Should()
                    .BeTrue();
                resultError.Should()
                    .BeEquivalentTo(error);
                result.Errors
                    .Should()
                    .ContainSingle()
                    .Which
                    .Should()
                    .BeEquivalentTo(error);
            }
        }

        [Fact]
        public void Equals_Result_ShouldReturnTrueForEqualResults()
        {
            // Arrange
            Result result1 = Result.Success();
            Result result2 = Result.Success();

            // Assert
            result1.Equals(result2)
                .Should()
                .BeTrue();
        }

        [Fact]
        public void Equals_Result_ShouldReturnFalseForUnequalResults()
        {
            // Arrange
            Result result1 = Result.Success();
            Result result2 = Result.Failure("Error");

            // Assert
            result1.Equals(result2)
                .Should()
                .BeFalse();
        }

        [Fact]
        public void Equals_Object_ShouldReturnTrueForEqualResults()
        {
            // Arrange
            Result result1 = Result.Success();
            Result result2 = Result.Success();

            // Assert
            result1.Equals((object)result2)
                .Should()
                .BeTrue();
        }

        [Fact]
        public void Equals_Object_ShouldReturnFalseForUnequalResults()
        {
            // Arrange
            Result result1 = Result.Success();
            Result result2 = Result.Failure("Error");

            // Assert
            result1.Equals((object)result2)
                .Should()
                .BeFalse();
        }

        [Fact]
        public void GetHashCode_ShouldReturnSameHashCodeForEqualResults()
        {
            // Arrange
            Result result1 = Result.Success();
            Result result2 = Result.Success();

            // Assert
            result1.GetHashCode()
                .Should()
                .Be(result2.GetHashCode());
        }

        [Fact]
        public void op_Equality_Result_ShouldReturnTrueForEqualResults()
        {
            // Arrange
            Result result1 = Result.Success();
            Result result2 = Result.Success();

            // Assert
            (result1 == result2).Should()
                .BeTrue();
        }

        [Fact]
        public void op_Equality_Result_ShouldReturnFalseForUnequalResults()
        {
            // Arrange
            Result result1 = Result.Success();
            Result result2 = Result.Failure("Error");

            // Assert
            (result1 == result2).Should()
                .BeFalse();
        }

        [Fact]
        public void op_Inequality_Result_ShouldReturnFalseForEqualResults()
        {
            // Arrange
            Result result1 = Result.Success();
            Result result2 = Result.Success();

            // Assert
            (result1 != result2).Should()
                .BeFalse();
        }

        [Fact]
        public void op_Inequality_Result_ShouldReturnTrueForUnequalResults()
        {
            // Arrange
            Result result1 = Result.Success();
            Result result2 = Result.Failure("Error");

            // Assert
            (result1 != result2).Should()
                .BeTrue();
        }

        [Fact]
        public void ToString_WhenSuccess_ShouldReturnStringRepresentation()
        {
            // Arrange
            Result result = Result.Success();

            // Assert
            result.ToString()
                .Should()
                .Be("Result { IsSuccess = True }");
        }

        [Theory]
        [InlineData("")]
        [InlineData("An unknown error occured!")]
        public void ToString_WhenFailure_ShouldReturnStringRepresentation(string errorMessage)
        {
            // Arrange
            Result result = Result.Failure(errorMessage);

            // Assert
            result.ToString()
                .Should()
                .Be(errorMessage.Length > 0 ? $"Result {{ IsSuccess = False, Error = \"{errorMessage}\" }}" : "Result { IsSuccess = False }");
        }

        [Fact]
        public void InterfaceSuccess_ShouldCreateSuccessResult()
        {
            // Arrange
            static Result Success<TResult>()
                where TResult : IActionableResult<Result>
            {
                return TResult.Success();
            }

            // Act
            Result result = Success<Result>();

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeTrue();
                result.IsFailure()
                    .Should()
                    .BeFalse();
                result.IsFailure(out IError? resultError)
                    .Should()
                    .BeFalse();
                resultError.Should()
                    .BeNull();
                result.Errors
                    .Should()
                    .BeEmpty();
            }
        }

        [Fact]
        public void InterfaceFailure_ShouldCreateFailureResultWithSingleError()
        {
            // Arrange
            static Result Fail<TResult>()
                where TResult : IActionableResult<Result>
            {
                return TResult.Failure();
            }

            // Act
            Result result = Fail<Result>();

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure(out _)
                    .Should()
                    .BeTrue();
                result.Errors
                    .Should()
                    .ContainSingle()
                    .Which
                    .Message
                    .Should()
                    .Be("");
            }
        }

        [Fact]
        public void InterfaceFailure_WithErrorMessage_ShouldCreateFailureResultWithSingleError()
        {
            // Arrange
            static Result Fail<TResult>()
                where TResult : IActionableResult<Result>
            {
                const string errorMessage = "Sample error message";
                return TResult.Failure(errorMessage);
            }

            // Act
            Result result = Fail<Result>();

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure(out _)
                    .Should()
                    .BeTrue();
                result.Errors
                    .Should()
                    .ContainSingle()
                    .Which
                    .Message
                    .Should()
                    .Be("Sample error message");
            }
        }

        [Fact]
        public void InterfaceFailure_WithErrorMessageAndTupleMetadata_ShouldCreateFailureResultWithSingleError()
        {
            // Arrange
            static Result Fail<TResult>()
                where TResult : IActionableResult<Result>
            {
                const string errorMessage = "Sample error message";
                (string Key, object Value) metadata = ("Key", 0);
                return TResult.Failure(errorMessage, metadata);
            }

            // Act
            Result result = Fail<Result>();

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure(out _)
                    .Should()
                    .BeTrue();
                IError? error = result.Errors
                    .Should()
                    .ContainSingle()
                    .Which;
                error.Message
                    .Should()
                    .Be("Sample error message");
                error.Metadata
                    .Should()
                    .ContainSingle()
                    .Which
                    .Should()
                    .BeEquivalentTo(new KeyValuePair<string, object>("Key", 0));
            }
        }

        [Fact]
        public void InterfaceFailure_WithErrorMessageAndDictionaryMetadata_ShouldCreateFailureResultWithSingleError()
        {
            // Arrange
            static Result Fail<TResult>()
                where TResult : IActionableResult<Result>
            {
                const string errorMessage = "Sample error message";
                IReadOnlyDictionary<string, object> metadata = new Dictionary<string, object> { { "Key", 0 } };
                return TResult.Failure(errorMessage, metadata);
            }

            // Act
            Result result = Fail<Result>();

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure(out _)
                    .Should()
                    .BeTrue();
                IError? error = result.Errors
                    .Should()
                    .ContainSingle()
                    .Which;
                error.Message
                    .Should()
                    .Be("Sample error message");
                error.Metadata
                    .Should()
                    .ContainSingle()
                    .Which
                    .Should()
                    .BeEquivalentTo(new KeyValuePair<string, object>("Key", 0));
            }
        }

        [Fact]
        public void InterfaceFailure_WithErrorObject_ShouldCreateFailureResultWithSingleError()
        {
            // Arrange
            static Result Fail<TResult>()
                where TResult : IActionableResult<Result>
            {
                Error error = new("Sample error");
                return TResult.Failure(error);
            }

            // Act
            Result result = Fail<Result>();

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure(out _)
                    .Should()
                    .BeTrue();
                result.Errors
                    .Should()
                    .ContainSingle()
                    .Which
                    .Should()
                    .BeEquivalentTo(new Error("Sample error"));
            }
        }

        [Fact]
        public void InterfaceFailure_WithErrorsEnumerable_ShouldCreateFailureResultWithMultipleErrors()
        {
            // Arrange
            static Result Fail<TResult>()
                where TResult : IActionableResult<Result>
            {
                List<IError> errors = new() { new Error("Error 1"), new Error("Error 2") };
                return TResult.Failure(errors.AsEnumerable());
            }

            // Act
            Result result = Fail<Result>();

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure(out _)
                    .Should()
                    .BeTrue();
                result.Errors
                    .Should()
                    .HaveCount(2)
                    .And
                    .BeEquivalentTo(new List<IError> { new Error("Error 1"), new Error("Error 2") }
                    );
            }
        }

        [Fact]
        public void InterfaceFailure_WithErrorsReadOnlyList_ShouldCreateFailureResultWithMultipleErrors()
        {
            // Arrange
            static Result Fail<TResult>()
                where TResult : IActionableResult<Result>
            {
                List<IError> errors = new() { new Error("Error 1"), new Error("Error 2") };
                return TResult.Failure(errors);
            }

            // Act
            Result result = Fail<Result>();

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsFailure()
                    .Should()
                    .BeTrue();
                result.IsFailure(out _)
                    .Should()
                    .BeTrue();
                result.Errors
                    .Should()
                    .HaveCount(2)
                    .And
                    .BeEquivalentTo(new List<IError> { new Error("Error 1"), new Error("Error 2") }
                    );
            }
        }

        private class ValidationError(string errorMessage) : Error(errorMessage);
    }
}
