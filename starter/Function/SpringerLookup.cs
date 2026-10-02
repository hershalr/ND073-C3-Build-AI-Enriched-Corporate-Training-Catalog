using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Udacity.springerlookupdemo
{
    public class SpringerLookup
    {
        static readonly string apikey = "7fc47d5599e50f2a9994ce32c6ca8030";
        static readonly string springerapiendpoint = "https://api.springernature.com/openaccess/json";

        private class InputRecord
        {
            public class InputRecordData
            {
                public string ArticleName { get; set; }
            }

            public string RecordId { get; set; }
            public InputRecordData Data { get; set; }
        }

        private class WebApiRequest
        {
            public List<InputRecord> Values { get; set; }
        }

        private class OutputRecord
        {
            public class OutputRecordData
            {
                public string PublicationName { get; set; } = "";
                public string Publisher { get; set; } = "";
                public string DOI { get; set; } = "";
                public string PublicationDate { get; set; } = "";
            }

            public class OutputRecordMessage
            {
                public string Message { get; set; } = string.Empty;
            }

            public string RecordId { get; set; }
            public OutputRecordData Data { get; set; }
            public List<OutputRecordMessage> Errors { get; set; } = new();
            public List<OutputRecordMessage> Warnings { get; set; } = new();
        }

        private class WebApiResponse
        {
            public List<OutputRecord> Values { get; set; } = new();
        }

        [Function("SpringerLookup")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var log = executionContext.GetLogger("SpringerLookup");
            log.LogInformation("SpringerLookup isolated HTTP trigger processed a request.");

            var responsePayload = new WebApiResponse { Values = new List<OutputRecord>() };
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<WebApiRequest>(requestBody);

            if (data?.Values == null)
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("The request schema does not match expected schema.");
                return bad;
            }

            foreach (var record in data.Values)
            {
                if (record == null || record.RecordId == null) continue;
                var responseRecord = new OutputRecord { RecordId = record.RecordId };
                try
                {
                    responseRecord.Data = await GetEntityMetadata(record.Data?.ArticleName ?? string.Empty);
                }
                catch (Exception e)
                {
                    responseRecord.Errors.Add(new OutputRecord.OutputRecordMessage { Message = e.Message });
                }
                finally
                {
                    responsePayload.Values.Add(responseRecord);
                }
            }

            var ok = req.CreateResponse(HttpStatusCode.OK);
            ok.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await ok.WriteStringAsync(JsonConvert.SerializeObject(responsePayload));
            return ok;
        }

        private static async Task<OutputRecord.OutputRecordData> GetEntityMetadata(string title)
        {
            // Basic Open Access plan: field qualifiers like title: are PREMIUM (403).
            // Use a quoted free-text query instead. Strip .pdf if ArticleName is a blob filename.
            var cleaned = (title ?? string.Empty).Trim();
            if (cleaned.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[..^4];
            }

            var q = Uri.EscapeDataString("\"" + cleaned + "\"");
            var uri = springerapiendpoint + "?q=" + q + "&api_key=" + Uri.EscapeDataString(apikey) + "&p=1";
            var result = new OutputRecord.OutputRecordData();

            using var client = new HttpClient();
            using var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(uri)
            };
            request.Headers.TryAddWithoutValidation("X-ApiKey", apikey);

            var httpResponse = await client.SendAsync(request);
            // Do NOT throw on Springer 404/non-2xx — AI Search treats skill record
            // Errors as indexer failures. Empty metadata is acceptable for misses.
            if (!httpResponse.IsSuccessStatusCode)
            {
                return result;
            }

            string responseBody = await httpResponse.Content.ReadAsStringAsync();
            var springerresults = JObject.Parse(responseBody);
            var parsedresults = springerresults["records"]?.Children().ToList() ?? new List<JToken>();

            foreach (var t in parsedresults)
            {
                result.DOI = t.Value<string>("doi") ?? result.DOI;
                result.PublicationDate = t.Value<string>("publicationDate") ?? result.PublicationDate;
                result.PublicationName = t.Value<string>("publicationName") ?? result.PublicationName;
                result.Publisher = t.Value<string>("publisher") ?? result.Publisher;
            }

            return result;
        }
    }
}
