using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    private const string ApiKey = "40cdff8569f044f79520c074dc";

    static async Task Main(string[] args)
    {
        bool showMenu = true;

        while (showMenu)
        {
            Console.Clear();
            Console.WriteLine("=======WELCOME TO THE AVIATION PLANNING APP=======");
            Console.WriteLine();
            Console.WriteLine("Please select an option from the menu below:");
            Console.WriteLine("1. Check current weather conditions at an airport (METARs)");
            Console.WriteLine("2. Check forecasted weather conditions at an airport (TAFs)");
            Console.WriteLine("3. Exit the application");
            Console.WriteLine();

            string userSelection = Console.ReadLine();

            switch (userSelection)
            {
                case "1":
                    await FetchWeatherData("metar");
                    break;

                case "2":
                    await FetchWeatherData("taf");
                    break;

                case "3":
                    showMenu = false;
                    Console.WriteLine("Thank you for using the Aviation Planning App. Have a safe flight!");
                    break;

                default:
                    Console.WriteLine("Invalid selection. Please select a valid option from the menu.");
                    break;
            }

            if (showMenu)
            {
                Console.WriteLine("\nPress any key to return to the menu...");
                Console.ReadKey();
            }
        }
    }

    static async Task FetchWeatherData(string type)
    {
        Console.WriteLine($"Enter the ICAO code(s) for {type.ToUpper()} (e.g., KJFK, KLAX). Separate multiple codes with a comma:");
        string codes = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(codes))
        {
            Console.WriteLine("Invalid input. Please enter at least one ICAO code.");
            return;
        }

        string baseUrl = "https://api.checkwx.com";
        string endpoint = type == "metar" ? "metar" : "taf";
        string url = $"{baseUrl}/{endpoint}/{codes}/decoded";

        using HttpClient client = new();
        client.DefaultRequestHeaders.Add("X-API-Key", ApiKey);

        try
        {
            Console.WriteLine($"\nRequest URL: {url}");
            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();

                var jsonDocument = JsonDocument.Parse(responseBody);
                var formattedJson = JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                Console.WriteLine("\n===== Weather Data (Formatted JSON) =====\n");
                Console.WriteLine(formattedJson);
                Console.WriteLine("\n========================================\n");

                // Ask the user if they want to export the data
                Console.WriteLine("Do you want to export the formatted JSON data to a file for viewing in Notepad++? (yes/no):");
                string exportChoice = Console.ReadLine()?.ToLower();

                if (exportChoice == "yes")
                {
                    await ExportJsonToNotepad(responseBody); // Export the raw JSON
                }
                else
                {
                    Console.WriteLine("Export skipped.");
                }
            }
            else
            {
                Console.WriteLine($"Failed to fetch {type.ToUpper()}. Status Code: {response.StatusCode}");
                string errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response Content: {errorContent}");
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP Request Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching {type.ToUpper()}: {ex.Message}");
        }
    }

    static async Task ExportJsonToNotepad(string json)
    {
        string fileName = "WeatherData.json";

        try
        {
            // Format the JSON
            var jsonDocument = JsonDocument.Parse(json);
            var formattedJson = JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Save the formatted JSON to a file
            await File.WriteAllTextAsync(fileName, formattedJson);
            Console.WriteLine($"Data successfully exported to {fileName}.");

            // Open the file in Notepad++
            string notepadPlusPath = @"C:\Program Files\Notepad++\notepad++.exe";

            if (File.Exists(notepadPlusPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = notepadPlusPath,
                    Arguments = fileName,
                    UseShellExecute = false
                });
                Console.WriteLine("Notepad++ has been launched to view the exported file.");
            }
            else
            {
                Console.WriteLine("Notepad++ is not found at the default location. Please check your installation.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exporting data to file: {ex.Message}");
        }
    }
}

