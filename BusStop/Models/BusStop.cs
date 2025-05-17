using System;
using System.Collections.ObjectModel;

namespace BusStop.Models;

public class BusStop
{
    public string Name { get; }
    public ObservableCollection<IPassenger> WaitingPassengers { get; } = new();
    public ObservableCollection<Bus> Buses { get; } = new();

    public event Action<BusStop, Bus>? BusArrived;
    
    public BusStop(string name)
    {
        Name = name;
    }

    public void AddBus(Bus bus)
    {
        if (!Buses.Contains(bus))
        {
            Buses.Add(bus);
            BusArrived?.Invoke(this, bus);
        }
    }

    public void RemoveBus(Bus bus)
    {
        Buses.Remove(bus);
    }

    public void AddPassenger(IPassenger passenger)
    {
        WaitingPassengers.Add(passenger);
    }

    public void RemovePassenger(IPassenger passenger)
    {
        WaitingPassengers.Remove(passenger);
    }

    public override string ToString() => $"Остановка: {Name}";
}