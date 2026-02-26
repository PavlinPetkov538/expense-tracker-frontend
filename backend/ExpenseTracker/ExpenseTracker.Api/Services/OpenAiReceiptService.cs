using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ExpenseTracker.Api.Services;

public class OpenAiReceiptService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _cfg;

    public OpenAiReceiptService(HttpClient http, IConfiguration cfg)
    {
        _http = http;
        _cfg = cfg;
    }

    public record AiTx(
        decimal Amount,
        string Type,           // "expense" | "income"
        string Date,           // "YYYY-MM-DD"
        string CategoryName,
        string Note,
        string Merchant,
        decimal Confidence
    );

    public async Task<AiTx> ExtractFromTextAsync(string text)
    {
        var input = new object[]
        {
            new {
                role = "user",
                content = new object[]
                {
                    new { type = "input_text", text = BuildPrompt(text) }
                }
            }
        };

        return await CallAsync(input);
    }

    public async Task<AiTx> ExtractFromFileAsync(byte[] bytes, string contentType, string? extraNote)
    {
        var b64 = Convert.ToBase64String(bytes);

        object filePart = contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase)
            ? new { type = "input_file", file_data = b64, filename = "receipt.pdf" }
            : new { type = "input_image", image_data = b64 };

        var input = new object[]
        {
            new {
                role = "user",
                content = new object[]
                {
                    new { type = "input_text", text = BuildPrompt(extraNote) },
                    filePart
                }
            }
        };

        return await CallAsync(input);
    }

    private string BuildPrompt(string? extra)
    {
        return
$@"You are extracting ONE financial transaction from the user input (text and/or receipt).
Return strictly valid JSON matching the schema.

Rules:
- Amount must be positive number.
- Type must be 'expense' or 'income'.
- Date must be YYYY-MM-DD. If missing, use today's date (UTC).
- CategoryName should be a short human category like 'Groceries', 'Transport', 'Salary', etc.
- Merchant can be empty if unknown.
- Confidence is 0..1.

Extra user note: {extra ?? ""}";
    }

    private async Task<AiTx> CallAsync(object[] input)
    {
        var apiKey = _cfg["OpenAI:ApiKey"];

        Console.WriteLine($"[OpenAI] ApiKey loaded? {(string.IsNullOrWhiteSpace(apiKey) ? "NO" : "YES")}  Length={apiKey?.Length ?? 0}");
        Console.WriteLine($"[OpenAI] ApiKey prefix: {(string.IsNullOrWhiteSpace(apiKey) ? "EMPTY" : apiKey.Substring(0, Math.Min(6, apiKey.Length)))}");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Missing OpenAI:ApiKey in configuration.");

        var model = _cfg["OpenAI:Model"] ?? "gpt-4o-mini";

        var schema = new
        {
            name = "transaction_extraction",
            strict = true,
            schema = new
            {
                type = "object",
                additionalProperties = false,
                required = new[] { "Amount", "Type", "Date", "CategoryName", "Note", "Merchant", "Confidence" },
                properties = new
                {
                    Amount = new { type = "number" },
                    Type = new { type = "string", @enum = new[] { "expense", "income" } },
                    Date = new { type = "string" },
                    CategoryName = new { type = "string" },
                    Note = new { type = "string" },
                    Merchant = new { type = "string" },
                    Confidence = new { type = "number", minimum = 0, maximum = 1 }
                }
            }
        };

        var payload = new
        {
            model,
            input,
            response_format = new
            {
                type = "json_schema",
                json_schema = schema
            }
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var res = await _http.SendAsync(req);
        var body = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
        {
            // This prints the REAL OpenAI error message
            Console.WriteLine($"[OpenAI] Status: {(int)res.StatusCode} {res.StatusCode}");
            Console.WriteLine($"[OpenAI] Body: {body}");

            // Also return it to frontend (so you can see it)
            throw new InvalidOperationException($"OpenAI error {(int)res.StatusCode}: {body}");
        }

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());

        var outText = doc.RootElement
            .GetProperty("output")[0]
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(outText))
            throw new InvalidOperationException("OpenAI returned empty output.");

        var parsed = JsonSerializer.Deserialize<AiTx>(
            outText,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (parsed == null)
            throw new InvalidOperationException("Failed to parse OpenAI JSON.");

        return parsed;
    }
}