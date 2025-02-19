using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

// ReSharper disable SuspiciousTypeConversion.Global

namespace UKHO.Infrastructure.Results.Tests
{
    public sealed class ResultTValueTests
    {
        private static readonly Error EmptyError = new();

        [Fact]
        public void DefaultStruct_ShouldBeFailureResultWithDefaultValue()
        {
            // Arrange
            Result<int> result = default;

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsSuccess(out int resultValue)
                    .Should()
                    .BeFalse();
                resultValue.Should()
                    .Be(0);
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
                    .ContainSingle()
                    .Which
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
        public void DefaultStruct_ShouldBeFailureResultWithNullValue()
        {
            // Arrange
            Result<object> result = default;

            // Assert
            using (new AssertionScope())
            {
                result.IsSuccess()
                    .Should()
                    .BeFalse();
                result.IsSuccess(out object? resultValue)
                    .Should()
                    .BeFalse();
                resultValue.Should()
                    .BeNull();
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
                    .ContainSingle()
                    .Which
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
            Result<int> result = Result.Success(42);

            // Assert
            result.IsSuccess()
                .Should()
                .BeTrue();
        }

        [Fact]
        public void IsSuccess_WhenResultIsSuccess_ShouldReturnAssignedValue()
        {
            // Arrange
            Result<int> result = Result.Success(42);

            // Act
            bool isSuccess = result.IsSuccess(out int resultValue);

            // Assert
            using (new AssertionScope())
            {
                isSuccess.Should()
                    .BeTrue();
                resultValue.Should()
                    .Be(42);
            }
        }

        [Fact]
        public void IsSuccess_WhenResultIsSuccess_ShouldReturnAssignedValueAndNullError()
        {
            // Arrange
            Result<int> result = Result.Success(42);

            // Act
            bool isSuccess = result.IsSuccess(out int resultValue, out IError? resultError);

            // Assert
            using (new AssertionScope())
            {
                isSuccess.Should()
                    .BeTrue();
                resultValue.Should()
                    .Be(42);
                resultError.Should()
                    .BeNull();
            }
        }

        [Fact]
        public void IsSuccess_WhenResultIsFailure_ShouldReturnDefaultValue()
        {
            // Arrange
            Result<int> result = Result.Failure<int>("Error message");

            // Act
            bool isSuccess = result.IsSuccess(out int resultValue);

            // Assert
            using (new AssertionScope())
            {
                isSuccess.Should()
                    .BeFalse();
                resultValue.Should()
                    .Be(0);
            }
        }

        [Fact]
        public void IsSuccess_WhenResultIsFailure_ShouldReturnNullValue()
        {
            // Arrange
            Result<object> result = Result.Failure<object>("Error message");

            // Act
            bool isSuccess = result.IsSuccess(out object? resultValue);

            // Assert
            using (new AssertionScope())
            {
                isSuccess.Should()
                    .BeFalse();
                resultValue.Should()
                    .BeNull();
            }
        }

        [Fact]
        public void IsSuccess_WhenResultIsFailure_ShouldReturnDefaultValueAndFirstError()
        {
            // Arrange
            Error firstError = new("Error 1");
            List<IError> errors = new() { firstError, new Error("Error 2") };
            Result<int> result = Result.Failure<int>(errors);

            // Act
            bool isSuccess = result.IsSuccess(out int resultValue, out IError? resultError);

            // Assert
            using (new AssertionScope())
            {
                isSuccess.Should()
                    .BeFalse();
                resultValue.Should()
                    .Be(0);
                resultError.Should()
                    .Be(firstError);
            }
        }

        [Fact]
        public void IsSuccess_WhenResultIsFailure_ShouldReturnNullValueAndFirstError()
        {
            // Arrange
            Error firstError = new("Error 1");
            List<IError> errors = new() { firstError, new Error("Error 2") };
            Result<object> result = Result.Failure<object>(errors);

            // Act
            bool isSuccess = result.IsSuccess(out object? resultValue, out IError? resultError);

            // Assert
            using (new AssertionScope())
            {
                isSuccess.Should()
                    .BeFalse();
                resultValue.Should()
                    .BeNull();
                resultError.Should()
                    .Be(firstError);
            }
        }

        [Fact]
        public void IsSuccess_WhenResultIsFailure_ShouldReturnDefaultValueAndDefaultError()
        {
            // Arrange
            Result<object> result = default;

            // Act
            bool isSuccess = result.IsSuccess(out object? resultValue, out IError? resultError);

            // Assert
            using (new AssertionScope())
            {
                isSuccess.Should()
                    .BeFalse();
                resultValue.Should()
                    .BeNull();
                resultError.Should()
                    .BeEquivalentTo(EmptyError);
            }
        }

        [Fact]
        public void IsFailure_WhenResultIsFailure()
        {
            // Arrange
            Result<int> result = Result.Failure<int>();

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
            Result<int> result = Result.Failure<int>(errors);

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
        public void IsFailure_WhenResultIsSuccess_ShouldReturnNullError()
        {
            // Arrange
            Result<int> result = Result.Success(42);

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
        public void IsFailure_WhenResultIsFailure_ShouldReturnFirstErrorAndDefaultValue()
        {
            // Arrange
            Error firstError = new("Error 1");
            List<IError> errors = new() { firstError, new Error("Error 2") };
            Result<int> result = Result.Failure<int>(errors);

            // Act
            bool isFailure = result.IsFailure(out IError? resultError, out int resultValue);

            // Assert
            using (new AssertionScope())
            {
                isFailure.Should()
                    .BeTrue();
                resultError.Should()
                    .Be(firstError);
                resultValue.Should()
                    .Be(0);
            }
        }

        [Fact]
        public void IsFailure_WhenResultIsFailure_ShouldReturnFirstErrorAndNullValue()
        {
            // Arrange
            Error firstError = new("Error 1");
            List<IError> errors = new() { firstError, new Error("Error 2") };
            Result<object> result = Result.Failure<object>(errors);

            // Act
            bool isFailure = result.IsFailure(out IError? resultError, out object? resultValue);

            // Assert
            using (new AssertionScope())
            {
                isFailure.Should()
                    .BeTrue();
                resultError.Should()
                    .Be(firstError);
                resultValue.Should()
                    .BeNull();
            }
        }

        [Fact]
        public void IsFailure_WhenResultIsFailure_ShouldReturnDefaultErrorAndNullValue()
        {
            // Arrange
            Result<object> result = default;

            // Act
            bool isFailure = result.IsFailure(out IError? resultError, out object? resultValue);

            // Assert
            using (new AssertionScope())
            {
                isFailure.Should()
                    .BeTrue();
                resultError.Should()
                    .BeEquivalentTo(EmptyError);
                resultValue.Should()
                    .BeNull();
            }
        }

        [Fact]
        public void IsFailure_WhenResultIsSuccess_ShouldReturnNullErrorAndAssignedValue()
        {
            // Arrange
            Result<int> result = Result.Success(42);

            // Act
            bool isFailure = result.IsFailure(out IError? resultError, out int resultValue);

            // Assert
            using (new AssertionScope())
            {
                isFailure.Should()
                    .BeFalse();
                resultError.Should()
                    .BeNull();
                resultValue.Should()
                    .Be(42);
            }
        }

        [Fact]
        public void Success_WithValue_ShouldCreateSuccessResultWithValue()
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
            Result<int> result = Result.Failure<int>();

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
        public void Failure_WithErrorMessage_ShouldCreateFailureResultWithSingleError()
        {
            // Arrange
            const string errorMessage = "Sample error message";

            // Act
            Result<int> result = Result.Failure<int>(errorMessage);

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
        public void Failure_WithErrorMessageAndTupleMetadata_ShouldCreateFailureResultWithSingleError()
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
        public void Failure_WithErrorMessageAndDictionaryMetadata_ShouldCreateFailureResultWithSingleError()
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
        public void Failure_WithErrorObject_ShouldCreateFailureResultWithSingleError()
        {
            // Arrange
            Error error = new("Sample error");

            // Act
            Result<int> result = Result.Failure<int>(error);

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
        public void Failure_WithErrorsEnumerable_ShouldCreateFailureResultWithMultipleErrors()
        {
            // Arrange
            List<IError> errors = new() { new Error("Error 1"), new Error("Error 2") };

            // Act
            Result<int> result = Result.Failure<int>(errors.AsEnumerable());

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
        public void Failure_WithErrorsReadOnlyList_ShouldCreateFailureResultWithMultipleErrors()
        {
            // Arrange
            List<IError> errors = new() { new Error("Error 1"), new Error("Error 2") };

            // Act
            Result<int> result = Result.Failure<int>(errors);

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
            Result<int> result = Result.Failure<int>(new ValidationError("Validation error"));

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
            List<IError> errors = new() { firstError, new ValidationError("Error 2") };
            Result<int> result = Result.Failure<int>(errors);

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
            Result<int> result = Result.Failure<int>(new Error("Generic error"));

            // Assert
            result.HasError<ValidationError>()
                .Should()
                .BeFalse();
        }

        [Fact]
        public void HasError_WithNonMatchingErrorType_ShouldOutDefaultError()
        {
            // Arrange
            Result<int> result = Result.Failure<int>(new Error("Generic error"));

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
            Result<int> result = Result.Success(42);

            // Assert
            result.HasError<ValidationError>()
                .Should()
                .BeFalse();
        }

        [Fact]
        public void HasError_WhenIsSuccess_ShouldOutDefaultError()
        {
            // Arrange
            Result<int> result = Result.Success(42);

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
        public void ImplicitOperator_ShouldCreateSuccessResultWithValue()
        {
            // Arrange
            const int value = 42;

            // Act
            Result<int> result = value;

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
                result.IsFailure(out _)
                    .Should()
                    .BeFalse();
                result.Errors
                    .Should()
                    .BeEmpty();
            }
        }

        [Fact]
        public void AsFailure_ShouldConvertResultToNonGenericResultWithSameErrors()
        {
            // Arrange
            List<IError> errors = new() { new Error("Error 1"), new Error("Error 2") };
            Result<int> result = Result.Failure<int>(errors);

            // Act
            Result nonGenericResult = result.AsFailure();

            // Assert
            using (new AssertionScope())
            {
                nonGenericResult.IsSuccess()
                    .Should()
                    .BeFalse();
                nonGenericResult.IsFailure()
                    .Should()
                    .BeTrue();
                nonGenericResult.Errors
                    .Should()
                    .HaveCount(2)
                    .And
                    .BeEquivalentTo(errors);
            }
        }

        [Fact]
        public void AsFailure_ShouldConvertDefaultResultToNonGenericResult()
        {
            // Arrange
            Result<int> result = default;

            // Act
            Result nonGenericResult = result.AsFailure();

            // Assert
            using (new AssertionScope())
            {
                nonGenericResult.IsSuccess()
                    .Should()
                    .BeFalse();
                nonGenericResult.IsFailure()
                    .Should()
                    .BeTrue();
                nonGenericResult.Errors
                    .Should()
                    .ContainSingle()
                    .Which
                    .Should()
                    .BeEquivalentTo(EmptyError);
            }
        }

        [Fact]
        public void AsFailure_ShouldConvertResultToGenericResultWithSameErrors()
        {
            // Arrange
            List<IError> errors = new() { new Error("Error 1"), new Error("Error 2") };
            Result<int> result = Result.Failure<int>(errors);

            // Act
            Result<object> genericResult = result.AsFailure<object>();

            // Assert
            using (new AssertionScope())
            {
                genericResult.IsSuccess()
                    .Should()
                    .BeFalse();
                genericResult.IsFailure()
                    .Should()
                    .BeTrue();
                genericResult.Errors
                    .Should()
                    .HaveCount(2)
                    .And
                    .BeEquivalentTo(errors);
            }
        }

        [Fact]
        public void AsFailure_ShouldConvertDefaultResultToGenericResult()
        {
            // Arrange
            Result<int> result = default;

            // Act
            Result<object> genericResult = result.AsFailure<object>();

            // Assert
            using (new AssertionScope())
            {
                genericResult.IsSuccess()
                    .Should()
                    .BeFalse();
                genericResult.IsFailure()
                    .Should()
                    .BeTrue();
                genericResult.Errors
                    .Should()
                    .ContainSingle()
                    .Which
                    .Should()
                    .BeEquivalentTo(EmptyError);
            }
        }

        [Fact]
        public void ImplicitCast_ShouldCreateSuccessResultFromValue()
        {
            // Arrange
            const int value = 42;

            // Act
            Result<int> result = value;

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
                result.IsFailure(out _)
                    .Should()
                    .BeFalse();
                result.Errors
                    .Should()
                    .BeEmpty();
            }
        }

        [Fact]
        public void ImplicitCast_ShouldCreateFailureResultFromError()
        {
            // Arrange
            Error error = new("Sample error");

            // Act
            Result<int> result = error;

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
        public void Equals_ResultInt_ShouldReturnTrueForEqualResults()
        {
            // Arrange
            Result<int> result1 = Result.Success(42);
            Result<int> result2 = Result.Success(42);

            // Assert
            result1.Equals(result2)
                .Should()
                .BeTrue();
        }

        [Fact]
        public void Equals_ResultInt_ShouldReturnFalseForDifferentResults()
        {
            // Arrange
            Result<int> result1 = Result.Success(42);
            Result<int> result2 = Result.Success(43);

            // Assert
            result1.Equals(result2)
                .Should()
                .BeFalse();
        }

        [Fact]
        public void Equals_ResultObject_ShouldReturnTrueForEqualResults()
        {
            // Arrange
            Result<object> result1 = Result.Success<object>("test");
            Result<object> result2 = Result.Success<object>("test");

            // Assert
            result1.Equals(result2)
                .Should()
                .BeTrue();
        }

        [Fact]
        public void Equals_ResultObject_ShouldReturnFalseForDifferentResults()
        {
            // Arrange
            Result<object> result1 = Result.Success<object>("test1");
            Result<object> result2 = Result.Success<object>("test2");

            // Assert
            result1.Equals(result2)
                .Should()
                .BeFalse();
        }

        [Fact]
        public void Equals_ResultIntToObject_ShouldReturnFalseForDifferentResults()
        {
            // Arrange
            Result<int> result1 = Result.Success(42);
            Result<object> result2 = Result.Success<object>(42);

            // Assert
            result1.Equals(result2)
                .Should()
                .BeFalse();
        }

        [Fact]
        public void GetHashCode_ResultInt_ShouldReturnSameHashCodeForEqualResults()
        {
            // Arrange
            Result<int> result1 = Result.Success(42);
            Result<int> result2 = Result.Success(42);

            // Assert
            result1.GetHashCode()
                .Should()
                .Be(result2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_ResultInt_ShouldReturnDifferentHashCodeForDifferentResults()
        {
            // Arrange
            Result<int> result1 = Result.Success(42);
            Result<int> result2 = Result.Success(43);

            // Assert
            result1.GetHashCode()
                .Should()
                .NotBe(result2.GetHashCode());
        }

        [Fact]
        public void op_Equality_ResultInt_ShouldReturnTrueForEqualResults()
        {
            // Arrange
            Result<int> result1 = Result.Success(42);
            Result<int> result2 = Result.Success(42);

            // Assert
            (result1 == result2).Should()
                .BeTrue();
        }

        [Fact]
        public void op_Equality_ResultInt_ShouldReturnFalseForDifferentResults()
        {
            // Arrange
            Result<int> result1 = Result.Success(42);
            Result<int> result2 = Result.Success(43);

            // Assert
            (result1 == result2).Should()
                .BeFalse();
        }

        [Fact]
        public void op_Inequality_ResultInt_ShouldReturnFalseForEqualResults()
        {
            // Arrange
            Result<int> result1 = Result.Success(42);
            Result<int> result2 = Result.Success(42);

            // Assert
            (result1 != result2).Should()
                .BeFalse();
        }

        [Fact]
        public void op_Inequality_ResultInt_ShouldReturnTrueForDifferentResults()
        {
            // Arrange
            Result<int> result1 = Result.Success(42);
            Result<int> result2 = Result.Success(43);

            // Assert
            (result1 != result2).Should()
                .BeTrue();
        }

        [Fact]
        public void op_Equality_ResultObject_ShouldReturnTrueForEqualResults()
        {
            // Arrange
            Result<object> result1 = Result.Success<object>("test");
            Result<object> result2 = Result.Success<object>("test");

            // Assert
            (result1 == result2).Should()
                .BeTrue();
        }

        [Fact]
        public void op_Equality_ResultObject_ShouldReturnFalseForDifferentResults()
        {
            // Arrange
            Result<object> result1 = Result.Success<object>("test1");
            Result<object> result2 = Result.Success<object>("test2");

            // Assert
            (result1 == result2).Should()
                .BeFalse();
        }

        [Fact]
        public void op_Inequality_ResultObject_ShouldReturnFalseForEqualResults()
        {
            // Arrange
            Result<object> result1 = Result.Success<object>("test");
            Result<object> result2 = Result.Success<object>("test");

            // Assert
            (result1 != result2).Should()
                .BeFalse();
        }

        [Fact]
        public void op_Inequality_ResultObject_ShouldReturnTrueForDifferentResults()
        {
            // Arrange
            Result<object> result1 = Result.Success<object>("test1");
            Result<object> result2 = Result.Success<object>("test2");

            // Assert
            (result1 != result2).Should()
                .BeTrue();
        }

        [Fact]
        public void op_Equality_ResultIntToObject_ShouldReturnFalseForDifferentResults()
        {
            // Arrange
            Result<int> result1 = Result.Success(42);
            Result<object> result2 = Result.Success<object>(42);

            // Assert
            (result1 == result2).Should()
                .BeFalse();
        }

        [Fact]
        public void op_Inequality_ResultIntToObject_ShouldReturnTrueForDifferentResults()
        {
            // Arrange
            Result<int> result1 = Result.Success(42);
            Result<object> result2 = Result.Success<object>(42);

            // Assert
            (result1 != result2).Should()
                .BeTrue();
        }

        [Theory]
        [InlineData(true, "IsSuccess = True, Value = True", "")]
        [InlineData(false, "IsSuccess = False", "")]
        [InlineData(false, "IsSuccess = False, Error = \"An unknown error occured!\"", "An unknown error occured!")]
        public void ToString_ShouldReturnProperRepresentationForBoolean(bool success, string expected, string errorMessage)
        {
            // Arrange
            Result<bool> result = success ? Result.Success(true) : Result.Failure<bool>(errorMessage);

            // Assert
            result.ToString()
                .Should()
                .Be($"Result {{ {expected} }}");
        }

        [Theory]
        [InlineData(true, "IsSuccess = True, Value = 1", "")]
        [InlineData(false, "IsSuccess = False", "")]
        [InlineData(false, "IsSuccess = False, Error = \"An unknown error occured!\"", "An unknown error occured!")]
        public void ToString_ShouldReturnProperRepresentationForSByte(bool success, string expected, string errorMessage)
        {
            // Arrange
            Result<sbyte> result = success ? Result.Success<sbyte>(1) : Result.Failure<sbyte>(errorMessage);

            // Assert
            result.ToString()
                .Should()
                .Be($"Result {{ {expected} }}");
        }

        [Theory]
        [InlineData(true, "IsSuccess = True, Value = 1", "")]
        [InlineData(false, "IsSuccess = False", "")]
        [InlineData(false, "IsSuccess = False, Error = \"An unknown error occured!\"", "An unknown error occured!")]
        public void ToString_ShouldReturnProperRepresentationForByte(bool success, string expected, string errorMessage)
        {
            // Arrange
            Result<byte> result = success ? Result.Success<byte>(1) : Result.Failure<byte>(errorMessage);

            // Assert
            result.ToString()
                .Should()
                .Be($"Result {{ {expected} }}");
        }

        [Theory]
        [InlineData(true, "IsSuccess = True, Value = 1", "")]
        [InlineData(false, "IsSuccess = False", "")]
        [InlineData(false, "IsSuccess = False, Error = \"An unknown error occured!\"", "An unknown error occured!")]
        public void ToString_ShouldReturnProperRepresentationForInt16(bool success, string expected, string errorMessage)
        {
            // Arrange
            Result<short> result = success ? Result.Success<short>(1) : Result.Failure<short>(errorMessage);

            // Assert
            result.ToString()
                .Should()
                .Be($"Result {{ {expected} }}");
        }

        [Theory]
        [InlineData(true, "IsSuccess = True, Value = 1", "")]
        [InlineData(false, "IsSuccess = False", "")]
        [InlineData(false, "IsSuccess = False, Error = \"An unknown error occured!\"", "An unknown error occured!")]
        public void ToString_ShouldReturnProperRepresentationForUInt16(bool success, string expected, string errorMessage)
        {
            // Arrange
            Result<ushort> result = success ? Result.Success<ushort>(1) : Result.Failure<ushort>(errorMessage);

            // Assert
            result.ToString()
                .Should()
                .Be($"Result {{ {expected} }}");
        }

        [Theory]
        [InlineData(true, "IsSuccess = True, Value = 1", "")]
        [InlineData(false, "IsSuccess = False", "")]
        [InlineData(false, "IsSuccess = False, Error = \"An unknown error occured!\"", "An unknown error occured!")]
        public void ToString_ShouldReturnProperRepresentationForInt32(bool success, string expected, string errorMessage)
        {
            // Arrange
            Result<int> result = success ? Result.Success(1) : Result.Failure<int>(errorMessage);

            // Assert
            result.ToString()
                .Should()
                .Be($"Result {{ {expected} }}");
        }

        [Theory]
        [InlineData(true, "IsSuccess = True, Value = 1", "")]
        [InlineData(false, "IsSuccess = False", "")]
        [InlineData(false, "IsSuccess = False, Error = \"An unknown error occured!\"", "An unknown error occured!")]
        public void ToString_ShouldReturnProperRepresentationForUInt32(bool success, string expected, string errorMessage)
        {
            // Arrange
            Result<uint> result = success ? Result.Success<uint>(1) : Result.Failure<uint>(errorMessage);

            // Assert
            result.ToString()
                .Should()
                .Be($"Result {{ {expected} }}");
        }

        [Theory]
        [InlineData(true, "IsSuccess = True, Value = 1", "")]
        [InlineData(false, "IsSuccess = False", "")]
        [InlineData(false, "IsSuccess = False, Error = \"An unknown error occured!\"", "An unknown error occured!")]
        public void ToString_ShouldReturnProperRepresentationForInt64(bool success, string expected, string errorMessage)
        {
            // Arrange
            Result<long> result = success ? Result.Success<long>(1) : Result.Failure<long>(errorMessage);

            // Assert
            result.ToString()
                .Should()
                .Be($"Result {{ {expected} }}");
        }

        [Theory]
        [InlineData(true, "IsSuccess = True, Value = 1", "")]
        [InlineData(false, "IsSuccess = False", "")]
        [InlineData(false, "IsSuccess = False, Error = \"An unknown error occured!\"", "An unknown error occured!")]
        public void ToString_ShouldReturnProperRepresentationForUInt64(bool success, string expected, string errorMessage)
        {
            // Arrange
            Result<ulong> result = success ? Result.Success<ulong>(1) : Result.Failure<ulong>(errorMessage);

            // Assert
            result.ToString()
                .Should()
                .Be($"Result {{ {expected} }}");
        }

        [Theory]
        [InlineData(true, "IsSuccess = True, Value = 1.1", "")]
        [InlineData(false, "IsSuccess = False", "")]
        [InlineData(false, "IsSuccess = False, Error = \"An unknown error occured!\"", "An unknown error occured!")]
        public void ToString_ShouldReturnProperRepresentationForDecimal(bool success, string expected, string errorMessage)
        {
            // Arrange
            Result<decimal> result = success ? Result.Success(1.1m) : Result.Failure<decimal>(errorMessage);

            // Assert
            result.ToString()
                .Should()
                .Be($"Result {{ {expected} }}");
        }

        [Theory]
        [InlineData(true, "IsSuccess = True, Value = 1.1", "")]
        [InlineData(false, "IsSuccess = False", "")]
        [InlineData(false, "IsSuccess = False, Error = \"An unknown error occured!\"", "An unknown error occured!")]
        public void ToString_ShouldReturnProperRepresentationForFloat(bool success, string expected, string errorMessage)
        {
            // Arrange
            Result<float> result = success ? Result.Success(1.1f) : Result.Failure<float>(errorMessage);

            // Assert
            result.ToString()
                .Should()
                .Be($"Result {{ {expected} }}");
        }

        [Theory]
        [InlineData(true, "IsSuccess = True, Value = 1.1", "")]
        [InlineData(false, "IsSuccess = False", "")]
        [InlineData(false, "IsSuccess = False, Error = \"An unknown error occured!\"", "An unknown error occured!")]
        public void ToString_ShouldReturnProperRepresentationForDouble(bool success, string expected, string errorMessage)
        {
            // Arrange
            Result<double> result = success ? Result.Success(1.1d) : Result.Failure<double>(errorMessage);

            // Assert
            result.ToString()
                .Should()
                .Be($"Result {{ {expected} }}");
        }

        [Theory]
        [InlineData(true, "IsSuccess = True, Value = \"2024-04-05T12:30:00Z\"", "")]
        [InlineData(false, "IsSuccess = False", "")]
        [InlineData(false, "IsSuccess = False, Error = \"An unknown error occured!\"", "An unknown error occured!")]
        public void ToString_ShouldReturnProperRepresentationForDateTime(bool success, string expected, string errorMessage)
        {
            // Arrange
            Result<DateTime> result = success ? Result.Success(new DateTime(2024, 04, 05, 12, 30, 00, DateTimeKind.Utc)) : Result.Failure<DateTime>(errorMessage);

            // Assert
            result.ToString()
                .Should()
                .Be($"Result {{ {expected} }}");
        }

        [Theory]
        [InlineData(true, "IsSuccess = True, Value = \"2024-04-05T12:30:00+00:00\"", "")]
        [InlineData(false, "IsSuccess = False", "")]
        [InlineData(false, "IsSuccess = False, Error = \"An unknown error occured!\"", "An unknown error occured!")]
        public void ToString_ShouldReturnProperRepresentationForDateTimeOffset(bool success, string expected, string errorMessage)
        {
            // Arrange
            Result<DateTimeOffset> result = success ? Result.Success(new DateTimeOffset(2024, 04, 05, 12, 30, 00, TimeSpan.Zero)) : Result.Failure<DateTimeOffset>(errorMessage);

            // Assert
            result.ToString()
                .Should()
                .Be($"Result {{ {expected} }}");
        }

        [Theory]
        [InlineData(true, "IsSuccess = True, Value = 'c'", "")]
        [InlineData(false, "IsSuccess = False", "")]
        [InlineData(false, "IsSuccess = False, Error = \"An unknown error occured!\"", "An unknown error occured!")]
        public void ToString_ShouldReturnProperRepresentationForChar(bool success, string expected, string errorMessage)
        {
            // Arrange
            Result<char> result = success ? Result.Success('c') : Result.Failure<char>(errorMessage);

            // Assert
            result.ToString()
                .Should()
                .Be($"Result {{ {expected} }}");
        }

        [Theory]
        [InlineData(true, "IsSuccess = True, Value = \"StringValue\"", "")]
        [InlineData(false, "IsSuccess = False", "")]
        [InlineData(false, "IsSuccess = False, Error = \"An unknown error occured!\"", "An unknown error occured!")]
        public void ToString_ShouldReturnProperRepresentationForString(bool success, string expected, string errorMessage)
        {
            // Arrange
            Result<string> result = success ? Result.Success("StringValue") : Result.Failure<string>(errorMessage);

            // Assert
            result.ToString()
                .Should()
                .Be($"Result {{ {expected} }}");
        }

        [Theory]
        [InlineData(true, "IsSuccess = True", "")]
        [InlineData(false, "IsSuccess = False", "")]
        [InlineData(false, "IsSuccess = False, Error = \"An unknown error occured!\"", "An unknown error occured!")]
        public void ToString_ShouldReturnProperRepresentationForObject(bool success, string expected, string errorMessage)
        {
            // Arrange
            Result<object> result = success ? Result.Success(new object()) : Result.Failure<object>(errorMessage);

            // Assert
            result.ToString()
                .Should()
                .Be($"Result {{ {expected} }}");
        }

        [Theory]
        [InlineData(true, "IsSuccess = True, Value = 1", "")]
        [InlineData(false, "IsSuccess = False", "")]
        [InlineData(false, "IsSuccess = False, Error = \"An unknown error occured!\"", "An unknown error occured!")]
        public void ToString_ShouldReturnProperRepresentationForNullableValueTypes(bool success, string expected, string errorMessage)
        {
            // Arrange
            Result<int?> result = success ? Result.Success<int?>(1) : Result.Failure<int?>(errorMessage);

            // Assert
            result.ToString()
                .Should()
                .Be($"Result {{ {expected} }}");
        }

        [Fact]
        public void InterfaceSuccess_WithValue_ShouldCreateSuccessResultWithValue()
        {
            // Arrange
            const int value = 42;

            static Result<TValue> Success<TValue, TResult>(TValue value)
                where TResult : IActionableResult<TValue, Result<TValue>>
            {
                return TResult.Success(value);
            }

            // Act
            Result<int> result = Success<int, Result<int>>(value);

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
        public void InterfaceFailure_ShouldCreateFailureResultWithSingleError()
        {
            // Arrange
            static Result<TValue> Fail<TValue, TResult>()
                where TResult : IActionableResult<TValue, Result<TValue>>
            {
                return TResult.Failure();
            }

            // Act
            Result<int> result = Fail<int, Result<int>>();

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
        public void InterfaceFailure_WithErrorMessage_ShouldCreateFailureResultWithSingleError()
        {
            // Arrange
            static Result<TValue> Fail<TValue, TResult>()
                where TResult : IActionableResult<TValue, Result<TValue>>
            {
                const string errorMessage = "Sample error message";
                return TResult.Failure(errorMessage);
            }

            // Act
            Result<int> result = Fail<int, Result<int>>();

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
                    .Be("Sample error message");
            }
        }

        [Fact]
        public void InterfaceFailure_WithErrorMessageAndTupleMetadata_ShouldCreateFailureResultWithSingleError()
        {
            // Arrange
            static Result<TValue> Fail<TValue, TResult>()
                where TResult : IActionableResult<TValue, Result<TValue>>
            {
                const string errorMessage = "Sample error message";
                (string Key, object Value) metadata = ("Key", 0);
                return TResult.Failure(errorMessage, metadata);
            }

            // Act
            Result<int> result = Fail<int, Result<int>>();

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
        public void InterfaceFailure_WithErrorMessageAndKeyValuePairMetadata_ShouldCreateFailureResultWithSingleError()
        {
            // Arrange
            static Result<TValue> Fail<TValue, TResult>()
                where TResult : IActionableResult<TValue, Result<TValue>>
            {
                const string errorMessage = "Sample error message";
                KeyValuePair<string, object> metadata = new("Key", 0);
                return TResult.Failure(errorMessage, metadata);
            }

            // Act
            Result<int> result = Fail<int, Result<int>>();

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
            static Result<TValue> Fail<TValue, TResult>()
                where TResult : IActionableResult<TValue, Result<TValue>>
            {
                const string errorMessage = "Sample error message";
                IReadOnlyDictionary<string, object> metadata = new Dictionary<string, object> { { "Key", 0 } };
                return TResult.Failure(errorMessage, metadata);
            }

            // Act
            Result<int> result = Fail<int, Result<int>>();

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
            static Result<TValue> Fail<TValue, TResult>()
                where TResult : IActionableResult<TValue, Result<TValue>>
            {
                Error error = new("Sample error");
                return TResult.Failure(error);
            }

            // Act
            Result<int> result = Fail<int, Result<int>>();

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
                    .BeEquivalentTo(new Error("Sample error"));
            }
        }

        [Fact]
        public void InterfaceFailure_WithErrorsEnumerable_ShouldCreateFailureResultWithMultipleErrors()
        {
            // Arrange
            static Result<TValue> Fail<TValue, TResult>()
                where TResult : IActionableResult<TValue, Result<TValue>>
            {
                List<IError> errors = new() { new Error("Error 1"), new Error("Error 2") };
                return TResult.Failure(errors.AsEnumerable());
            }

            // Act
            Result<int> result = Fail<int, Result<int>>();

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
                    .BeEquivalentTo(new List<IError> { new Error("Error 1"), new Error("Error 2") }
                    );
            }
        }

        [Fact]
        public void InterfaceFailure_WithErrorsReadOnlyList_ShouldCreateFailureResultWithMultipleErrors()
        {
            // Arrange
            static Result<TValue> Fail<TValue, TResult>()
                where TResult : IActionableResult<TValue, Result<TValue>>
            {
                List<IError> errors = new() { new Error("Error 1"), new Error("Error 2") };
                return TResult.Failure(errors);
            }

            // Act
            Result<int> result = Fail<int, Result<int>>();

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
                    .BeEquivalentTo(new List<IError> { new Error("Error 1"), new Error("Error 2") }
                    );
            }
        }

        [Theory]
        [InlineData(true, "IsSuccess = True, Value = \"2024-04-05\"", "")]
        [InlineData(false, "IsSuccess = False", "")]
        [InlineData(false, "IsSuccess = False, Error = \"An unknown error occured!\"", "An unknown error occured!")]
        public void ToString_ShouldReturnProperRepresentationForDateOnly(bool success, string expected, string errorMessage)
        {
            // Arrange
            Result<DateOnly> result = success ? Result.Success(new DateOnly(2024, 04, 05)) : Result.Failure<DateOnly>(errorMessage);

            // Assert
            result.ToString()
                .Should()
                .Be($"Result {{ {expected} }}");
        }

        [Theory]
        [InlineData(true, "IsSuccess = True, Value = \"12:30:00\"", "")]
        [InlineData(false, "IsSuccess = False", "")]
        [InlineData(false, "IsSuccess = False, Error = \"An unknown error occured!\"", "An unknown error occured!")]
        public void ToString_ShouldReturnProperRepresentationForTimeOnly(bool success, string expected, string errorMessage)
        {
            // Arrange
            Result<TimeOnly> result = success ? Result.Success(new TimeOnly(12, 30, 00)) : Result.Failure<TimeOnly>(errorMessage);

            // Assert
            result.ToString()
                .Should()
                .Be($"Result {{ {expected} }}");
        }

        [Theory]
        [InlineData(true, "IsSuccess = True, Value = 1", "")]
        [InlineData(false, "IsSuccess = False", "")]
        [InlineData(false, "IsSuccess = False, Error = \"An unknown error occured!\"", "An unknown error occured!")]
        public void ToString_ShouldReturnProperRepresentationForInt128(bool success, string expected, string errorMessage)
        {
            // Arrange
            Result<Int128> result = success ? Result.Success<Int128>(1) : Result.Failure<Int128>(errorMessage);

            // Assert
            result.ToString()
                .Should()
                .Be($"Result {{ {expected} }}");
        }

        [Theory]
        [InlineData(true, "IsSuccess = True, Value = 1", "")]
        [InlineData(false, "IsSuccess = False", "")]
        [InlineData(false, "IsSuccess = False, Error = \"An unknown error occured!\"", "An unknown error occured!")]
        public void ToString_ShouldReturnProperRepresentationForUInt128(bool success, string expected, string errorMessage)
        {
            // Arrange
            Result<UInt128> result = success ? Result.Success<UInt128>(1) : Result.Failure<UInt128>(errorMessage);

            // Assert
            result.ToString()
                .Should()
                .Be($"Result {{ {expected} }}");
        }

        private class ValidationError(string errorMessage) : Error(errorMessage);
    }
}
