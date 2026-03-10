using System.Net.Http.Headers;
using CallSense.Intake.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CallSense.Intake.Infrastructure.Transcription;

public class GroqTranscriptionService : ITranscriptionService
{
    private const string GroqApiUrl = "https://api.groq.com/openai/v1/audio/transcriptions";
    private const string Model = "whisper-large-v3-turbo";

    private readonly HttpClient _httpClient;
    private readonly ILogger<GroqTranscriptionService> _logger;

    public GroqTranscriptionService(HttpClient httpClient, IConfiguration configuration, ILogger<GroqTranscriptionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var apiKey = configuration["Groq:ApiKey"]
            ?? throw new InvalidOperationException("Groq:ApiKey is not configured.");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<string> TranscribeAsync(Stream audioStream, string fileName, CancellationToken cancellationToken = default)
    {
        // Read to byte[] first — StreamContent would dispose the caller's stream when the form is disposed
        var audioBytes = new byte[audioStream.Length - audioStream.Position];
        _ = await audioStream.ReadAsync(audioBytes, cancellationToken);

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(audioBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent(Model), "model");
        form.Add(new StringContent("text"), "response_format");

        _logger.LogInformation("Sending {FileName} to Groq Whisper", fileName);
        var response = await _httpClient.PostAsync(GroqApiUrl, form, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Groq Whisper returned {Status}: {Body}", response.StatusCode, error);
            throw new HttpRequestException($"Groq transcription failed ({response.StatusCode}): {error}");
        }

        var transcript = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogInformation("Transcription complete: {Length} chars", transcript.Length);
        return transcript.Trim();
    }
}
