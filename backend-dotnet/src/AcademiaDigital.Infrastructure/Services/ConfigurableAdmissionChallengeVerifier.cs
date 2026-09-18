using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using AcademiaDigital.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AcademiaDigital.Infrastructure.Services;

public sealed class ConfigurableAdmissionChallengeVerifier : IAdmissionChallengeVerifier, IDisposable
{
    private const string DefaultTurnstileVerificationUrl =
        "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    private readonly AdmissionChallengeMode _mode;
    private readonly string? _secret;
    private readonly Uri? _verificationUri;
    private readonly HttpClient _httpClient;

    public ConfigurableAdmissionChallengeVerifier(IConfiguration configuration)
    {
        var section = configuration.GetSection("AdmissionAntiAbuse:Challenge");
        var configuredMode = section["Mode"] ?? nameof(AdmissionChallengeMode.Disabled);
        if (!Enum.TryParse(configuredMode, ignoreCase: true, out _mode))
            throw new InvalidOperationException(
                $"Unsupported admission challenge mode '{configuredMode}'.");

        _secret = section["Secret"];
        if (_mode is AdmissionChallengeMode.StaticToken or AdmissionChallengeMode.Turnstile
            && string.IsNullOrWhiteSpace(_secret))
            throw new InvalidOperationException(
                $"AdmissionAntiAbuse:Challenge:Secret is required for mode '{_mode}'.");

        if (_mode == AdmissionChallengeMode.Turnstile)
        {
            var verificationUrl = section["VerificationUrl"] ?? DefaultTurnstileVerificationUrl;
            if (!Uri.TryCreate(verificationUrl, UriKind.Absolute, out _verificationUri)
                || _verificationUri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException(
                    "Admission challenge verification URL must be an absolute HTTPS URL.");
        }

        var timeoutSeconds = int.TryParse(section["TimeoutSeconds"], out var configuredTimeout)
            ? Math.Clamp(configuredTimeout, 1, 30)
            : 5;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(timeoutSeconds) };
    }

    public Task<bool> VerifyAsync(
        string? challengeToken,
        string? remoteIpAddress,
        CancellationToken ct = default)
        => _mode switch
        {
            AdmissionChallengeMode.Disabled => Task.FromResult(true),
            AdmissionChallengeMode.StaticToken => Task.FromResult(VerifyStaticToken(challengeToken)),
            AdmissionChallengeMode.Turnstile => VerifyTurnstileAsync(
                challengeToken,
                remoteIpAddress,
                ct),
            _ => Task.FromResult(false)
        };

    private bool VerifyStaticToken(string? challengeToken)
    {
        if (string.IsNullOrEmpty(challengeToken) || string.IsNullOrEmpty(_secret))
            return false;

        var supplied = Encoding.UTF8.GetBytes(challengeToken);
        var expected = Encoding.UTF8.GetBytes(_secret);
        return CryptographicOperations.FixedTimeEquals(supplied, expected);
    }

    private async Task<bool> VerifyTurnstileAsync(
        string? challengeToken,
        string? remoteIpAddress,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(challengeToken)
            || string.IsNullOrWhiteSpace(_secret)
            || _verificationUri is null)
            return false;

        var fields = new Dictionary<string, string>
        {
            ["secret"] = _secret,
            ["response"] = challengeToken
        };
        if (!string.IsNullOrWhiteSpace(remoteIpAddress))
            fields["remoteip"] = remoteIpAddress;

        try
        {
            using var content = new FormUrlEncodedContent(fields);
            using var response = await _httpClient.PostAsync(_verificationUri, content, ct);
            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<TurnstileResponse>(
                cancellationToken: ct);
            return result?.Success == true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return false;
        }
    }

    public void Dispose() => _httpClient.Dispose();

    private enum AdmissionChallengeMode
    {
        Disabled,
        StaticToken,
        Turnstile
    }

    private sealed record TurnstileResponse(
        [property: JsonPropertyName("success")] bool Success);
}
