using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VolunteerHub.Models;

[Table("REPORTS")]
public partial class Report
{
    [Key]
    [Column("REPORT_ID")]
    public int ReportId { get; set; }

    [Column("VID")]
    public int Vid { get; set; }

    [Column("PROJECT_ID")]
    public int ProjectId { get; set; }

    [Column("TOTAL_HOURS", TypeName = "decimal(6, 2)")]
    public decimal? TotalHours { get; set; }

    [Column("MID")]
    public int Mid { get; set; }

    [Column("STATUS")]
    [StringLength(30)]
    [Unicode(false)]
    public string? Status { get; set; }

    [InverseProperty("Report")]
    public virtual ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();

    [ForeignKey("Mid")]
    [InverseProperty("Reports")]
    public virtual Manager MidNavigation { get; set; } = null!;

    [ForeignKey("ProjectId")]
    [InverseProperty("Reports")]
    public virtual Project Project { get; set; } = null!;

    [ForeignKey("Vid")]
    [InverseProperty("Reports")]
    public virtual Volunteer VidNavigation { get; set; } = null!;
}
