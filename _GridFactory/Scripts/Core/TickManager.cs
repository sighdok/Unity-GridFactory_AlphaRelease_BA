using System;

using UnityEngine;

using GridFactory.Conveyor;
using System.Runtime.InteropServices.WindowsRuntime;

namespace GridFactory.Core
{
    public class TickManager : MonoBehaviour
    {
        public static TickManager Instance { get; private set; }

        [SerializeField] private float tickInterval = 0.025f; // seconds

        private float _timer;

        public event Action OnTick;
        public event Action OnTickEnergy;

        public float TickInterval
        {
            get => tickInterval;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= tickInterval)
            {
                _timer -= tickInterval;

                ConveyorBase.TickAllConveyors();
                MetaConveyorBase.TickAllConveyors();

                OnTickEnergy?.Invoke();
                OnTick?.Invoke();
            }
        }
    }
}
