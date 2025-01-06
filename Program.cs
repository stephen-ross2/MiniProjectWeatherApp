using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    private const string ApiKey = "40cdff8569f044f79520c074dc"; //API key for CheckWX.com

    static async Task Main(string[] args)
    {
        bool showMenu = true;

        while (showMenu) //While loop to keep the menu running until the user chooses to exit
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

                case "3": //ends the menu loop and exits the application
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

    static async Task FetchWeatherData(string type) //FetchWeatherData method to pull weather data from the API
    {
        Console.WriteLine($"Enter the ICAO code(s) for {type.ToUpper()} (e.g., KJFK, KLAX). Separate multiple codes with a comma:"); 
        string airportCode = Console.ReadLine(); //Asks the user to input the airport identifier and stores it in the airportCode variable

        if (string.IsNullOrWhiteSpace(airportCode)) //Edge case to check if the user input is empty or incorrect. If it is, the code returns an error message and exits the method
        {
            Console.WriteLine("Invalid input. Please enter at least one ICAO code.");
            return;
        }

        string baseUrl = "https://api.checkwx.com"; //API Origin URL
        string endpoint = type == "metar" ? "metar" : "taf"; 
        string url = $"{baseUrl}/{endpoint}/{airportCode}/decoded"; //API URL to fetch the weather data based on the user input. "decoded" makes the data more readable and can be deleted for the raw text

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

                // Ask the user if they want to export the data to a file for viewing in Notepad++ for printing. 
                Console.WriteLine("Do you want to export the formatted JSON data to a file for viewing in Notepad++? (yes/no):");
                string exportChoice = Console.ReadLine()?.ToLower();

                if (exportChoice == "yes")
                {
                    await ExportJsonToNotepad(responseBody); // Exports the JSON file. 
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

    static async Task ExportJsonToNotepad(string json) //This method exports the JSON data to a file and opens it in Notepad++ for viewing and printing if needed. 
    {
        string fileName = "WeatherData.json";

        try
        {
           
            var jsonDocument = JsonDocument.Parse(json);
            var formattedJson = JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            
            await File.WriteAllTextAsync(fileName, formattedJson);
            Console.WriteLine($"Data successfully exported to {fileName}.");

            
            string notepadPlusPath = @"C:\Program Files\Notepad++\notepad++.exe"; //Default installation path for Notepad++ on my computer.

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

