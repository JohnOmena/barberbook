namespace BarberBook.Domain.Enums;

public enum AppointmentStatus : short
{
    Pending = 0,
    Confirmed = 1,
    CheckIn = 2,
    InService = 3,
    Done = 4,
    NoShow = 5,
    Cancelled = 6
}

