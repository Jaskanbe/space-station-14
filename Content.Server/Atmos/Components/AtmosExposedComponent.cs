﻿using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Components
{
    /// <summary>
    /// Represents that entity can be exposed to Atmos
    /// </summary>
    [RegisterComponent]
    public class AtmosExposedComponent
    : Component
    {
        public override string Name => "AtmosExposed";

        [ViewVariables]
        [ComponentDependency] private readonly TemperatureComponent? _temperatureComponent = null;

        [ViewVariables]
        [ComponentDependency] private readonly BarotraumaComponent? _barotraumaComponent = null;

        [ViewVariables]
        [ComponentDependency] private readonly FlammableComponent? _flammableComponent = null;

        public void Update(GasMixture air, float frameDelta, AtmosphereSystem atmosphereSystem)
        {
            if (_temperatureComponent != null)
            {
                var temperatureDelta = air.Temperature - _temperatureComponent.CurrentTemperature;
                var tileHeatCapacity = atmosphereSystem.GetHeatCapacity(air);
                var heat = temperatureDelta * (tileHeatCapacity * _temperatureComponent.HeatCapacity / (tileHeatCapacity + _temperatureComponent.HeatCapacity));
                _temperatureComponent.ReceiveHeat(heat);
                _temperatureComponent.Update();
            }

            _barotraumaComponent?.Update(air.Pressure);

            _flammableComponent?.Update(air);
        }
    }
}
