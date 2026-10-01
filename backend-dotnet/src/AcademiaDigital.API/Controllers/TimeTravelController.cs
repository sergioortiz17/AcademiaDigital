using AcademiaDigital.Infrastructure.Time;
using Microsoft.AspNetCore.Mvc;

namespace AcademiaDigital.API.Controllers;

/// <summary>
/// Control del "viaje en el tiempo" para DEV: permite simular que la app corre en otra fecha/hora,
/// afectando a todo lo que usa TimeProvider inyectado (períodos, asistencia y su plazo de 48hs,
/// asignaciones docentes, comisiones, certificados, planillas). NO afecta login/JWT/sesiones/lockout.
///
/// SALVAGUARDA: todos los endpoints devuelven 403 si la variable de entorno ALLOW_TIME_TRAVEL no es
/// 'true'. Mismo criterio anti-producción que ALLOW_DESTRUCTIVE_DB_TOOLS de dev-tools.
/// </summary>
[ApiController]
[Route("api/v1/devtools/time")]
public sealed class TimeTravelController(OverridableTimeProvider timeProvider) : ControllerBase
{
    private static bool Allowed =>
        string.Equals(Environment.GetEnvironmentVariable("ALLOW_TIME_TRAVEL"), "true", StringComparison.OrdinalIgnoreCase);

    private IActionResult? Guard()
        => Allowed ? null : StatusCode(StatusCodes.Status403Forbidden, new
        {
            error = "Time-travel deshabilitado en este ambiente (falta ALLOW_TIME_TRAVEL=true)."
        });

    // GET: estado actual del reloj (real, efectivo, si está activo y el fakeNow cargado).
    [HttpGet]
    public IActionResult GetState()
    {
        var guard = Guard(); if (guard is not null) return guard;
        return Ok(new
        {
            realNow = TimeProvider.System.GetUtcNow(),
            effectiveNow = timeProvider.EffectiveNow,
            enabled = timeProvider.Enabled,
            fakeNow = timeProvider.FakeNow
        });
    }

    // POST { fakeNow }: carga la fecha simulada y activa el override.
    [HttpPost]
    public IActionResult Apply([FromBody] SetFakeNowRequest request)
    {
        var guard = Guard(); if (guard is not null) return guard;
        timeProvider.Apply(request.FakeNow);
        return Ok(new { enabled = timeProvider.Enabled, fakeNow = timeProvider.FakeNow, effectiveNow = timeProvider.EffectiveNow });
    }

    // PATCH toggle { enabled }: prende/apaga sin perder el fakeNow cargado.
    [HttpPatch("toggle")]
    public IActionResult Toggle([FromBody] ToggleRequest request)
    {
        var guard = Guard(); if (guard is not null) return guard;
        timeProvider.SetEnabled(request.Enabled);
        return Ok(new { enabled = timeProvider.Enabled, fakeNow = timeProvider.FakeNow, effectiveNow = timeProvider.EffectiveNow });
    }

    // DELETE: limpia todo y vuelve a tiempo real.
    [HttpDelete]
    public IActionResult Reset()
    {
        var guard = Guard(); if (guard is not null) return guard;
        timeProvider.Clear();
        return Ok(new { enabled = timeProvider.Enabled, fakeNow = timeProvider.FakeNow, effectiveNow = timeProvider.EffectiveNow });
    }
}

public sealed record SetFakeNowRequest(DateTimeOffset FakeNow);
public sealed record ToggleRequest(bool Enabled);
