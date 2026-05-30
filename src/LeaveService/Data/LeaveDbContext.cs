using Microsoft.EntityFrameworkCore;
using LeaveService.Models;

namespace LeaveService.Data;

public class LeaveDbContext : DbContext
{
    public LeaveDbContext(DbContextOptions<LeaveDbContext> options) : base(options) { }

    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<LeaveBalance> LeaveBalances { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // One balance record per employee per year
        modelBuilder.Entity<LeaveBalance>()
            .HasIndex(lb => new { lb.EmployeeId, lb.Year })
            .IsUnique();

        // Ignore computed properties — EF should not try to store these
        modelBuilder.Entity<LeaveBalance>()
            .Ignore(lb => lb.RemainingSickLeaves)
            .Ignore(lb => lb.RemainingCasualLeaves)
            .Ignore(lb => lb.RemainingPrivilegeLeaves);
    }
}