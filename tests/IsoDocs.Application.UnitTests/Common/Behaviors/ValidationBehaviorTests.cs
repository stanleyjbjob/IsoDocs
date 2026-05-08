using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using IsoDocs.Application.Common.Behaviors;
using IsoDocs.Application.Common.Messaging;
using MediatR;
using NSubstitute;

namespace IsoDocs.Application.UnitTests.Common.Behaviors;

public class ValidationBehaviorTests
{
    public sealed record SampleCommand(string Name) : ICommand<int>;

    [Fact]
    public async Task Handle_WhenNoValidators_ShouldCallNext()
    {
        // Arrange
        var behavior = new ValidationBehavior<SampleCommand, int>(Array.Empty<IValidator<SampleCommand>>());
        var nextCalled = false;

        Task<int> Next() { nextCalled = true; return Task.FromResult(42); }

        // Act
        var result = await behavior.Handle(new SampleCommand("ok"), Next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.Should().Be(42);
    }

    [Fact]
    public async Task Handle_WhenValidatorPasses_ShouldCallNext()
    {
        // Arrange
        var validator = Substitute.For<IValidator<SampleCommand>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<SampleCommand>>(), Arg.Any<CancellationToken>())
                 .Returns(new ValidationResult());

        var behavior = new ValidationBehavior<SampleCommand, int>(new[] { validator });

        Task<int> Next() => Task.FromResult(7);

        // Act
        var result = await behavior.Handle(new SampleCommand("ok"), Next, CancellationToken.None);

        // Assert
        result.Should().Be(7);
    }

    [Fact]
    public async Task Handle_WhenValidatorFails_ShouldThrowValidationException()
    {
        // Arrange
        var validator = Substitute.For<IValidator<SampleCommand>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<SampleCommand>>(), Arg.Any<CancellationToken>())
                 .Returns(new ValidationResult(new[]
                 {
                     new ValidationFailure("Name", "Name is required")
                 }));

        var behavior = new ValidationBehavior<SampleCommand, int>(new[] { validator });

        Task<int> Next() => Task.FromResult(0);

        // Act
        var act = () => behavior.Handle(new SampleCommand(""), Next, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainSingle(e => e.PropertyName == "Name" && e.ErrorMessage == "Name is required");
    }
}
