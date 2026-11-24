using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Makanak.Domain.Entities;
using Makanak.Domain.Enums;

namespace Makanak.Infrastructure.Data;

/// <summary>
/// Seeds realistic development data for testing and UI alignment.
/// Runs only in Development environment when configured.
/// Documented credentials:
/// - Owner 1: owner1@makanak.com / Owner@12345
/// - Owner 2: owner2@makanak.com / Owner@12345
/// - Seeker 1: seeker1@makanak.com / Seeker@12345
/// - Seeker 2: seeker2@makanak.com / Seeker@12345
/// </summary>
public static class DevDataSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        // Check if database is already seeded
        if (await context.Properties.IgnoreQueryFilters().AnyAsync())
            return;

        // 1. Seed Users
        var owner1 = ApplicationUser.Create("owner1@makanak.com", "Ahmed El-Sayed", UserType.Owner, "+201001112233");
        var owner2 = ApplicationUser.Create("owner2@makanak.com", "Mona Mahmoud", UserType.Owner, "+201002223344");
        var seeker1 = ApplicationUser.Create("seeker1@makanak.com", "Karim Tarek", UserType.Seeker, "+201003334455");
        var seeker2 = ApplicationUser.Create("seeker2@makanak.com", "Nour Hassan", UserType.Seeker, "+201004445566");

        await userManager.CreateAsync(owner1, "Owner@12345");
        await userManager.CreateAsync(owner2, "Owner@12345");
        await userManager.CreateAsync(seeker1, "Seeker@12345");
        await userManager.CreateAsync(seeker2, "Seeker@12345");

        // 2. Seed 12 Properties
        var p1 = Property.Create(owner1.Id, "Modern Apartment in New Cairo", "Spacious luxury apartment close to AUC, premium finishes, 24/7 security", 3800000m, PropertyType.Apartment, ListingType.Sale, "New Cairo", "South 90th St, Fifth Settlement", 3, 2, 185m, 30.0150m, 31.4920m, Features.Parking | Features.Security | Features.Elevator | Features.Balcony);
        p1.AddImage("https://images.unsplash.com/photo-1545324418-cc1a3fa10c00?w=800&auto=format&fit=crop");
        p1.AddImage("https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=800&auto=format&fit=crop");

        var p2 = Property.Create(owner1.Id, "Luxury Standalone Villa in Sheikh Zayed", "Private pool, landscaped garden, smart home automation", 12800000m, PropertyType.Villa, ListingType.Sale, "Sheikh Zayed", "Beverly Hills, Gate 2", 5, 5, 450m, 30.0630m, 30.9570m, Features.Parking | Features.Security | Features.Gym | Features.AirConditioning | Features.Balcony);
        p2.AddImage("https://images.unsplash.com/photo-1580587771525-78b9dba3b914?w=800&auto=format&fit=crop");

        var p3 = Property.Create(owner1.Id, "Prime Office Space in Downtown", "Commercial office floor, high-speed fiber internet, reception desk", 45000m, PropertyType.Office, ListingType.Rent, "Downtown", "Talaat Harb St", 4, 2, 160m, 30.0470m, 31.2380m, Features.Security | Features.Elevator | Features.AirConditioning);
        p3.AddImage("https://images.unsplash.com/photo-1497366216548-37526070297c?w=800&auto=format&fit=crop");

        var p4 = Property.Create(owner1.Id, "Modern Duplex with Roof in 6th of October", "Two-level duplex with private rooftop terrace, panoramic city view", 5500000m, PropertyType.Duplex, ListingType.Sale, "6th of October", "Al Motamayez District", 4, 3, 280m, 29.9730m, 30.9320m, Features.Parking | Features.Balcony | Features.Elevator);
        p4.AddImage("https://images.unsplash.com/photo-1600585154340-be6161a56a0c?w=800&auto=format&fit=crop");

        var p5 = Property.Create(owner1.Id, "Family Townhouse in New Cairo", "Quiet compound with clubhouse, private rear garden", 8200000m, PropertyType.Townhouse, ListingType.Sale, "New Cairo", "Al Andalus, Compound Square", 4, 4, 320m, 30.0090m, 31.5030m, Features.Parking | Features.Security | Features.Gym | Features.Balcony);
        p5.AddImage("https://images.unsplash.com/photo-1568605117036-5fe5e7bab0b7?w=800&auto=format&fit=crop");

        var p6 = Property.Create(owner1.Id, "Nile View Apartment in Maadi", "Direct Nile front view, fully furnished, wooden flooring", 25000m, PropertyType.Apartment, ListingType.Rent, "Maadi", "Corniche El Nile, Sarayat Maadi", 2, 2, 140m, 29.9600m, 31.2580m, Features.Security | Features.AirConditioning | Features.Elevator | Features.Balcony);
        p6.AddImage("https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800&auto=format&fit=crop");

        var p7 = Property.Create(owner2.Id, "Penthouse in New Giza", "Open-plan penthouse with double height living room and private Jacuzzi", 9500000m, PropertyType.Apartment, ListingType.Sale, "6th of October", "New Giza, District 3", 3, 3, 260m, 30.0210m, 31.0250m, Features.Parking | Features.Security | Features.Gym | Features.Elevator | Features.Balcony);
        p7.AddImage("https://images.unsplash.com/photo-1512917774080-9991f1c4c750?w=800&auto=format&fit=crop");

        var p8 = Property.Create(owner2.Id, "Executive Villa in El Gouna", "Lagoon-front luxury villa with boat dock and private garden", 18000000m, PropertyType.Villa, ListingType.Sale, "El Gouna", "West Golf Phase 2", 5, 6, 520m, 27.3940m, 33.6760m, Features.Parking | Features.Security | Features.AirConditioning | Features.Balcony);
        p8.AddImage("https://images.unsplash.com/photo-1613977257363-707ba9348227?w=800&auto=format&fit=crop");

        var p9 = Property.Create(owner2.Id, "Commercial Office in Smart Village", "Corporate headquarters building, central AC, underground parking", 60000m, PropertyType.Office, ListingType.Rent, "Smart Village", "Alexandria Desert Road, Bldg B12", 6, 3, 350m, 30.0750m, 31.0200m, Features.Parking | Features.Security | Features.Elevator | Features.AirConditioning);
        p9.AddImage("https://images.unsplash.com/photo-1497215728101-856f4ea42174?w=800&auto=format&fit=crop");

        var p10 = Property.Create(owner2.Id, "Garden Apartment in Nasr City", "Ground floor apartment with 80 sqm private terrace, near City Stars", 2900000m, PropertyType.Apartment, ListingType.Sale, "Nasr City", "Makram Ebeid St", 3, 2, 170m, 30.0600m, 31.3400m, Features.Parking | Features.Elevator | Features.Balcony);
        p10.AddImage("https://images.unsplash.com/photo-1502005229762-ae1b465ab5c7?w=800&auto=format&fit=crop");

        var p11 = Property.Create(owner2.Id, "Cozy Duplex in Heliopolis", "Heritage building, high ceilings, owner paused offers temporarily", 4700000m, PropertyType.Duplex, ListingType.Sale, "Heliopolis", "Baghdad St, Korba", 3, 2, 210m, 30.0900m, 31.3200m, Features.Elevator | Features.Balcony);
        p11.AddImage("https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=800&auto=format&fit=crop");
        p11.ToggleStatus(PropertyStatus.Pending); // Owner paused

        var p12 = Property.Create(owner2.Id, "Luxury Apartment in Zamalek", "Sold property, transaction completed successfully", 7200000m, PropertyType.Apartment, ListingType.Sale, "Zamalek", "26th of July St", 3, 3, 220m, 30.0620m, 31.2190m, Features.Parking | Features.Security | Features.Elevator | Features.Balcony);
        p12.AddImage("https://images.unsplash.com/photo-1545324418-cc1a3fa10c00?w=800&auto=format&fit=crop");
        p12.MarkAsSold(); // Sold

        await context.Properties.AddRangeAsync(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12);
        await context.SaveChangesAsync();

        // 3. Seed Offers in each state (Pending, Countered, Accepted, Rejected) with NegotiationHistory
        // Offer 1: Pending (Round 1) on Property 1
        var o1 = Offer.Create(p1.Id, seeker1.Id, 3500000m);
        await context.Offers.AddAsync(o1);
        await context.SaveChangesAsync();
        context.NegotiationHistories.Add(NegotiationHistory.Record(o1.Id, seeker1.Id, NegotiationActionType.Submitted, 3500000m, o1.RoundNumber, DateTime.UtcNow.AddHours(-10)));

        // Offer 2: Countered (Round 2) on Property 2
        var o2 = Offer.Create(p2.Id, seeker1.Id, 11500000m);
        await context.Offers.AddAsync(o2);
        await context.SaveChangesAsync();
        context.NegotiationHistories.Add(NegotiationHistory.Record(o2.Id, seeker1.Id, NegotiationActionType.Submitted, 11500000m, 1, DateTime.UtcNow.AddHours(-8)));
        o2.Counter(12200000m);
        context.NegotiationHistories.Add(NegotiationHistory.Record(o2.Id, owner1.Id, NegotiationActionType.Countered, 12200000m, o2.RoundNumber, DateTime.UtcNow.AddHours(-4)));

        // Offer 3: Accepted on Property 12
        var o3 = Offer.Create(p12.Id, seeker2.Id, 7000000m);
        await context.Offers.AddAsync(o3);
        await context.SaveChangesAsync();
        context.NegotiationHistories.Add(NegotiationHistory.Record(o3.Id, seeker2.Id, NegotiationActionType.Submitted, 7000000m, 1, DateTime.UtcNow.AddDays(-2)));
        o3.Accept();
        context.NegotiationHistories.Add(NegotiationHistory.Record(o3.Id, owner2.Id, NegotiationActionType.Accepted, 7000000m, o3.RoundNumber, DateTime.UtcNow.AddDays(-1)));

        // Offer 4: Rejected on Property 3
        var o4 = Offer.Create(p3.Id, seeker2.Id, 30000m);
        await context.Offers.AddAsync(o4);
        await context.SaveChangesAsync();
        context.NegotiationHistories.Add(NegotiationHistory.Record(o4.Id, seeker2.Id, NegotiationActionType.Submitted, 30000m, 1, DateTime.UtcNow.AddHours(-12)));
        o4.Reject();
        context.NegotiationHistories.Add(NegotiationHistory.Record(o4.Id, owner1.Id, NegotiationActionType.Rejected, 30000m, o4.RoundNumber, DateTime.UtcNow.AddHours(-6)));

        await context.SaveChangesAsync();
    }
}
