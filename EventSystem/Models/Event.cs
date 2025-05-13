using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventSystem.Models;

public partial class Event
{
    public int EventId { get; set; }

    [Required(ErrorMessage = "Event name is required.")]
    [StringLength(100, ErrorMessage = "Event name cannot exceed 100 characters.")]
    public string EventName { get; set; } = null!;

    [Required(ErrorMessage = "Event date is required.")]
    public DateOnly EventDate { get; set; }

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string Description { get; set; } = null!;

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}