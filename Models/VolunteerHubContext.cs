using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace VolunteerHub.Models;

public partial class VolunteerHubContext : DbContext
{
    public VolunteerHubContext()
    {
    }

    public VolunteerHubContext(DbContextOptions<VolunteerHubContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<Announcement> Announcements { get; set; }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<Certificate> Certificates { get; set; }

    public virtual DbSet<Manager> Managers { get; set; }

    public virtual DbSet<Organization> Organizations { get; set; }

    public virtual DbSet<Person> Persons { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectAssignment> ProjectAssignments { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<Skill> Skills { get; set; }

    public virtual DbSet<Staff> Staff { get; set; }

    public virtual DbSet<Volunteer> Volunteers { get; set; }

    public virtual DbSet<VolunteerSkill> VolunteerSkills { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS02;Database=VolunteerHub;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.AddressId).HasName("PK__ADDRESS__66EDEA2F876F2DD8");
        });

        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.HasKey(e => e.AnnId).HasName("PK__ANNOUNCE__50BF531C939426D2");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.MidNavigation).WithMany(p => p.Announcements)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ANNOUNCEMENTS_MANAGERS");

            entity.HasOne(d => d.OidNavigation).WithMany(p => p.Announcements)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ANNOUNCEMENTS_ORGANIZATIONS");
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttId).HasName("PK__ATTENDAN__236A8ABCAE091F9A");

            entity.Property(e => e.HoursWorked).HasDefaultValue(0m);
            entity.Property(e => e.Status).HasDefaultValue("PRESENT");
            entity.Property(e => e.Verified).HasDefaultValue(false);

            entity.HasOne(d => d.Project).WithMany(p => p.Attendances)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ATTENDANCE_PROJECTS");

            entity.HasOne(d => d.SidNavigation).WithMany(p => p.Attendances)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ATTENDANCE_STAFF");

            entity.HasOne(d => d.VidNavigation).WithMany(p => p.Attendances)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ATTENDANCE_VOLUNTEERS");
        });

        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.HasKey(e => e.CertId).HasName("PK__CERTIFIC__D9BDF203F059E48D");

            entity.Property(e => e.IssuedDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Project).WithMany(p => p.Certificates)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CERTIFICATES_PROJECTS");

            entity.HasOne(d => d.Report).WithMany(p => p.Certificates)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CERTIFICATES_REPORTS");

            entity.HasOne(d => d.VidNavigation).WithMany(p => p.Certificates)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CERTIFICATES_VOLUNTEERS");
        });

        modelBuilder.Entity<Manager>(entity =>
        {
            entity.HasKey(e => e.Mid).HasName("PK__MANAGERS__C797348A94FA3856");

            entity.HasOne(d => d.PidNavigation).WithOne(p => p.Manager)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MANAGERS_PERSONS");
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.Oid).HasName("PK__ORGANIZA__CB394B3985A8A697");

            entity.HasOne(d => d.Address).WithMany(p => p.Organizations).HasConstraintName("FK_ORGANIZATIONS_ADDRESS");

            entity.HasOne(d => d.MidNavigation).WithMany(p => p.Organizations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ORGANIZATIONS_MANAGERS");
        });

        modelBuilder.Entity<Person>(entity =>
        {
            entity.HasKey(e => e.Pid).HasName("PK__PERSONS__C577552082BAB4A4");

            entity.Property(e => e.JoinedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status).HasDefaultValue("ACTIVE");

            entity.HasOne(d => d.Address).WithMany(p => p.People).HasConstraintName("FK_PERSONS_ADDRESS");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.ProjectId).HasName("PK__PROJECTS__8398A78606DCEE28");

            entity.Property(e => e.Status).HasDefaultValue("PLANNED");

            entity.HasOne(d => d.MidNavigation).WithMany(p => p.Projects)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PROJECTS_MANAGERS");
        });

        modelBuilder.Entity<ProjectAssignment>(entity =>
        {
            entity.HasKey(e => e.AssignId).HasName("PK__PROJECT___CAABF0651AFBB712");

            entity.Property(e => e.Status).HasDefaultValue("ASSIGNED");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectAssignments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ASSIGNMENTS_PROJECTS");

            entity.HasOne(d => d.SidNavigation).WithMany(p => p.ProjectAssignments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ASSIGNMENTS_STAFF");

            entity.HasOne(d => d.VidNavigation).WithMany(p => p.ProjectAssignments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ASSIGNMENTS_VOLUNTEERS");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PK__REPORTS__B106F6BB056791E4");

            entity.Property(e => e.Status).HasDefaultValue("SUBMITTED");
            entity.Property(e => e.TotalHours).HasDefaultValue(0m);

            entity.HasOne(d => d.MidNavigation).WithMany(p => p.Reports)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_REPORTS_MANAGERS");

            entity.HasOne(d => d.Project).WithMany(p => p.Reports)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_REPORTS_PROJECTS");

            entity.HasOne(d => d.VidNavigation).WithMany(p => p.Reports)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_REPORTS_VOLUNTEERS");
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.SkillId).HasName("PK__SKILLS__DE41A5691648A8BB");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasKey(e => e.Sid).HasName("PK__STAFF__CA195970684BC632");

            entity.HasOne(d => d.MidNavigation).WithMany(p => p.Staff)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_STAFF_MANAGERS");

            entity.HasOne(d => d.PidNavigation).WithOne(p => p.Staff)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_STAFF_PERSONS");
        });

        modelBuilder.Entity<Volunteer>(entity =>
        {
            entity.HasKey(e => e.Vid).HasName("PK__VOLUNTEE__C5DF22BB687DF87C");

            entity.HasOne(d => d.MidNavigation).WithMany(p => p.Volunteers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VOLUNTEERS_MANAGERS");

            entity.HasOne(d => d.PidNavigation).WithOne(p => p.Volunteer)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VOLUNTEERS_PERSONS");
        });

        modelBuilder.Entity<VolunteerSkill>(entity =>
        {
            entity.HasKey(e => e.Vsid).HasName("PK__VOLUNTEE__421D6462ACACED31");

            entity.HasOne(d => d.Skill).WithMany(p => p.VolunteerSkills)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VOLUNTEER_SKILLS_SKILLS");

            entity.HasOne(d => d.VidNavigation).WithMany(p => p.VolunteerSkills)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VOLUNTEER_SKILLS_VOLUNTEERS");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
