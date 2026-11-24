using Makanak.Application.DTOs.Offers;
using Makanak.Application.Validators;
using Makanak.Domain.Entities;
using Makanak.Domain.Enums;
using Makanak.Domain.Exceptions;
using Xunit;

namespace Makanak.Tests;

public class OfferDomainTests
{
    [Fact]
    public void Offer_Create_ShouldInitializeWithRoundOneAndPendingStatus()
    {
        var offer = Offer.Create(propertyId: 1, seekerId: 2, initialPrice: 1500000m);

        Assert.Equal(1, offer.PropertyId);
        Assert.Equal(2, offer.SeekerId);
        Assert.Equal(1500000m, offer.InitialPrice);
        Assert.Null(offer.CounterPrice);
        Assert.Equal(1, offer.RoundNumber);
        Assert.Equal(OfferStatus.Pending, offer.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public void Offer_Create_WithInvalidInitialPrice_ShouldThrow(decimal invalidPrice)
    {
        Assert.Throws<InvalidOfferOperationException>(() =>
            Offer.Create(1, 2, invalidPrice));
    }

    [Fact]
    public void Offer_Counter_ShouldAdvanceToRoundTwoAndSetCounteredStatus()
    {
        var offer = Offer.Create(1, 2, 1000m);
        offer.Counter(1200m);

        Assert.Equal(2, offer.RoundNumber);
        Assert.Equal(1200m, offer.CounterPrice);
        Assert.Equal(OfferStatus.Countered, offer.Status);
    }

    [Fact]
    public void Offer_Counter_ExceedingTwoRounds_ShouldThrowNegotiationLimitExceeded()
    {
        var offer = Offer.Create(1, 2, 1000m);
        offer.Counter(1200m); // Round 2 reached

        Assert.Throws<NegotiationLimitExceededException>(() =>
            offer.Counter(1300m)); // Attempting Round 3
    }

    [Fact]
    public void Offer_Accept_ShouldTransitionToAccepted()
    {
        var offer = Offer.Create(1, 2, 1000m);
        offer.Accept();

        Assert.Equal(OfferStatus.Accepted, offer.Status);
    }

    [Fact]
    public void Offer_Reject_ShouldTransitionToRejected()
    {
        var offer = Offer.Create(1, 2, 1000m);
        offer.Reject();

        Assert.Equal(OfferStatus.Rejected, offer.Status);
    }

    [Fact]
    public void Offer_Reject_WhenAlreadyAccepted_ShouldThrow()
    {
        var offer = Offer.Create(1, 2, 1000m);
        offer.Accept();

        Assert.Throws<InvalidOfferOperationException>(() => offer.Reject());
    }

    [Fact]
    public void GetAllowedActions_WhenPending_OwnerGetsAcceptCounterReject()
    {
        var offer = Offer.Create(1, 2, 1000m);
        var actions = offer.GetAllowedActions("Owner", PropertyStatus.Available);

        Assert.Contains("accept", actions);
        Assert.Contains("counter", actions);
        Assert.Contains("reject", actions);
        Assert.Equal(3, actions.Count);
    }

    [Fact]
    public void GetAllowedActions_WhenPending_SeekerGetsOnlyReject()
    {
        var offer = Offer.Create(1, 2, 1000m);
        var actions = offer.GetAllowedActions("Seeker", PropertyStatus.Available);

        Assert.Single(actions);
        Assert.Contains("reject", actions);
    }

    [Fact]
    public void GetAllowedActions_WhenCountered_SeekerGetsAcceptAndReject()
    {
        var offer = Offer.Create(1, 2, 1000m);
        offer.Counter(1200m);

        var actions = offer.GetAllowedActions("Seeker", PropertyStatus.Available);
        Assert.Contains("accept", actions);
        Assert.Contains("reject", actions);
        Assert.Equal(2, actions.Count);
    }

    [Fact]
    public void GetAllowedActions_WhenCountered_OwnerGetsNoActions()
    {
        var offer = Offer.Create(1, 2, 1000m);
        offer.Counter(1200m);

        var actions = offer.GetAllowedActions("Owner", PropertyStatus.Available);
        Assert.Empty(actions);
    }

    [Fact]
    public void GetAllowedActions_WhenAcceptedOrRejected_NoActionsAllowed()
    {
        var offer = Offer.Create(1, 2, 1000m);
        offer.Accept();

        Assert.Empty(offer.GetAllowedActions("Owner", PropertyStatus.Available));
        Assert.Empty(offer.GetAllowedActions("Seeker", PropertyStatus.Available));

        var offer2 = Offer.Create(1, 2, 1000m);
        offer2.Reject();

        Assert.Empty(offer2.GetAllowedActions("Owner", PropertyStatus.Available));
        Assert.Empty(offer2.GetAllowedActions("Seeker", PropertyStatus.Available));
    }

    [Fact]
    public void GetAllowedActions_WhenPropertySoldOrDeleted_NoActionsAllowed()
    {
        var offer = Offer.Create(1, 2, 1000m);

        // Sold property
        Assert.Empty(offer.GetAllowedActions("Owner", PropertyStatus.Sold));
        Assert.Empty(offer.GetAllowedActions("Seeker", PropertyStatus.Sold));

        // Soft-deleted property
        Assert.Empty(offer.GetAllowedActions("Owner", PropertyStatus.Available, isPropertyDeleted: true));
        Assert.Empty(offer.GetAllowedActions("Seeker", PropertyStatus.Available, isPropertyDeleted: true));
    }

    [Fact]
    public void CreateOfferValidator_WithValidData_ShouldPass()
    {
        var validator = new CreateOfferRequestValidator();
        var request = new CreateOfferRequestDto(Guid.NewGuid(), 2500000m);
        var result = validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateOfferValidator_WithZeroPrice_ShouldFail()
    {
        var validator = new CreateOfferRequestValidator();
        var request = new CreateOfferRequestDto(Guid.NewGuid(), 0m);
        var result = validator.Validate(request);
        Assert.False(result.IsValid);
    }
}
