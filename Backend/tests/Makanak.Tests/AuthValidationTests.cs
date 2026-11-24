using FluentValidation.TestHelper;
using Makanak.Application.DTOs.Auth;
using Makanak.Application.Validators;
using Makanak.Domain.Entities;
using Makanak.Domain.Enums;
using Xunit;

namespace Makanak.Tests;

public class AuthValidationTests
{
    private readonly RegisterRequestValidator _registerValidator = new();
    private readonly LoginRequestValidator _loginValidator = new();
    private readonly UpdateUserProfileValidator _updateProfileValidator = new();

    [Fact]
    public void Register_WithValidOwnerData_ShouldPassValidation()
    {
        var request = new RegisterRequestDto("Ahmed Ali", "ahmed@example.com", "Password123!", UserType.Owner, "+201001234567");
        var result = _registerValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Register_WithValidSeekerData_ShouldPassValidation()
    {
        var request = new RegisterRequestDto("Sara Mohamed", "sara@example.com", "Password123!", UserType.Seeker);
        var result = _registerValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Register_WithAdminUserType_ShouldFailValidation()
    {
        var request = new RegisterRequestDto("Hacker", "hacker@example.com", "Password123!", UserType.Admin);
        var result = _registerValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.UserType)
              .WithErrorMessage("User type must be either Owner or Seeker. Admin registration is prohibited.");
    }

    [Fact]
    public void Register_WithEmptyFullName_ShouldFailValidation()
    {
        var request = new RegisterRequestDto("", "test@example.com", "Password123!", UserType.Seeker);
        var result = _registerValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public void Register_WithFullNameExceeding100Chars_ShouldFailValidation()
    {
        var longName = new string('a', 101);
        var request = new RegisterRequestDto(longName, "test@example.com", "Password123!", UserType.Seeker);
        var result = _registerValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public void Register_WithInvalidEmail_ShouldFailValidation()
    {
        var request = new RegisterRequestDto("Test User", "not-an-email", "Password123!", UserType.Seeker);
        var result = _registerValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Register_WithShortPassword_ShouldFailValidation()
    {
        var request = new RegisterRequestDto("Test User", "test@example.com", "123", UserType.Seeker);
        var result = _registerValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Login_WithValidData_ShouldPassValidation()
    {
        var request = new LoginRequestDto("test@example.com", "Secret123!");
        var result = _loginValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Login_WithInvalidEmail_ShouldFailValidation()
    {
        var request = new LoginRequestDto("invalid", "Secret123!");
        var result = _loginValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void UpdateProfile_WithValidData_ShouldPass()
    {
        var request = new UpdateUserProfileRequestDto("New Name", "+201112223334");
        var result = _updateProfileValidator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ApplicationUser_Create_ShouldInitializePropertiesCorrectly()
    {
        var user = ApplicationUser.Create("user@test.com", "User Name", UserType.Owner, "+201011122233");

        Assert.Equal("user@test.com", user.Email);
        Assert.Equal("USER@TEST.COM", user.NormalizedEmail);
        Assert.Equal("User Name", user.FullName);
        Assert.Equal(UserType.Owner, user.UserType);
        Assert.Equal("+201011122233", user.PhoneNumber);
        Assert.NotEqual(Guid.Empty, user.PublicId);
    }

    [Fact]
    public void ApplicationUser_UpdateProfile_ShouldUpdateFullNameAndPhone()
    {
        var user = ApplicationUser.Create("user@test.com", "Old Name", UserType.Seeker);
        user.UpdateProfile("Updated Name", "+201000000000");

        Assert.Equal("Updated Name", user.FullName);
        Assert.Equal("+201000000000", user.PhoneNumber);
    }
}
