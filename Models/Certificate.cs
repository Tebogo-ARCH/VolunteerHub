using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VolunteerHub.Models;

[Table("CERTIFICATES")]
public partial class Certificate
{
    [Key]
    [Column("CERT_ID")]
    public int CertId { get; set; }

    [Column("VID")]
    public int Vid { get; set; }

    [Column("PROJECT_ID")]
    public int ProjectId { get; set; }

    [Column("REPORT_ID")]
    public int ReportId { get; set; }

    [Column("ISSUED_DATE")]
    public DateOnly? IssuedDate { get; set; }

    [Column("DESCRIPTION")]
    [StringLength(500)]
    [Unicode(false)]
    public string? Description { get; set; }

    [ForeignKey("ProjectId")]
    [InverseProperty("Certificates")]
    public virtual Project Project { get; set; } = null!;

    [ForeignKey("ReportId")]
    [InverseProperty("Certificates")]
    public virtual Report Report { get; set; } = null!;

    [ForeignKey("Vid")]
    [InverseProperty("Certificates")]
    public virtual Volunteer VidNavigation { get; set; } = null!;
}
