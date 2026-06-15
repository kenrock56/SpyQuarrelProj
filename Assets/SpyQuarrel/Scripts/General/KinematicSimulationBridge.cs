using System.Collections.Generic;
using AutoSingleton;
using KinematicCharacterController;
using UnityEngine;

namespace SpyQuarrelRuntime
{
    [Singleton]
    public class KinematicSimulationBridge : MonoBehaviour
    {
        public static KinematicSimulationBridge Instance =>
            _instance ??= Singleton<KinematicSimulationBridge>.Instance;

        private static KinematicSimulationBridge _instance;

        public static bool HasInstance => _instance != null;

     
        private bool _simulating;

        private readonly List<KinematicCharacterMotor> _motors = new();
        private readonly List<PhysicsMover> _movers = new();
        
        private readonly List<KinematicCharacterMotor> _singleMotorCollection = new(1);
        private readonly List<KinematicCharacterMotor> _filteredScratch = new();
        private readonly List<PhysicsMover> _noMovers = new(0);

        private void Awake()
        {
            KinematicCharacterSystem.EnsureCreation();
            KinematicCharacterSystem.Settings.AutoSimulation = false;
        }

      

        public void RegisterMotor(KinematicCharacterMotor motor)
        {
            if (motor != null && !_motors.Contains(motor))
                _motors.Add(motor);
        }

        public void UnregisterMotor(KinematicCharacterMotor motor)
        {
            _motors.Remove(motor);
        }

        public void RegisterMover(PhysicsMover mover)
        {
            if (mover != null && !_movers.Contains(mover))
                _movers.Add(mover);
        }

        public void UnregisterMover(PhysicsMover mover)
        {
            _movers.Remove(mover);
        }

        
        public void BeginTickSimulation(float deltaTime)
        {
            if (KinematicCharacterSystem.Settings.Interpolate)
                KinematicCharacterSystem.PreSimulationInterpolationUpdate(deltaTime);
        }

        public void EndTickSimulation(float deltaTime)
        {
            if (KinematicCharacterSystem.Settings.Interpolate)
                KinematicCharacterSystem.PostSimulationInterpolationUpdate(deltaTime);
        }

        

        public void SimulateMotor(KinematicCharacterMotor motor, float deltaTime)
        {
            _singleMotorCollection.Clear();
            _singleMotorCollection.Add(motor);
            KinematicCharacterSystem.Simulate(deltaTime, _singleMotorCollection, _noMovers);
        }
        

        public void SimulateAll(float deltaTime)
        {
            if (_simulating) return;
            _simulating = true;

            KinematicCharacterSystem.Simulate(deltaTime, _motors, _movers);

            _simulating = false;
        }

        public void SimulateAllMotorsExcept(KinematicCharacterMotor motor, float deltaTime)
        {
            if (_simulating) return;
            _simulating = true;

            _filteredScratch.Clear();
            foreach (var m in _motors)
                if (m != motor) _filteredScratch.Add(m);

            KinematicCharacterSystem.Simulate(deltaTime, _filteredScratch, _movers);

            _simulating = false;
        }
    }
}