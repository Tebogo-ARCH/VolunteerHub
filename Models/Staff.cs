using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VolunteerHub.Models;

[Table("STAFF")]
[Index("Pid", Name = "UQ_STAFF_PID", IsUnique = true)]
public partial class Staff
{
    [Key]
    [Column("SID")]
    public int Sid { get; set; }

    [Column("PID")]
    public int Pid { get; set; }

    [Column("MID")]
    public int Mid { get; set; }

    [InverseProperty("SidNavigation")]
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    [ForeignKey("Mid")]
    [InverseProperty("Staff")]
    public virtual Manager MidNavigation { get; set; } = null!;

    [ForeignKey("Pid")]
    [InverseProperty("Staff")]
    public virtual Person PidNavigation { get; set; } = null!;

    [InverseProperty("SidNavigation")]
    public virtual ICollection<ProjectAssignment> ProjectAssignments { get; set; } = new List<ProjectAssignment>();
}
