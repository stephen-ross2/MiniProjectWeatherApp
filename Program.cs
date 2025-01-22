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
                    await FetchWeatherData("metar"); //calls the FetchWeatherData method to fetch the METAR data via the URL below
                    break;

                case "2":
                    await FetchWeatherData("taf"); //calls the FetchWeatherData method to fetch the TAF data via the URL below
                    break;

                case "3": //ends the menu loop and exits the application
                    showMenu = false;
                    Console.WriteLine("Thank you for using the Aviation Planning App. Have a safe flight!");
                    break;

                default: //Error message for invalid input
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
        string airportCode = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(airportCode))
        {
            Console.WriteLine("Invalid input. Please enter at least one ICAO code.");
            return;
        }

        string baseUrl = "https://api.checkwx.com";
        string endpoint = type == "metar" ? "metar" : "taf";
        string url = $"{baseUrl}/{endpoint}/{airportCode}/decoded";

        using HttpClient client = new();
        client.DefaultRequestHeaders.Add("X-API-Key", ApiKey);

        try
        {
            Console.WriteLine($"\nRequest URL: {url}");
            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();

                // Process the weather data here
                try
                {
                    var jsonDocument = JsonDocument.Parse(responseBody);
                    ExtractWeatherDetails(jsonDocument, type); // Pass parsed JSON to decoding method
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error decoding JSON: {ex.Message}");
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


    static void ExtractWeatherDetails(JsonDocument jsonDocument, string type)
    {


        var data = jsonDocument.RootElement.GetProperty("data");
        foreach (var report in data.EnumerateArray())
        {
            string station = report.GetProperty("icao").GetString();
            string rawText = report.GetProperty("raw_text").GetString();
            Console.WriteLine($"Station: {station}");
            Console.WriteLine($"\n===== Raw {type.ToUpper()} =====\n{rawText}");
            Console.WriteLine($"\n===== Decoded {type.ToUpper()} =====");

            try
            {
                if (type == "metar")
                {
                    // METAR-specific decoding
                    DecodeMetarDetails(report);
                }
                else if (type == "taf")
                {
                    // TAF-specific decoding
                    DecodeTafDetails(report);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing decoded {type.ToUpper()} data: {ex.Message}");
            }
                      

            Console.WriteLine("\n========================================\n");
        }
    }

    static void DecodeMetarDetails(JsonElement report)
    {
        // Temperature and dewpoint
        if (report.TryGetProperty("temperature", out var temperatureElement))
        {
            var tempC = temperatureElement.GetProperty("celsius").GetDouble();
            Console.WriteLine($"  Temperature: {tempC}°C");
        }

        if (report.TryGetProperty("dewpoint", out var dewpointElement))
        {
            var dewC = dewpointElement.GetProperty("celsius").GetDouble();
            Console.WriteLine($"  Dewpoint: {dewC}°C");
        }

        // Wind
        if (report.TryGetProperty("wind", out var windElement))
        {
            var windDir = windElement.GetProperty("degrees").GetInt32();
            var windSpeedKts = windElement.GetProperty("speed_kts").GetInt32();
            Console.WriteLine($"  Wind: {windSpeedKts} knots from {windDir}°");
        }

        // Visibility
        if (report.TryGetProperty("visibility", out var visibilityElement))
        {
            var visibilityMiles = visibilityElement.GetProperty("miles_float").GetDouble();
            Console.WriteLine($"  Visibility: {visibilityMiles} miles");
        }

        // Altimeter
        if (report.TryGetProperty("barometer", out var barometerElement))
        {
            var altimeterHg = barometerElement.GetProperty("hg").GetDouble();
            Console.WriteLine($"  Altimeter: {altimeterHg} inHg");
        }

        // Cloud Ceilings
        if (report.TryGetProperty("clouds", out var cloudsElement) && cloudsElement.GetArrayLength() > 0)
        {
            Console.WriteLine("  Cloud Ceilings:");
            foreach (var cloud in cloudsElement.EnumerateArray())
            {
                var coverage = cloud.GetProperty("text").GetString();
                if (cloud.TryGetProperty("base_feet_agl", out var baseFeetElement))
                {
                    var baseFeet = baseFeetElement.GetInt32();
                    Console.WriteLine($"    - {coverage} at {baseFeet} feet AGL");
                }
                else
                {
                    Console.WriteLine($"    - {coverage} at an unknown height.");
                }
            }
        }
        else
        {
            Console.WriteLine("  Cloud Ceilings: No significant clouds.");
        }
    }

    static void DecodeTafDetails(JsonElement report)
    {
        Console.WriteLine("\nDecoded TAF Details:");

        if (report.TryGetProperty("forecast", out var forecastElement) && forecastElement.GetArrayLength() > 0)
        {
            Console.WriteLine("  Forecast Periods:");
            foreach (var period in forecastElement.EnumerateArray())
            {
                try
                {
                    // Extract start and end times from the "timestamp" object
                    var timestamp = period.GetProperty("timestamp");
                    string startTime = timestamp.GetProperty("from").GetString();
                    string endTime = timestamp.GetProperty("to").GetString();

                    Console.WriteLine($"    From {startTime} to {endTime}:");

                    // Weather conditions (optional)
                    if (period.TryGetProperty("change", out var changeElement))
                    {
                        var changeText = changeElement.GetProperty("indicator").GetProperty("text").GetString();
                        Console.WriteLine($"      Change: {changeText}");
                    }

                    // Wind
                    if (period.TryGetProperty("wind", out var windElement))
                    {
                        var windDir = windElement.GetProperty("degrees").GetInt32();
                        var windSpeedKts = windElement.GetProperty("speed_kts").GetInt32();
                        Console.WriteLine($"      Wind: {windSpeedKts} knots from {windDir}°");
                    }

                    // Visibility
                    if (period.TryGetProperty("visibility", out var visibilityElement))
                    {
                        var visibilityMiles = visibilityElement.GetProperty("miles").GetString();
                        Console.WriteLine($"      Visibility: {visibilityMiles}");
                    }

                    // Clouds
                    if (period.TryGetProperty("clouds", out var cloudsElement) && cloudsElement.GetArrayLength() > 0)
                    {
                        Console.WriteLine("      Clouds:");
                        foreach (var cloud in cloudsElement.EnumerateArray())
                        {
                            var coverage = cloud.GetProperty("text").GetString();
                            if (cloud.TryGetProperty("base_feet_agl", out var baseFeetElement))
                            {
                                var baseFeet = baseFeetElement.GetInt32();
                                Console.WriteLine($"        - {coverage} at {baseFeet} feet AGL");
                            }
                            else
                            {
                                Console.WriteLine($"        - {coverage}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("      Clouds: None reported.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"      Error processing period data: {ex.Message}");
                }
            }
        }
        else
        {
            Console.WriteLine("  No forecast data available.");
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

