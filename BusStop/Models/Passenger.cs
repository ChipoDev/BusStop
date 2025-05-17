namespace BusStop.Models;

public class Passenger : IPassenger
{
    public string Name { get; }
    public string Destination { get; }

    public Passenger(string name, string destination)
    {
        Name = name;
        Destination = destination;
    }

    public override string ToString() => $"{Name} (to {Destination})";
}