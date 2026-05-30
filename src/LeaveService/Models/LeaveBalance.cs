namespace LeaveService.Models;

public class LeaveBalance
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int Year { get; set; }

    // Allocations
    public int TotalSickLeaves { get; set; } = 10;
    public int TotalCasualLeaves { get; set; } = 12;
    public int TotalPrivilegeLeaves { get; set; } = 15;

    // Used
    public int UsedSickLeaves { get; set; } = 0;
    public int UsedCasualLeaves { get; set; } = 0;
    public int UsedPrivilegeLeaves { get; set; } = 0;

    // Computed — not stored in DB
    public int RemainingSickLeaves => TotalSickLeaves - UsedSickLeaves;
    public int RemainingCasualLeaves => TotalCasualLeaves - UsedCasualLeaves;
    public int RemainingPrivilegeLeaves => TotalPrivilegeLeaves - UsedPrivilegeLeaves;
}