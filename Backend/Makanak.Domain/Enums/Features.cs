using System;

namespace Makanak.Domain.Enums;

[Flags]
public enum Features
{
    None = 0,
    Parking = 1,
    Security = 2,
    Balcony = 4,
    Elevator = 8,
    AirConditioning = 16,
    Gym = 32
}
