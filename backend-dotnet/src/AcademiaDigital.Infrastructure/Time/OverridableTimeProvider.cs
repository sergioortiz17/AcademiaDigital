namespace AcademiaDigital.Infrastructure.Time;

/// <summary>
/// TimeProvider que permite "viajar en el tiempo" en DESARROLLO: cuando está habilitado, devuelve
/// una hora simulada que sigue CORRIENDO desde el instante en que se fijó (no queda congelada).
///
/// Se registra como singleton único en lugar de TimeProvider.System, de modo que todos los handlers
/// que ya inyectan TimeProvider (períodos de inscripción, asistencia y su plazo de 48hs, asignaciones
/// docentes, comisiones, certificados, planillas) usan el reloj simulado sin cambios.
///
/// Deliberadamente NO afecta login/JWT/sesiones/lockout: esos usan DateTime.UtcNow directo, no este
/// proveedor. El control de activación vive en el TimeTravelController, gateado por ALLOW_TIME_TRAVEL.
/// </summary>
public sealed class OverridableTimeProvider : TimeProvider
{
    private readonly TimeProvider _system = System;
    private readonly object _gate = new();

    private bool _enabled;
    // Hora simulada "base" cargada (persiste aunque se desactive el toggle, para no perder el valor).
    private DateTimeOffset? _fakeNow;
    // Diferencia entre la hora simulada base y la real en el momento de fijarla. Sumado a la hora real
    // hace que el tiempo simulado avance junto con el real (no congelado).
    private TimeSpan _offset = TimeSpan.Zero;

    public bool Enabled { get { lock (_gate) return _enabled; } }

    /// <summary>La hora simulada base cargada (aunque el override esté apagado). Null si no hay ninguna.</summary>
    public DateTimeOffset? FakeNow { get { lock (_gate) return _fakeNow; } }

    /// <summary>La hora que efectivamente ven los handlers en este instante.</summary>
    public DateTimeOffset EffectiveNow => GetUtcNow();

    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate)
        {
            if (_enabled && _fakeNow.HasValue)
                return _system.GetUtcNow() + _offset;
            return _system.GetUtcNow();
        }
    }

    /// <summary>Fija la hora simulada y activa el override. El offset se calcula contra la hora real
    /// del momento, así el tiempo simulado sigue corriendo desde ese punto.</summary>
    public void Apply(DateTimeOffset fakeNow)
    {
        lock (_gate)
        {
            _fakeNow = fakeNow;
            _offset = fakeNow - _system.GetUtcNow();
            _enabled = true;
        }
    }

    /// <summary>Prende/apaga el override sin tocar el fakeNow cargado. Al re-prender, recalcula el
    /// offset contra la hora real actual para retomar la simulación corrida por el tiempo transcurrido.</summary>
    public void SetEnabled(bool enabled)
    {
        lock (_gate)
        {
            if (enabled)
            {
                if (!_fakeNow.HasValue) return; // no hay nada cargado; no se puede prender
                // Retomar: el fakeNow "efectivo" avanzó con el reloj real mientras estuvo apagado.
                // Mantener el mismo offset preserva esa continuidad.
                _enabled = true;
            }
            else
            {
                _enabled = false;
            }
        }
    }

    /// <summary>Limpia todo: vuelve a tiempo real y descarta el fakeNow.</summary>
    public void Clear()
    {
        lock (_gate)
        {
            _enabled = false;
            _fakeNow = null;
            _offset = TimeSpan.Zero;
        }
    }
}
