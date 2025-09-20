using ProtoBuf;
namespace Tut.Common.Models;

[ProtoContract]
public class User
{
    [ProtoMember(1)]
    public int Id { get; set; }
    [ProtoMember(2)]
    public string Mobile { get; set; } = string.Empty;
    [ProtoMember(3)]
    public string FirstName { get; set; } = string.Empty;
    [ProtoMember(4)]
    public string LastName { get; set; } = string.Empty;
    [ProtoMember(5)]
    public string Email { get; set; } = string.Empty;

    [ProtoMember(6)]
    public UserState Status { get; set; } = UserState.Unspecified;
    [ProtoMember(7)]
    public string Password { get; set; } = string.Empty;
    [ProtoMember(8)]
    public int TotalTrips { get; set; }
    [ProtoMember(9)]
    public double Rating { get; set; }
    [ProtoMember(10)]
    public double TotalSpending { get; set; }
    
    [ProtoMember(11)]
    public List<SavedPlace>? SavedPlaces { get; set; }
    [ProtoMember(12)]
    public List<Trip>? Trips { get; set; }

    public int QipUserId { get; set; }
}

public enum UserState
{
    Unspecified = 0,
    Active = 1,
    OnTrip = 2,
    Blocked = 3,
    Deleted = 4
}
