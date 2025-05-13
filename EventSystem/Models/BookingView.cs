using System;
using System.Collections.Generic;

namespace EventSystem.Models;

public partial class BookingView
{
    public int BookingId { get; set; }

    public string VenueName { get; set; } = null!;

    public string Location { get; set; } = null!;

    public string EventName { get; set; } = null!;

    public DateOnly EventDate { get; set; }

    public DateOnly BookingDate { get; set; }
}
