using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BusStop.Models;

public class Bus : IDisposable
{
    public string Number { get; }
    public int Capacity { get; }
    public ObservableCollection<string> Route { get; }
    public int CurrentStopIndex { get; private set; }
    public string CurrentStop => Route[CurrentStopIndex];
    public ObservableCollection<IPassenger> Passengers { get; } = new();
    public bool IsMoving { get; private set; }
    public double ProgressToNextStop { get; private set; }
    public bool IsForwardDirection { get; private set; } = true;
    public string RouteDescription => string.Join(" → ", Route);
    public string DirectionSymbol => IsForwardDirection ? "→" : "←";

    public event Action<Bus>? BusFull;
    public event Action<Bus, IPassenger>? PassengerBoarded;
    public event Action<Bus, IPassenger>? PassengerDisembarked;
    public event Action<Bus>? BusRemoved;
    public event Action<Bus>? PositionChanged;
    public event Action<Bus>? DepartedFromStop;
    public event Action<Bus>? ArrivedAtStop;
    public event Action<Bus>? Departing;

    private CancellationTokenSource? _cts;
    private bool _isAtStop = true;

    public Bus(string number, int capacity, ObservableCollection<string> route, int startIndex = 0)
    {
        Number = number;
        Capacity = capacity;
        Route = route;
        CurrentStopIndex = startIndex;
    }

    public async Task StartMoving()
    {
        if (IsMoving || Route.Count < 2) return;

        IsMoving = true;
        _cts = new CancellationTokenSource();

        // Первая посадка пассажиров на начальной остановке
        _isAtStop = true;
        ArrivedAtStop?.Invoke(this);
        await Task.Delay(5000, _cts.Token);
        Departing?.Invoke(this);
        _isAtStop = false;
        DepartedFromStop?.Invoke(this);

        while (IsMoving && !_cts.Token.IsCancellationRequested)
        {
            // Движение между остановками
            ProgressToNextStop = 0;
            for (int i = 0; i < 100; i++)
            {
                if (_cts.Token.IsCancellationRequested)
                    return;

                await Task.Delay(50, _cts.Token);
                ProgressToNextStop = i / 100.0;
                PositionChanged?.Invoke(this);
            }

            // Определяем следующую остановку
            if (IsForwardDirection)
            {
                CurrentStopIndex++;
                if (CurrentStopIndex >= Route.Count)
                {
                    // Достигли конца маршрута - начинаем движение в обратном направлении
                    IsForwardDirection = false;
                    CurrentStopIndex = Route.Count - 2;
                }
            }
            else
            {
                CurrentStopIndex--;
                if (CurrentStopIndex < 0)
                {
                    // Достигли начала маршрута - начинаем движение в прямом направлении
                    IsForwardDirection = true;
                    CurrentStopIndex = 1;
                }
            }

            // Прибытие на остановку
            ProgressToNextStop = 0;
            _isAtStop = true;
            ArrivedAtStop?.Invoke(this);
            PositionChanged?.Invoke(this);

            // Высадка пассажиров
            var passengersToDisembark = Passengers
                .Where(p => p.Destination == CurrentStop)
                .ToList();

            foreach (var passenger in passengersToDisembark)
            {
                Passengers.Remove(passenger);
                PassengerDisembarked?.Invoke(this, passenger);
            }

            // Ожидание на остановке
            await Task.Delay(4000, _cts.Token);
            
            // Посадка перед отправкой
            Departing?.Invoke(this);
            await Task.Delay(1000, _cts.Token);
            
            _isAtStop = false;
            DepartedFromStop?.Invoke(this);
        }
    }

    public void StopMoving()
    {
        IsMoving = false;
        _cts?.Cancel();
        ProgressToNextStop = 0;
        PositionChanged?.Invoke(this);
    }

    public bool CanBeRemoved() => _isAtStop;

    public bool BoardPassenger(IPassenger passenger)
    {
        if (Passengers.Count >= Capacity || !_isAtStop)
            return false;

        Passengers.Add(passenger);
        PassengerBoarded?.Invoke(this, passenger);

        if (Passengers.Count >= Capacity)
            BusFull?.Invoke(this);

        return true;
    }

    public void RemoveFromRoute()
    {
        if (!CanBeRemoved()) return;

        StopMoving();
        BusRemoved?.Invoke(this);
    }

    public void Dispose()
    {
        StopMoving();
        _cts?.Dispose();
    }

    public override string ToString() => 
        $"Автобус {Number} ({RouteDescription})";
}