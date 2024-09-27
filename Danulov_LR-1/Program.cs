using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Data.SQLite;
using System.IO;
using System.Xml.Linq;

namespace AgifyApiDemo
{
    class Program
    {
        public struct ApiResponse
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("age")]
            public int Age { get; set; }

            [JsonPropertyName("count")]
            public int Count { get; set; }
        }
        
        private static string _apiUrl = "https://api.agify.io?name=";
        private static string _dbFileName = "response.db";
        
        private static async Task Main(string[] args)
        {
          await GetName();
        }

        public static async Task GetName()
        {
            Console.WriteLine("Write name: ");
            string name = Console.ReadLine();
            if (name != null)
            {
                await GetResponse(name);
            }
        }

        public static async Task GetResponse(string name)
        {
            try
            {
                var response = await GetApiResponse(_apiUrl + name);

                if (!File.Exists(_dbFileName))
                {
                    CreateTable();
                }
                if (response.Age > 0)
                {
                    SaveDataToDatabase(response.Name, response.Age, response.Count);

                    GetAndPrintDataFromDatabase(response.Name);
                }
                else
                {
                    Console.WriteLine("Age can`t be lower then 0");
                }
            }
            catch (HttpRequestException)
            {
                Console.WriteLine("Unable to connect to the internet. Trying to retrieve data from the database...");
                GetAndPrintDataFromDatabase(name);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public static async Task<ApiResponse> GetApiResponse(string apiUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Response: {responseBody}");
                ApiResponse apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseBody);

                return apiResponse;
            }
        }

        public static void CreateTable()
        {
            using (SQLiteConnection _connection = new SQLiteConnection("Data Source=response.db;Version=3;"))
            {
                _connection.Open();

                string createTableQuery = @"CREATE TABLE IF NOT EXISTS ResponseResults (
                                            Name TEXT PRIMARY KEY,
                                            Age INTEGER,
                                            Count INTEGER)";
                SQLiteCommand command = new SQLiteCommand(createTableQuery, _connection);
                command.ExecuteNonQuery();

                _connection.Close();
            }
        }

        public static void SaveDataToDatabase(string name, int age, int count)
        {
            using (SQLiteConnection _connection = new SQLiteConnection("Data Source=response.db;Version=3;"))
            {
                _connection.Open();

                string insertQuery = "INSERT INTO ResponseResults (Name, Age, Count) VALUES (@name, @age, @count)";
                SQLiteCommand command = new SQLiteCommand(insertQuery, _connection);
                command.Parameters.AddWithValue("@name", name);
                command.Parameters.AddWithValue("@age", age);
                command.Parameters.AddWithValue("@count", count);

                command.ExecuteNonQuery();

                _connection.Close();
            }
        }

        public static void GetAndPrintDataFromDatabase(string Name)
        {
            using (SQLiteConnection _connection = new SQLiteConnection("Data Source=response.db;Version=3;"))
            {
                _connection.Open();

                string selectQuery = "SELECT* FROM ResponseResults WHERE Name = @Name";
                SQLiteCommand command = new SQLiteCommand(selectQuery, _connection);
                command.Parameters.AddWithValue("@Name", Name);

                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string name = reader["Name"].ToString();
                        int age = Convert.ToInt32(reader["Age"]);
                        int count = Convert.ToInt32(reader["Count"]);

                        Console.WriteLine($"Name: {name}, Age: {age}, Count: {count}");
                    }
                    else
                    {
                        Console.WriteLine("Database have no such data");
                    }
                }

                _connection.Close();
            }
        }
    }
}
