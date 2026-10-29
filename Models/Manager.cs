using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VolunteerHub.Models;

[Table("MANAGERS")]
[Index("Pid", Name = "UQ_MANAGERS_PID", IsUnique = true)]
public partial class Manager
{
    [Key]
    [Column("MID")]
    public int Mid { get; set; }

    [Column("PID")]
    public int Pid { get; set; }

    [InverseProperty("MidNavigation")]
    public virtual ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();

    [InverseProperty("MidNavigation")]
    public virtual ICollection<Organization> Organizations { get; set; } = new List<Organization>();

    [ForeignKey("Pid")]
    [InverseProperty("Manager")]
    public virtual Person PidNavigation { get; set; } = null!;

    [InverseProperty("MidNavigation")]
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    [InverseProperty("MidNavigation")]
    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

    [InverseProperty("MidNavigation")]
    public virtual ICollection<Staff> Staff { get; set; } = new List<Staff>();

    [InverseProperty("MidNavigation")]
    public virtual ICollection<Volunteer> Volunteers { get; set; } = new List<Volunteer>();
}
