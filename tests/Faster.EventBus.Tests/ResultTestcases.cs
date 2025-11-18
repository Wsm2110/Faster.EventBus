using System;
using Faster.EventBus.Core;
using Xunit;

namespace Faster.EventBus.Tests;

public class ResultTests
{
    [Fact]
    public void Success_ShouldSetIsSuccessTrue_AndEmptyError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Error);
    }

    [Fact]
    public void Failure_ShouldSetIsSuccessFalse_AndProvideErrorMessage()
    {
        var result = Result.Failure("Something went wrong");

        Assert.False(result.IsSuccess);
        Assert.Equal("Something went wrong", result.Error);
    }

    [Fact]
    public void Match_OnSuccess_ShouldExecuteSuccessDelegate()
    {
        var result = Result.Success();
        var called = false;

        result.Match(
            success: () => called = true,
            failure: _ => throw new Exception("Should not call failure")
        );

        Assert.True(called);
    }

    [Fact]
    public void Match_OnFailure_ShouldExecuteFailureDelegate()
    {
        var result = Result.Failure("Oops");
        string? receivedError = null;

        result.Match(
            success: () => throw new Exception("Should not call success"),
            failure: err => receivedError = err
        );

        Assert.Equal("Oops", receivedError);
    }

    [Fact]
    public void GenericSuccess_ShouldContainValue()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericFailure_ShouldContainErrorAndDefaultValue()
    {
        var result = Result<int>.Failure("Bad things");

        Assert.False(result.IsSuccess);
        Assert.Equal("Bad things", result.Error);
        Assert.Equal(default, result.Value);
    }

    [Fact]
    public void GenericMatch_OnSuccess_ShouldReturnValueToDelegate()
    {
        var result = Result<string>.Success("Hello");
        string? output = null;

        result.Match(
            success: v => output = v,
            failure: _ => throw new Exception("Failure should not run")
        );

        Assert.Equal("Hello", output);
    }

    [Fact]
    public void GenericMatch_OnFailure_ShouldReturnErrorToDelegate()
    {
        var result = Result<string>.Failure("Boom");
        string? receivedError = null;

        result.Match(
            success: _ => throw new Exception("Success should not run"),
            failure: err => receivedError = err
        );

        Assert.Equal("Boom", receivedError);
    }

    [Fact]
    public void ConvertGenericToNonGeneric_ShouldPreserveSuccess()
    {
        var generic = Result<string>.Success("Something");
        var converted = generic.ToResult();

        Assert.True(converted.IsSuccess);
    }

    [Fact]
    public void ConvertGenericToNonGeneric_ShouldPreserveFailure()
    {
        var generic = Result<string>.Failure("Err");
        var converted = generic.ToResult();

        Assert.False(converted.IsSuccess);
        Assert.Equal("Err", converted.Error);
    }

    [Fact]
    public void ConvertNonGenericToGeneric_ShouldCreateValueResult()
    {
        var result = Result.Success();
        var converted = result.To(123);

        Assert.True(converted.IsSuccess);
        Assert.Equal(123, converted.Value);
    }
}