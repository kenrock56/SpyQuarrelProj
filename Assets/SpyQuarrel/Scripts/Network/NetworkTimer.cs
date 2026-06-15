using System;
using Unity.Netcode;

namespace SpyQuarrelRuntime
{
    public class NetworkTimer : IDisposable
    {
        public int CurrentTick => _networkManager.NetworkTickSystem.LocalTime.Tick;

        public float TickRate { get; }
        public float FixedTickInterval => 1f / TickRate;

        public event Action OnTick;

        private readonly NetworkManager _networkManager;
        private readonly KinematicSimulationBridge _simulationBridge;

        public NetworkTimer(NetworkManager networkManager)
        {
            _networkManager = networkManager;
            _simulationBridge = KinematicSimulationBridge.Instance;

            TickRate = _networkManager.NetworkTickSystem.TickRate;

            _networkManager.NetworkTickSystem.Tick += HandleTick;
        }

        private void HandleTick()
        {
            float deltaTime = FixedTickInterval;

            
            _simulationBridge.BeginTickSimulation(deltaTime);
            OnTick?.Invoke();
            _simulationBridge.EndTickSimulation(deltaTime);
        }

        public void Dispose()
        {
            if (_networkManager != null &&
                _networkManager.NetworkTickSystem != null)
            {
                _networkManager.NetworkTickSystem.Tick -= HandleTick;
            }
        }
    }
}