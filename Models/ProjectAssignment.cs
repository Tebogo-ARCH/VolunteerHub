using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VolunteerHub.Models;

[Table("PROJECT_ASSIGNMENTS")]
[Index("ProjectId", "Vid", Name = "UQ_PROJECT_VOLUNTEER", IsUnique = true)]
public partial class ProjectAssignment
{
    [Key]
    [Column("ASSIGN_ID")]
    public int AssignId { get; set; }

    [Column("PROJECT_ID")]
    public int ProjectId { get; set; }

    [Column("VID")]
    public int Vid { get; set; }

    [Column("SID")]
    public int Sid { get; set; }

    [Column("STATUS")]
    [StringLength(30)]
    [Unicode(false)]
    public string? Status { get; set; }

    [ForeignKey("ProjectId")]
    [InverseProperty("ProjectAssignments")]
    public virtual Project Project { get; set; } = null!;

    [ForeignKey("Sid")]
    [InverseProperty("ProjectAssignments")]
    public virtual Staff SidNavigation { get; set; } = null!;

    [ForeignKey("Vid")]
    [InverseProperty("ProjectAssignments")]
    public virtual Volunteer VidNavigation { get; set; } = null!;
}
