using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using BusStop.Models;
using ReactiveUI;

namespace BusStop.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private string _statusMessage = "Симуляция готова";
    private int _deliveredPassengers;
    private string? _selectedStopToAdd;
    private ObservableCollection<string> _currentRoute = new();

    public ObservableCollection<Models.BusStop> BusStops { get; } = new();
    public ObservableCollection<Bus> AllBuses { get; } = new();
    public ObservableCollection<Bus> MovingBuses => new(AllBuses.Where(b => b.IsMoving));
    public ObservableCollection<string> AvailableStops { get; } = new();
    public ObservableCollection<string> CurrentRoute 
    { 
        get => _currentRoute; 
        set => this.RaiseAndSetIfChanged(ref _currentRoute, value); 
    }

    public string CurrentRouteDescription => CurrentRoute.Any() ? string.Join(" → ", CurrentRoute) : "Маршрут не задан";
    public bool CanAddBus => CurrentRoute.Count >= 2;

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public int DeliveredPassengers
    {
        get => _deliveredPassengers;
        set => this.RaiseAndSetIfChanged(ref _deliveredPassengers, value);
    }

    public string? SelectedStopToAdd
    {
        get => _selectedStopToAdd;
        set => this.RaiseAndSetIfChanged(ref _selectedStopToAdd, value);
    }

    private Random _rand = new Random();

    public MainWindowViewModel()
    {
        InitializeBusStops();
        GeneratePassenger();
    }
    
    private void InitializeBusStops()
    {
        var stops = new[] { "Центральная", "Северная", "Южная", "Восточная", "Западная" };
        foreach (var stop in stops)
        {
            var busStop = new Models.BusStop(stop);
            BusStops.Add(busStop);
            AvailableStops.Add(stop);
        }
    }

    public void AddStopToRoute()
    {
        if (SelectedStopToAdd == null || CurrentRoute.Contains(SelectedStopToAdd)) return;

        CurrentRoute.Add(SelectedStopToAdd);
        this.RaisePropertyChanged(nameof(CurrentRouteDescription));
        this.RaisePropertyChanged(nameof(CanAddBus));
        StatusMessage = $"Остановка {SelectedStopToAdd} добавлена в маршрут";
    }

    public void ClearRoute()
    {
        CurrentRoute.Clear();
        this.RaisePropertyChanged(nameof(CurrentRouteDescription));
        this.RaisePropertyChanged(nameof(CanAddBus));
        StatusMessage = "Маршрут очищен";
    }

    public async void AddBus()
    {
        if (CurrentRoute.Count < 2) return;

        var route = new ObservableCollection<string>(CurrentRoute);
        var bus = new Bus($"А{AllBuses.Count + 1}", 15, route);
        
        bus.PassengerBoarded += (b, p) => 
            StatusMessage = $"{p} сел в {b}";
        bus.BusFull += b => 
            StatusMessage = $"{b} заполнен и не принимает пассажиров";
        bus.PositionChanged += b => 
            this.RaisePropertyChanged(nameof(MovingBuses));

        bus.PassengerDisembarked += (b, p) => 
        {
            DeliveredPassengers++;
            StatusMessage = $"{p} вышел на {b.CurrentStop}";
        };

        bus.ArrivedAtStop += b =>
        {
            var stop = BusStops.FirstOrDefault(s => s.Name == b.CurrentStop);
            if (stop != null)
            {
                stop.AddBus(b);
                StatusMessage = $"{b} прибыл на остановку {b.CurrentStop}";
            }
            this.RaisePropertyChanged(nameof(MovingBuses));
        };

        bus.DepartedFromStop += b =>
        {
            foreach (var s in BusStops.Where(s => s.Buses.Contains(b)))
            {
                s.RemoveBus(b);
            }
            StatusMessage = $"{b} отправился с остановки {b.CurrentStop}";
            this.RaisePropertyChanged(nameof(MovingBuses));
        };

        bus.Departing += b =>
        {
            var stop = BusStops.FirstOrDefault(s => s.Name == b.CurrentStop);
            if (stop != null)
            {
                var passengersToBoard = stop.WaitingPassengers
                    .Where(p => p.Destination != b.CurrentStop && b.Route.Contains(p.Destination))
                    .Take(b.Capacity - b.Passengers.Count)
                    .ToList();

                foreach (var passenger in passengersToBoard)
                {
                    if (b.BoardPassenger(passenger))
                    {
                        stop.RemovePassenger(passenger);
                    }
                }
            }
        };

        AllBuses.Add(bus);
        
        var firstStop = BusStops.First(s => s.Name == CurrentRoute[0]);
        firstStop.AddBus(bus);
        
        StatusMessage = $"Добавлен новый автобус: {bus}";
        
        await bus.StartMoving();
    }

    private void GeneratePassenger()
    {
        if (!BusStops.Any()) return;

        var stop = BusStops[_rand.Next(BusStops.Count)];
        string destination;

        do
        {
            destination = BusStops[_rand.Next(BusStops.Count)].Name;
        } while (destination == stop.Name);

        var passenger = new Passenger($"Пассажир {new Random().Next(1000)}", destination);
        
        Dispatcher.UIThread.Post(() =>
        {
            stop.AddPassenger(passenger);
            StatusMessage = $"{passenger} ждет на {stop.Name}";
            Task.Delay(_rand.Next(2000, 5000)).ContinueWith(_ => GeneratePassenger());
        });
    }
}