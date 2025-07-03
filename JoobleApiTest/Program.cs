using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using Newtonsoft.Json;
using System.Data.SqlClient;
using Microsoft.VisualBasic;


namespace JoobleApiTest
{
    public class Job
    {
        public string title { get; set; }
        public string location { get; set; }
        public string snippet { get; set; }
        public string salary { get; set; }
        public string source { get; set; }
        public string link { get; set; }
        public string updated { get; set; }
        public string type { get; set; }
    }

    public class ApiResponse
    {
        public List<Job>? jobs { get; set; }
    }

    class Program
    {
        static async Task Main()
        {
            string keyVaultUrl = "https://jobapp-cloud.vault.azure.net/";
            string secretName = "Dbpw";  // This is the name of the secret you created in Key Vault

            // 🌐 2. Create Key Vault client using Azure CLI credentials
            var client = new SecretClient(new Uri(keyVaultUrl), new AzureCliCredential());

            // 🔑 3. Retrieve the database password from Key Vault
            KeyVaultSecret secret = await client.GetSecretAsync(secretName);
            string dbPassword = secret.Value;

            // 💾 4. SQL Server connection string using the retrieved password
            string connectionString = $"Server=tcp:jobappdb-server.database.windows.net,1433;" +
                                      $"Initial Catalog=JobAppDB;Persist Security Info=False;" +
                                      $"User ID=dbadmin;Password={dbPassword};" +
                                      $"MultipleActiveResultSets=False;Encrypt=True;" +
                                      $"TrustServerCertificate=False;Connection Timeout=30;";

            // 🌍 5. Jooble API endpoint and query
            string url = "https://jooble.org/api/c57d7b28-9141-499a-b08b-59ae038cbeb1";
            string json = @"{
                ""keywords"": ""software engineer"",
                ""location"": ""Berlin""
            }";

            // 🌐 6. Create and send HTTP POST request
            using (HttpClient httpClient = new HttpClient())
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    // 📥 7. Deserialize API response into objects
                    string result = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<ApiResponse>(result);

                    // 🔌 8. Open SQL connection
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        await conn.OpenAsync();

                        // 🧾 9. Loop through job results and insert into DB
                        foreach (var job in data.jobs)
                        {
                            string query = @"
                                IF NOT EXISTS (SELECT 1 FROM jobapp.joblist WHERE Link = @Link)
                                INSERT INTO jobapp.joblist 
                                    (Title, Location, Snippet, Salary, Source, Link, Updated, JobType)
                                VALUES
                                    (@Title, @Location, @Snippet, @Salary, @Source, @Link, @Updated, @JobType)";

                            using (SqlCommand cmd = new SqlCommand(query, conn))
                            {
                                // Avoid SQL injection and handle NULL values with AddWithValue
                                cmd.Parameters.AddWithValue("@Title", job.title ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Location", job.location ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Snippet", job.snippet ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Salary", job.salary ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Source", job.source ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Link", job.link ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Updated", job.updated ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@JobType", job.type ?? (object)DBNull.Value);

                                // 💾 Execute insert
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        Console.WriteLine("Job data inserted successfully.");
                    }
                }
                else
                {
                    Console.WriteLine($"API call failed: {response.StatusCode}");
                }
            }
        }


    }
}