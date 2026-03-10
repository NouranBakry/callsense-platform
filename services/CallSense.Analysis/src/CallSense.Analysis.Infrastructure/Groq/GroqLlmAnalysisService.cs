using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CallSense.Analysis.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CallSense.Analysis.Infrastructure.Groq;

public class GroqLlmAnalysisService : ILlmAnalysisService
{
    private const string Url = "https://api.groq.com/openai/v1/chat/completions";
    private const string Model = "llama-3.3-70b-versatile";

    private readonly HttpClient _httpClient;
    private readonly ILogger<GroqLlmAnalysisService> _logger;

    public GroqLlmAnalysisService(HttpClient httpClient, IConfiguration configuration, ILogger<GroqLlmAnalysisService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var apiKey = configuration["Groq:ApiKey"]
            ?? throw new InvalidOperationException("Groq:ApiKey not configured.");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<LlmAnalysisResult> AnalyzeTranscriptAsync(string transcriptText, CancellationToken cancellationToken = default)
    {
        var prompt = $$"""
            Analyse this customer service call transcript and return a JSON object with exactly these fields:
            {
              "overallScore": <integer 1-10>,
              "report": "<2-3 sentence summary of call quality>",
              "strengths": ["<strength 1>", "<strength 2>"],
              "improvements": ["<improvement 1>", "<improvement 2>"]
            }

            Scoring: 9-10 excellent, 7-8 good, 5-6 average, 3-4 poor, 1-2 very poor.

            Transcript:
            {{transcriptText}}
            """;

        var requestBody = JsonSerializer.Serialize(new
        {
            model = Model,
            messages = new[]
            {
                new { role = "system", content = "You are a call centre quality analyst. Respond ONLY with valid JSON, no markdown." },
                new { role = "user", content = prompt }
            },
            temperature = 0.2,
            response_format = new { type = "json_object" }
        });

        using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(Url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Groq LLM returned {Status}: {Body}", response.StatusCode, error);
            throw new HttpRequestException($"Groq analysis failed ({response.StatusCode}): {error}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var analysisJson = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? throw new InvalidOperationException("Empty response from Groq.");

        using var analysisDoc = JsonDocument.Parse(analysisJson);
        var root = analysisDoc.RootElement;
        return new LlmAnalysisResult(
            OverallScore: root.GetProperty("overallScore").GetInt32(),
            Report: root.GetProperty("report").GetString() ?? "",
            Strengths: root.GetProperty("strengths").EnumerateArray().Select(e => e.GetString() ?? "").ToArray(),
            Improvements: root.GetProperty("improvements").EnumerateArray().Select(e => e.GetString() ?? "").ToArray());
    }
}
