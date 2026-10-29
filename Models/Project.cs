using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VolunteerHub.Models;

[Table("PROJECTS")]
public partial class Project
{
    [Key]
    [Column("PROJECT_ID")]
    public int ProjectId { get; set; }

    [Column("PROJECT_NAME")]
    [StringLength(150)]
    [Unicode(false)]
    public string ProjectName { get; set; } = null!;

    [Column("DESCRIPTION")]
    [StringLength(500)]
    [Unicode(false)]
    public string? Description { get; set; }

    [Column("START_DATE")]
    public DateOnly StartDate { get; set; }

    [Column("END_DATE")]
    public DateOnly? EndDate { get; set; }

    [Column("STATUS")]
    [StringLength(30)]
    [Unicode(false)]
    public string? Status { get; set; }

    [Column("MID")]
    public int Mid { get; set; }

    [Column("LOCATION")]
    [StringLength(200)]
    [Unicode(false)]
    public string? Location { get; set; }

    [InverseProperty("Project")]
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    [InverseProperty("Project")]
    public virtual ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();

    [ForeignKey("Mid")]
    [InverseProperty("Projects")]
    public virtual Manager MidNavigation { get; set; } = null!;

    [InverseProperty("Project")]
    public virtual ICollection<ProjectAssignment> ProjectAssignments { get; set; } = new List<ProjectAssignment>();

    [InverseProperty("Project")]
    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
}
