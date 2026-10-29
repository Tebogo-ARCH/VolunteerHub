using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VolunteerHub.Models;

[Table("ATTENDANCE")]
public partial class Attendance
{
    [Key]
    [Column("ATT_ID")]
    public int AttId { get; set; }

    [Column("VID")]
    public int Vid { get; set; }

    [Column("PROJECT_ID")]
    public int ProjectId { get; set; }

    [Column("ATT_DATE")]
    public DateOnly AttDate { get; set; }

    [Column("STATUS")]
    [StringLength(30)]
    [Unicode(false)]
    public string? Status { get; set; }

    [Column("HOURS_WORKED", TypeName = "decimal(5, 2)")]
    public decimal? HoursWorked { get; set; }

    [Column("VERIFIED")]
    public bool? Verified { get; set; }

    [Column("SID")]
    public int Sid { get; set; }

    [ForeignKey("ProjectId")]
    [InverseProperty("Attendances")]
    public virtual Project Project { get; set; } = null!;

    [ForeignKey("Sid")]
    [InverseProperty("Attendances")]
    public virtual Staff SidNavigation { get; set; } = null!;

    [ForeignKey("Vid")]
    [InverseProperty("Attendances")]
    public virtual Volunteer VidNavigation { get; set; } = null!;
}
