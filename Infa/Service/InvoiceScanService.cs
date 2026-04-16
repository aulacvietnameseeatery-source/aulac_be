using Core.Data;
using Core.DTO.General;
using Core.DTO.Inventory;
using Core.Interface.Service.Others;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infa.Service
{
    public class InvoiceScanService : IInvoiceScanService
    {
        private readonly HttpClient _httpClient;
        private readonly LlmVisionOptions _options;
        private readonly ILogger<InvoiceScanService> _logger;

        public InvoiceScanService(
            HttpClient httpClient,
            IOptions<LlmVisionOptions> options,
            ILogger<InvoiceScanService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<InvoiceScanResult> ScanInvoiceAsync(
            MediaFileInput image, CancellationToken ct = default)
        {
            try
            {
                var base64 = await ConvertToBase64Async(image, ct);
                var mimeType = image.ContentType ?? "image/jpeg";

                if (_options.Provider.Equals("gemini", StringComparison.OrdinalIgnoreCase))
                {
                    return await ScanWithGeminiSdkAsync(image, mimeType, ct);
                }

                var (requestUrl, requestBody, authHeader) = BuildProviderRequest(base64, mimeType);

                using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                if (authHeader != null)
                    request.Headers.Authorization = authHeader;

                _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

                using var response = await _httpClient.SendAsync(request, ct);

                var responseBody = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("LLM Vision API returned {Status}: {Body}",
                        (int)response.StatusCode, responseBody.Length > 500 ? responseBody[..500] : responseBody);

                    return new InvoiceScanResult
                    {
                        Success = false,
                        ErrorMessage = $"LLM API returned HTTP {(int)response.StatusCode}. Check server logs for details."
                    };
                }

                var (rawJson, tokensUsed) = ExtractContentFromResponse(responseBody);

                return new InvoiceScanResult
                {
                    Success = true,
                    RawJson = rawJson,
                    TokensUsed = tokensUsed
                };
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("LLM Vision API request timed out after {Timeout}s", _options.TimeoutSeconds);
                return new InvoiceScanResult
                {
                    Success = false,
                    ErrorMessage = "Invoice scan timed out. Please try again."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during invoice scan");
                return new InvoiceScanResult
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred during invoice scanning."
                };
            }
        }

        private async Task<InvoiceScanResult> ScanWithGeminiSdkAsync(
            MediaFileInput image,
            string mimeType,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                return new InvoiceScanResult
                {
                    Success = false,
                    ErrorMessage = "LlmVision.ApiKey is missing for Gemini provider."
                };
            }

            byte[] imageBytes;
            using (var ms = new MemoryStream())
            {
                image.Stream.Position = 0;
                await image.Stream.CopyToAsync(ms, ct);
                imageBytes = ms.ToArray();
            }

            var client = new Client(apiKey: _options.ApiKey, vertexAI: false);

            var contents = new Content
            {
                Role = "user",
                Parts =
                [
                    Part.FromText($"{SystemPrompt}\n\n{UserPrompt}"),
                    Part.FromBytes(imageBytes, mimeType)
                ]
            };

            var config = new GenerateContentConfig
            {
                Temperature = 0.1f,
                MaxOutputTokens = _options.MaxTokens,
                ResponseMimeType = "application/json",
                ResponseJsonSchema = BuildInvoiceResponseJsonSchema()
            };

            var response = await client.Models.GenerateContentAsync(
                model: _options.Model,
                contents: contents,
                config: config,
                cancellationToken: ct);

            var rawJson = response.Text ?? string.Empty;
            var tokensUsed = response.UsageMetadata?.TotalTokenCount ?? 0;

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return new InvoiceScanResult
                {
                    Success = false,
                    ErrorMessage = "Gemini returned an empty response.",
                    TokensUsed = tokensUsed
                };
            }

            return new InvoiceScanResult
            {
                Success = true,
                RawJson = rawJson,
                TokensUsed = tokensUsed
            };
        }

        // ──────────────────────────────────────────────────────
        // Provider-specific request building
        // ──────────────────────────────────────────────────────

        private (string url, string body, AuthenticationHeaderValue? auth) BuildProviderRequest(
            string base64Image, string mimeType)
        {
            return _options.Provider.ToLowerInvariant() switch
            {
                "openai" => BuildOpenAiRequest(base64Image, mimeType),
                "gemini" => BuildGeminiRequest(base64Image, mimeType),
                "anthropic" => BuildAnthropicRequest(base64Image, mimeType),
                _ => throw new InvalidOperationException(
                    $"Unsupported LLM vision provider: '{_options.Provider}'. Supported: openai, gemini, anthropic.")
            };
        }

        private (string, string, AuthenticationHeaderValue?) BuildOpenAiRequest(string base64, string mime)
        {
            var url = _options.Endpoint ?? "https://api.openai.com/v1/chat/completions";

            var payload = new
            {
                model = _options.Model,
                max_tokens = _options.MaxTokens,
                temperature = 0.1,
                messages = new object[]
                {
                    new { role = "system", content = SystemPrompt },
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = UserPrompt },
                            new
                            {
                                type = "image_url",
                                image_url = new { url = $"data:{mime};base64,{base64}", detail = "high" }
                            }
                        }
                    }
                }
            };

            var body = JsonSerializer.Serialize(payload, SerializerOptions);
            var auth = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            return (url, body, auth);
        }

        private (string, string, AuthenticationHeaderValue?) BuildGeminiRequest(string base64, string mime)
        {
            var url = _options.Endpoint
                ?? $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:generateContent?key={_options.ApiKey}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = $"{SystemPrompt}\n\n{UserPrompt}" },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = mime,
                                    data = base64
                                }
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.1,
                    maxOutputTokens = _options.MaxTokens,
                    responseMimeType = "application/json",
                    responseJsonSchema = BuildInvoiceResponseJsonSchema()
                }
            };

            var body = JsonSerializer.Serialize(payload, SerializerOptions);
            return (url, body, null); // key is in URL for Gemini
        }

        private (string, string, AuthenticationHeaderValue?) BuildAnthropicRequest(string base64, string mime)
        {
            var url = _options.Endpoint ?? "https://api.anthropic.com/v1/messages";

            var payload = new
            {
                model = _options.Model,
                max_tokens = _options.MaxTokens,
                system = SystemPrompt,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new
                            {
                                type = "image",
                                source = new
                                {
                                    type = "base64",
                                    media_type = mime,
                                    data = base64
                                }
                            },
                            new { type = "text", text = UserPrompt }
                        }
                    }
                }
            };

            // Anthropic uses x-api-key header, not Bearer
            _httpClient.DefaultRequestHeaders.Remove("x-api-key");
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _options.ApiKey);
            _httpClient.DefaultRequestHeaders.Remove("anthropic-version");
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var body = JsonSerializer.Serialize(payload, SerializerOptions);
            return (url, body, null);
        }

        // ──────────────────────────────────────────────────────
        // Response extraction (provider-agnostic)
        // ──────────────────────────────────────────────────────

        private (string rawJson, int tokensUsed) ExtractContentFromResponse(string responseBody)
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            int tokensUsed = 0;

            // OpenAI format
            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var content = choices[0].GetProperty("message").GetProperty("content").GetString() ?? "";
                if (root.TryGetProperty("usage", out var usage) &&
                    usage.TryGetProperty("total_tokens", out var tokens))
                    tokensUsed = tokens.GetInt32();

                return (content, tokensUsed);
            }

            // Gemini format
            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var parts = candidates[0].GetProperty("content").GetProperty("parts");
                var text = parts[0].GetProperty("text").GetString() ?? "";
                if (root.TryGetProperty("usageMetadata", out var gUsage) &&
                    gUsage.TryGetProperty("totalTokenCount", out var gTokens))
                    tokensUsed = gTokens.GetInt32();

                return (text, tokensUsed);
            }

            // Anthropic format
            if (root.TryGetProperty("content", out var anthropicContent) && anthropicContent.GetArrayLength() > 0)
            {
                var text = anthropicContent[0].GetProperty("text").GetString() ?? "";
                if (root.TryGetProperty("usage", out var aUsage) &&
                    aUsage.TryGetProperty("input_tokens", out var inputT) &&
                    aUsage.TryGetProperty("output_tokens", out var outputT))
                    tokensUsed = inputT.GetInt32() + outputT.GetInt32();

                return (text, tokensUsed);
            }

            // Fallback: return the entire response body
            _logger.LogWarning("Could not parse LLM response format, returning raw body");
            return (responseBody, 0);
        }

        // ──────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────

        private static async Task<string> ConvertToBase64Async(MediaFileInput image, CancellationToken ct)
        {
            using var ms = new MemoryStream();
            await image.Stream.CopyToAsync(ms, ct);
            return Convert.ToBase64String(ms.ToArray());
        }

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        private static Dictionary<string, object> BuildInvoiceResponseJsonSchema()
        {
            return new Dictionary<string, object>
            {
                ["type"] = "object",
                ["propertyOrdering"] = new[]
                {
                    "document_type", "header", "lines", "totals", "raw_text_excerpt"
                },
                ["properties"] = new Dictionary<string, object>
                {
                    ["document_type"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["enum"] = new[] { "INVOICE", "RECEIPT", "DELIVERY_NOTE", "UNKNOWN" },
                        ["description"] = "Detected document type"
                    },
                    ["header"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["propertyOrdering"] = new[]
                        {
                            "supplier_name", "supplier_address", "supplier_phone", "supplier_tax_id",
                            "invoice_number", "invoice_date", "client_name", "client_number",
                            "delivery_number", "delivery_date", "payment_method", "currency"
                        },
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["supplier_name"] = NullableStringSchema("Supplier name as printed"),
                            ["supplier_address"] = NullableStringSchema("Supplier address as printed"),
                            ["supplier_phone"] = NullableStringSchema("Supplier phone as printed"),
                            ["supplier_tax_id"] = NullableStringSchema("Supplier VAT/tax id as printed"),
                            ["invoice_number"] = NullableStringSchema("Invoice number as printed"),
                            ["invoice_date"] = NullableStringSchema("Invoice date in YYYY-MM-DD if parseable"),
                            ["client_name"] = NullableStringSchema("Client name as printed"),
                            ["client_number"] = NullableStringSchema("Client number as printed"),
                            ["delivery_number"] = NullableStringSchema("Delivery note / BL number if present"),
                            ["delivery_date"] = NullableStringSchema("Delivery date in YYYY-MM-DD if parseable"),
                            ["payment_method"] = NullableStringSchema("Payment method if present"),
                            ["currency"] = new Dictionary<string, object>
                            {
                                ["type"] = "string",
                                ["description"] = "Document currency, default CHF"
                            }
                        },
                        ["required"] = new[]
                        {
                            "supplier_name", "supplier_address", "supplier_phone", "supplier_tax_id",
                            "invoice_number", "invoice_date", "client_name", "client_number",
                            "delivery_number", "delivery_date", "payment_method", "currency"
                        }
                    },
                    ["lines"] = new Dictionary<string, object>
                    {
                        ["type"] = "array",
                        ["items"] = new Dictionary<string, object>
                        {
                            ["type"] = "object",
                            ["propertyOrdering"] = new[]
                            {
                                "line_number", "raw_text", "identifier", "item_name", "origin",
                                "quantity", "unit", "unit_price", "line_total", "discount",
                                "batch_number", "expiry_date", "notes", "ocr_confidence"
                            },
                            ["properties"] = new Dictionary<string, object>
                            {
                                ["line_number"] = new Dictionary<string, object>
                                {
                                    ["type"] = "integer",
                                    ["description"] = "1-based line index"
                                },
                                ["raw_text"] = NullableStringSchema("Original full text for this line"),
                                ["identifier"] = NullableStringSchema("Identifier-like text as printed; do not normalize"),
                                ["item_name"] = NullableStringSchema("Item description as printed"),
                                ["origin"] = NullableStringSchema("Origin/country text if present"),
                                ["quantity"] = NullableNumberSchema("Numeric quantity"),
                                ["unit"] = NullableStringSchema("Unit text as printed"),
                                ["unit_price"] = NullableNumberSchema("Unit price without currency symbol"),
                                ["line_total"] = NullableNumberSchema("Line total without currency symbol"),
                                ["discount"] = NullableNumberSchema("Discount amount if present"),
                                ["batch_number"] = NullableStringSchema("Batch number if present"),
                                ["expiry_date"] = NullableStringSchema("Expiry date in YYYY-MM-DD if parseable"),
                                ["notes"] = NullableStringSchema("Additional line notes"),
                                ["ocr_confidence"] = new Dictionary<string, object>
                                {
                                    ["type"] = new[] { "string", "null" },
                                    ["enum"] = new object?[] { "HIGH", "MEDIUM", "LOW", null },
                                    ["description"] = "OCR confidence for this line"
                                }
                            },
                            ["required"] = new[]
                            {
                                "line_number", "raw_text", "identifier", "item_name", "origin",
                                "quantity", "unit", "unit_price", "line_total", "discount",
                                "batch_number", "expiry_date", "notes", "ocr_confidence"
                            }
                        }
                    },
                    ["totals"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["propertyOrdering"] = new[]
                        {
                            "subtotal", "tax_rate", "tax_amount", "total", "total_label"
                        },
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["subtotal"] = NullableNumberSchema("Subtotal without tax"),
                            ["tax_rate"] = NullableNumberSchema("Tax rate percentage"),
                            ["tax_amount"] = NullableNumberSchema("Tax amount"),
                            ["total"] = NullableNumberSchema("Grand total"),
                            ["total_label"] = NullableStringSchema("Exact total label text from document")
                        },
                        ["required"] = new[]
                        {
                            "subtotal", "tax_rate", "tax_amount", "total", "total_label"
                        }
                    },
                    ["raw_text_excerpt"] = NullableStringSchema("Short raw OCR excerpt for debugging")
                },
                ["required"] = new[]
                {
                    "document_type", "header", "lines", "totals", "raw_text_excerpt"
                }
            };
        }

        private static Dictionary<string, object> NullableStringSchema(string description)
        {
            return new Dictionary<string, object>
            {
                ["type"] = new[] { "string", "null" },
                ["description"] = description
            };
        }

        private static Dictionary<string, object> NullableNumberSchema(string description)
        {
            return new Dictionary<string, object>
            {
                ["type"] = new[] { "number", "null" },
                ["description"] = description
            };
        }

        // ──────────────────────────────────────────────────────
        // Prompt template
        // ──────────────────────────────────────────────────────

        private const string SystemPrompt = """
            You are a precise invoice/receipt OCR extraction engine for a restaurant inventory system.
            You MUST return ONLY valid JSON — no markdown fences, no explanations, no extra text.
            Extract every visible piece of information from the invoice image.
            If a field is not visible or unclear, set it to null — NEVER guess or fabricate values.
            For numbers, preserve the exact format printed on the document.
            For dates, use ISO format YYYY-MM-DD when converting.
            The invoices may be in French, German, Vietnamese, or English.
            """;

        private const string UserPrompt = """
            Extract all data from this invoice/receipt image according to the response JSON schema.
            CRITICAL RULES:
            - Return ONLY the JSON object, nothing else
            - Every line item visible on the document MUST be included
            - If no line items are visible, return an empty "lines" array
            - Preserve raw text exactly as printed for identifiers and item names
            - For European number formats: use dot as decimal separator in the output
              (e.g. "15.700 KG" at unit price 23.00 with total 361.10 means quantity is 15.7)
            - Currency values should be plain numbers without currency symbols
            """;
    }
}
