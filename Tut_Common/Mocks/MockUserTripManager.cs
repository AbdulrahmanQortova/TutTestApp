using Tut.Common.Managers;
using Tut.Common.Models;
namespace Tut.Common.Mocks;

public class MockUserTripManager : IUserTripManager
{
    private readonly Lock _stateLock = new();
    private ConnectionState _connectionState = ConnectionState.Disconnected;
    private Trip? _currentTrip;

    // Configurable timing delays (in milliseconds)
    public int ConnectionDelayMs { get; init; } = 500;
    public int InquiryDelayMs { get; init; } = 500;
    public int StateTransitionDelayMs { get; init; } = 5000;
    public int LongStateTransitionDelayMs { get; init; } = 10000;

    // Public read-only accessors
    public ConnectionState CurrentState
    {
        get
        {
            lock (_stateLock) { return _connectionState; }
        }
    }

    public Trip? CurrentTrip
    {
        get
        {
            lock (_stateLock) { return _currentTrip; }
        }
        private set
        {
            lock (_stateLock) { _currentTrip = value; }
        }
    }
    public event EventHandler<StatusUpdateEventArgs>? StatusChanged;
    public event EventHandler<ErrorReceivedEventArgs>? ErrorReceived;
    public event EventHandler<NotificationReceivedEventArgs>? NotificationReceived;
    public event EventHandler<DriverLocationsReceivedEventArgs>? DriverLocationsReceived;
    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
    public event EventHandler<InquireResultEventArgs>? InquireResultReceived;
    public async Task Connect(CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(ConnectionDelayMs), cancellationToken);
        SetConnectionState(ConnectionState.Connected);
    }
    public async Task SendInquireTripAsync(Trip trip, CancellationToken cancellationToken = default)
    {
        if (CurrentState != ConnectionState.Connected)
        {
            ErrorReceived?.Invoke(this, new ErrorReceivedEventArgs { ErrorText = "Not connected" });
            return;
        }
        CurrentTrip = trip;
        CurrentTrip.EstimatedArrivalDuration = 190;
        CurrentTrip.EstimatedDistance = 4200;
        CurrentTrip.EstimatedTripDuration = 620;
        CurrentTrip.EstimatedCost = 12.5;
        await Task.Delay(TimeSpan.FromMilliseconds(InquiryDelayMs), cancellationToken);
        InquireResultReceived?.Invoke(this, new InquireResultEventArgs { Trip = trip });
    }
    
    public async Task SendRequestTripAsync(Trip trip, CancellationToken cancellationToken = default)
    {
        if (CurrentState != ConnectionState.Connected)
        {
            ErrorReceived?.Invoke(this, new ErrorReceivedEventArgs { ErrorText = "Not connected" });
            return;
        }
        try
        {
            await TripFeedbackSimulationLoop(cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // Cancellation is expected behavior, not an error
        }
    }
    public Task SendCancelTripAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
    public Task SendAsync(UserTripPacket packet, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
    public Task Disconnect()
    {
        SetConnectionState(ConnectionState.Disconnected);
        return Task.CompletedTask;
    }
    
    private async Task TripFeedbackSimulationLoop(CancellationToken cancellationToken)
    {
        if(CurrentTrip == null || CurrentState != ConnectionState.Connected)
            return;

        await Task.Delay(TimeSpan.FromMilliseconds(StateTransitionDelayMs), cancellationToken);
        if (cancellationToken.IsCancellationRequested)
            return;
        CurrentTrip.Status = TripState.Requested;
        StatusChanged?.Invoke(this, new StatusUpdateEventArgs { Trip =  CurrentTrip });
        if (cancellationToken.IsCancellationRequested)
            return;
        
        await Task.Delay(TimeSpan.FromMilliseconds(StateTransitionDelayMs), cancellationToken);
        if (cancellationToken.IsCancellationRequested)
            return;
        CurrentTrip.Status = TripState.Acknowledged;
        StatusChanged?.Invoke(this, new StatusUpdateEventArgs { Trip =  CurrentTrip });
        
        await Task.Delay(TimeSpan.FromMilliseconds(StateTransitionDelayMs), cancellationToken);
        CurrentTrip.Status = TripState.Accepted;
        CurrentTrip.Driver = new Driver
        {
            FirstName = "John",
            LastName = "Doe",
            Rating = 4.2
        };
        StatusChanged?.Invoke(this, new StatusUpdateEventArgs { Trip =  CurrentTrip });

        await Task.Delay(TimeSpan.FromMilliseconds(LongStateTransitionDelayMs), cancellationToken);
        if (cancellationToken.IsCancellationRequested)
            return;
        CurrentTrip.Status = TripState.DriverArrived;
        CurrentTrip.NextStop++;
        StatusChanged?.Invoke(this, new StatusUpdateEventArgs { Trip =  CurrentTrip });

        await Task.Delay(TimeSpan.FromMilliseconds(LongStateTransitionDelayMs), cancellationToken);
        if (cancellationToken.IsCancellationRequested)
            return;
        CurrentTrip.Status = TripState.Ongoing;
        StatusChanged?.Invoke(this, new StatusUpdateEventArgs { Trip =  CurrentTrip });

        for (int i = 1; i < CurrentTrip.Stops.Count - 1; i++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(LongStateTransitionDelayMs), cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;
            CurrentTrip.Status = TripState.AtStop;
            CurrentTrip.NextStop++;
            StatusChanged?.Invoke(this, new StatusUpdateEventArgs { Trip =  CurrentTrip });
        
            await Task.Delay(TimeSpan.FromMilliseconds(LongStateTransitionDelayMs), cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;
            CurrentTrip.Status = TripState.Ongoing;
            StatusChanged?.Invoke(this, new StatusUpdateEventArgs { Trip =  CurrentTrip });
        }
        await Task.Delay(TimeSpan.FromMilliseconds(LongStateTransitionDelayMs), cancellationToken);
        if (cancellationToken.IsCancellationRequested)
            return;
        CurrentTrip.Status = TripState.Arrived;
        CurrentTrip.ActualCost = 33.3;
        StatusChanged?.Invoke(this, new StatusUpdateEventArgs { Trip =  CurrentTrip });
        
        await Task.Delay(TimeSpan.FromMilliseconds(LongStateTransitionDelayMs), cancellationToken);
        if (cancellationToken.IsCancellationRequested)
            return;
        CurrentTrip.Status = TripState.Ended;
        StatusChanged?.Invoke(this, new StatusUpdateEventArgs { Trip =  CurrentTrip });
    }
    
    
    private void SetConnectionState(ConnectionState newState)
    {
        ConnectionState prev;
        bool changed = false;
        lock (_stateLock)
        {
            prev = _connectionState;
            if (prev != newState)
            {
                _connectionState = newState;
                changed = true;
            }
        }

        if (changed)
        {
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs { OldState = prev, NewState = newState });
        }
    }

}
