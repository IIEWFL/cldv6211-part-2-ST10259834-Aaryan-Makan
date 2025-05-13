using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventSystem.Models;

public partial class Booking
{
    public int BookingId { get; set; }

    [Required(ErrorMessage = "Venue is required.")]
    public int? VenueId { get; set; }

    [Required(ErrorMessage = "Event is required.")]
    public int? EventId { get; set; }

    [Required(ErrorMessage = "Booking date is required.")]
    public DateOnly BookingDate { get; set; }

    public virtual Event? Event { get; set; }

    public virtual Venue? Venue { get; set; }
}