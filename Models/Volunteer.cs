using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VolunteerHub.Models;

[Table("VOLUNTEERS")]
[Index("Pid", Name = "UQ_VOLUNTEERS_PID", IsUnique = true)]
public partial class Volunteer
{
    [Key]
    [Column("VID")]
    public int Vid { get; set; }

    [Column("PID")]
    public int Pid { get; set; }

    [Column("MID")]
    public int Mid { get; set; }

    [InverseProperty("VidNavigation")]
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    [InverseProperty("VidNavigation")]
    public virtual ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();

    [ForeignKey("Mid")]
    [InverseProperty("Volunteers")]
    public virtual Manager MidNavigation { get; set; } = null!;

    [ForeignKey("Pid")]
    [InverseProperty("Volunteer")]
    public virtual Person PidNavigation { get; set; } = null!;

    [InverseProperty("VidNavigation")]
    public virtual ICollection<ProjectAssignment> ProjectAssignments { get; set; } = new List<ProjectAssignment>();

    [InverseProperty("VidNavigation")]
    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

    [InverseProperty("VidNavigation")]
    public virtual ICollection<VolunteerSkill> VolunteerSkills { get; set; } = new List<VolunteerSkill>();
}
