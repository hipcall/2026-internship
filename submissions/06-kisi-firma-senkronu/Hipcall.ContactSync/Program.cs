using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Hipcall.ContactSync
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string? apiToken = Environment.GetEnvironmentVariable("HIPCALL_API_TOKEN");
            if (string.IsNullOrEmpty(apiToken))
            {
                Console.WriteLine("HATA: Lütfen HIPCALL_API_TOKEN çevre değişkenini ayarlayın.");
                return;
            }

            var baseAddress = Environment.GetEnvironmentVariable("HIPCALL_API_ENDPOINT") ?? "https://use.hipcall.com.tr/api/v3/";
            using var httpClient = new HttpClient { BaseAddress = new Uri(baseAddress.TrimEnd('/') + "/") };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            string jsonFilePath = File.Exists("crm_data.json") ? "crm_data.json" : Path.Combine(AppContext.BaseDirectory, "crm_data.json");
            if (!File.Exists(jsonFilePath))
            {
                Console.WriteLine($"HATA: {jsonFilePath} bulunamadı.");
                return;
            }

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var records = JsonSerializer.Deserialize<List<CrmRecord>>(File.ReadAllText(jsonFilePath), jsonOptions);
            if (records == null || records.Count == 0)
            {
                Console.WriteLine("CRM verisi bulunamadı veya çözümlenemedi.");
                return;
            }

            Console.WriteLine($"--- Hipcall Contact & Company Sync Başlatılıyor ({records.Count} kayıt) ---\n");

            int created = 0, updated = 0, unchanged = 0, errors = 0;

            foreach (var record in records)
            {
                Console.WriteLine($"İşleniyor: [{record.CustomerId}] {record.FirstName} {record.LastName}");
                try
                {
                    int? internalCompanyId = await SyncCompanyAsync(httpClient, record.CompanyId);

                    string status = await SyncContactAsync(httpClient, record, internalCompanyId);
                    
                    Console.WriteLine($"Durum: {status}");

                    if (status == "created") created++;
                    else if (status == "updated") updated++;
                    else if (status == "unchanged") unchanged++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"HATA ({record.CustomerId}): {ex.Message}");
                    errors++;
                }
            }

            Console.WriteLine($"\n================ ÖZET ================");
            Console.WriteLine($"Özet: {created} oluşturuldu, {updated} güncellendi, {unchanged} değişmedi, {errors} hata.");
            Console.WriteLine($"======================================");
        }

        static async Task<int?> SyncCompanyAsync(HttpClient client, string? externalId)
        {
            if (string.IsNullOrWhiteSpace(externalId)) return null;

            var response = await client.GetAsync($"companies/by-external-id/{externalId}");
            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<HipcallResponse<Company>>();
                return body?.Data?.Id;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var req = new { name = $"Firma {externalId}", external_id = externalId };
                var postResp = await client.PostAsJsonAsync("companies", req);
                if (!postResp.IsSuccessStatusCode)
                {
                    string err = await postResp.Content.ReadAsStringAsync();
                    throw new Exception($"Firma oluşturma hatası: {postResp.StatusCode} - {err}");
                }
                var body = await postResp.Content.ReadFromJsonAsync<HipcallResponse<Company>>();
                return body?.Data?.Id;
            }
            else
            {
                string err = await response.Content.ReadAsStringAsync();
                throw new Exception($"Firma sorgulama hatası: {response.StatusCode} - {err}");
            }
        }

        static async Task<string> SyncContactAsync(HttpClient client, CrmRecord record, int? companyId)
        {
            string status = "unchanged";
            int contactId;
            List<Phone> existingPhones = new List<Phone>();

            var getResp = await client.GetAsync($"contacts/by-external-id/{record.CustomerId}");
            if (getResp.IsSuccessStatusCode)
            {
                var body = await getResp.Content.ReadFromJsonAsync<HipcallResponse<Contact>>();
                var contact = body?.Data ?? throw new Exception("Kişi verisi boş döndü.");
                contactId = contact.Id;
                existingPhones = contact.Phones ?? new List<Phone>();

                bool needsUpdate = false;
                var patchReq = new Dictionary<string, object>();
                var updateDetails = new List<string>();

                string currentFirst = (contact.FirstName ?? "").Trim();
                string newFirst = (record.FirstName ?? "").Trim();
                if (!string.Equals(currentFirst, newFirst, StringComparison.Ordinal))
                {
                    patchReq["first_name"] = newFirst;
                    needsUpdate = true;
                    updateDetails.Add($"Ad ('{currentFirst}' -> '{newFirst}')");
                }

                string currentLast = (contact.LastName ?? "").Trim();
                string newLast = (record.LastName ?? "").Trim();
                if (!string.Equals(currentLast, newLast, StringComparison.Ordinal))
                {
                    patchReq["last_name"] = newLast;
                    needsUpdate = true;
                    updateDetails.Add($"Soyad ('{currentLast}' -> '{newLast}')");
                }

                int? currentCompanyId = contact.Company?.Id;
                if (companyId.HasValue && currentCompanyId != companyId.Value)
                {
                    patchReq["company_id"] = companyId.Value;
                    needsUpdate = true;
                    updateDetails.Add($"Firma ID ({currentCompanyId} -> {companyId.Value})");
                }

                if (needsUpdate)
                {
                    // Telefonları ve e-postaları PATCH gövdesine KOYMUYORUZ (Bölüm B bulgusu).
                    var patchResp = await client.PatchAsJsonAsync($"contacts/{contactId}", patchReq);
                    if (!patchResp.IsSuccessStatusCode)
                    {
                        string err = await patchResp.Content.ReadAsStringAsync();
                        throw new Exception($"Kişi güncelleme hatası: {err}");
                    }
                    status = "updated";
                    Console.WriteLine($"   [+] Bilgiler güncellendi: {string.Join(", ", updateDetails)}");
                }
            }
            else if (getResp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var postReq = new Dictionary<string, object>
                {
                    ["first_name"] = (record.FirstName ?? "").Trim(),
                    ["last_name"] = (record.LastName ?? "").Trim(),
                    ["external_id"] = record.CustomerId ?? ""
                };
                if (companyId.HasValue)
                {
                    postReq["company_id"] = companyId.Value;
                }

                var postResp = await client.PostAsJsonAsync("contacts", postReq);
                if (!postResp.IsSuccessStatusCode)
                {
                    string err = await postResp.Content.ReadAsStringAsync();
                    throw new Exception($"Kişi oluşturma hatası: {err}");
                }
                var body = await postResp.Content.ReadFromJsonAsync<HipcallResponse<Contact>>();
                contactId = body?.Data?.Id ?? throw new Exception("Kişi ID'si alınamadı.");
                status = "created";
                Console.WriteLine($"   [+] Yeni kişi oluşturuldu (ID: {contactId})");
            }
            else
            {
                string err = await getResp.Content.ReadAsStringAsync();
                throw new Exception($"Kişi sorgulama hatası: {getResp.StatusCode} - {err}");
            }

            if (!string.IsNullOrWhiteSpace(record.Phone))
            {
                string targetPhone = record.Phone.Trim();
                bool phoneExists = existingPhones.Any(p => CleanPhone(p.Number) == CleanPhone(targetPhone));
                
                if (!phoneExists)
                {
                    // Kişinin numarası değişmişse eski numaraları temizle (Bölüm B bulgusu)
                    foreach (var oldPhone in existingPhones)
                    {
                        if (!string.IsNullOrWhiteSpace(oldPhone.Number))
                        {
                            await client.DeleteAsync($"contacts/{contactId}/phones/{Uri.EscapeDataString(oldPhone.Number)}");
                        }
                    }

                    string countryCode = targetPhone.StartsWith("+1") ? "US" : "TR";
                    var phoneReq = new
                    {
                        phones = new[]
                        {
                            new
                            {
                                country = countryCode,
                                number = targetPhone
                            }
                        }
                    };

                    var phoneResp = await client.PostAsJsonAsync($"contacts/{contactId}/phones", phoneReq);
                    if (!phoneResp.IsSuccessStatusCode)
                    {
                        string err = await phoneResp.Content.ReadAsStringAsync();
                        throw new Exception($"Telefon ekleme hatası: {err}");
                    }
                    if (status == "unchanged") status = "updated";
                    Console.WriteLine($"   [+] Telefon senkronize edildi: {targetPhone}");
                }
            }

            if (status == "unchanged")
            {
                Console.WriteLine("   [=] Değişiklik yok (Kayıt güncel)");
            }

            return status;
        }

        static string CleanPhone(string? p)
        {
            if (string.IsNullOrEmpty(p)) return "";
            return new string(p.Where(char.IsDigit).ToArray());
        }
    }

    public class CrmRecord
    {
        public string? CustomerId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? CompanyId { get; set; }
    }

    public class HipcallResponse<T>
    {
        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    public class Company
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public class Contact
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        [JsonPropertyName("company")]
        public Company? Company { get; set; }

        [JsonPropertyName("phones")]
        public List<Phone>? Phones { get; set; }
    }

    public class Phone
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("number")]
        public string? Number { get; set; }
    }
}
