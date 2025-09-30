using ProtoBuf;
namespace Tut.Common.Models;

[ProtoContract]
public class Driver
{
    [ProtoMember(1)]
    public int Id { get; set; }
    [ProtoMember(2)]
    public string Mobile { get; set; } = string.Empty;
    [ProtoMember(3)]
    public string Email { get; set; } = string.Empty;
    [ProtoMember(4)]
    public string FirstName { get; set; } = string.Empty;
    [ProtoMember(5)]
    public string LastName { get; set; } = string.Empty;
    [ProtoMember(6)]
    public string NationalId { get; set; } = string.Empty;
    [ProtoMember(7)]
    public DriverState State { get; set; } = DriverState.Unspecified;
    [ProtoMember(8)]
    public GLocation Location { get; set; } = new();
    [ProtoMember(9)]
    public string Password { get; set; } = string.Empty;
    [ProtoMember(10)]
    public int TotalTrips { get; set; }
    [ProtoMember(11)]
    public double Rating { get; set; }
    [ProtoMember(12)]
    public double TotalEarnings { get; set; }
    
    public int QipUserId { get; set; }
    public string FullName {get => FirstName + " " + LastName; }
    public List<Trip>? Trips { get; set; } = [];

}


public enum DriverState
{
    Unspecified = 0,
    Offline = 1,
    Inactive = 2,
    Available = 3,
    Requested = 4,
    EnRoute = 5,
    OnTrip = 6,
    Deleted = 7
}
