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

        while (showMenu)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("======= FLIGHT FORECASTS =======");
            Console.ResetColor(); // Reset the color to default after the title
            Console.WriteLine("\nPlease select an option below by typing the number and pressing ENTER:");
            Console.WriteLine();
            Console.WriteLine("1. Check current and forecasted weather conditions at an airport (METARs / TAFs)");
            Console.WriteLine("2. Exit");
            Console.WriteLine();

            string userSelection = Console.ReadLine();

            switch (userSelection)
            {
                case "1":
                    Console.WriteLine("Enter the ICAO code(s) (e.g., KJFK, KLAX). Separate multiple codes with a comma:");
                    string airportCodes = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(airportCodes))
                    {
                        Console.WriteLine("Invalid input. Please enter at least one ICAO code.");
                    }
                    else
                    {
                        await FetchAndDisplayWeatherData(airportCodes);
                    }
                    break;

                case "2":
                    showMenu = false;
                    Console.WriteLine("Thank you for using the Aviation Planning App. Safe travels!");
                    break;

                default:
                    Console.WriteLine("Invalid selection. Please try again.");
                    break;
            }

            if (showMenu)
            {
                Console.WriteLine("\nPress any key to return to the menu...");
                Console.ReadKey();
            }
        }
    }

    static async Task FetchAndDisplayWeatherData(string airportCodes)
    {
        string baseUrl = "https://api.checkwx.com";
        string dataToSave = ""; // This will accumulate the data that we'll potentially save to file

        // Split the input for multiple codes
        foreach (string code in airportCodes.Split(','))
        {
            foreach (string type in new[] { "metar", "taf" })
            {
                string endpoint = $"{baseUrl}/{type}/{code}/decoded";

                using HttpClient client = new();
                client.DefaultRequestHeaders.Add("X-API-Key", ApiKey);

                try
                {
                    HttpResponseMessage response = await client.GetAsync(endpoint);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();

                        try
                        {
                            var jsonDocument = JsonDocument.Parse(responseBody);
                            var airportData = jsonDocument.RootElement.GetProperty("data")[0];
                            var station = airportData.GetProperty("station");
                            var airportName = station.GetProperty("name").GetString();
                            var location = station.GetProperty("location").GetString();

                            // Display the airport details once, before showing METAR/TAF data
                            if (dataToSave == "")  // Only show once for the first report
                            {
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine($"\n\nAirport: {airportName} - Location: {location}");
                                Console.ResetColor();
                                dataToSave += $"\nAirport: {airportName} - Location: {location}\n\n"; // Save airport info
                            }

                            // Display and accumulate METAR/TAF details
                            string weatherDetails = ExtractWeatherDetails(jsonDocument, type);
                            Console.WriteLine(weatherDetails);
                            dataToSave += weatherDetails; // Accumulate to be saved to file

                            if (type == "taf")
                            {
                                Console.WriteLine("___________________________________________________________________________________________________________________________________________________________________________");
                                dataToSave += "\n___________________________________________________________________________________________________________________________________________________________________________\n";
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error decoding {type.ToUpper()} JSON: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to fetch {type.ToUpper()}. Status Code: {response.StatusCode}");
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
        }

        // Prompt for saving the data
        Console.WriteLine("\nWould you like to save this weather data to a file? (y/n)");
        string saveResponse = Console.ReadLine()?.ToLower();

        if (saveResponse == "y")
        {
            Console.WriteLine("\nEnter the filename (without extension): ");
            string fileName = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(fileName))
            {
                Console.WriteLine("Invalid filename. Using default name 'WeatherData'.");
                fileName = "WeatherData";  // Set a default filename if the input is invalid.
            }

            // Call SaveDataToFile with the user-provided filename
            await SaveDataToFile(fileName, dataToSave);
            Console.WriteLine("Data has been saved to your desktop.");
        }
    }


    static string ExtractWeatherDetails(JsonDocument jsonDocument, string type)
    {
        var data = jsonDocument.RootElement.GetProperty("data");
        string details = ""; // Store the raw and decoded data

        foreach (var report in data.EnumerateArray())
        {
            details += $"\n\n========= {type.ToUpper()} =========";
            details += $"\nRaw: {report.GetProperty("raw_text").GetString()}";

            if (type == "metar")
            {
                details += DecodeMetarDetailsToString(report);
            }
            else if (type == "taf")
            {
                details += DecodeTafDetailsToString(report);
            }
        }

        return details;
    }

    static string DecodeMetarDetailsToString(JsonElement report)
    {
        string details = "\n\nDecoded METAR Details:";

        if (report.TryGetProperty("observed", out var observedTime))
        {
            DateTime observationDateTime = DateTime.Parse(observedTime.GetString());
            string formattedDate = observationDateTime.ToString("MMM dd yyyy HH:mm 'UTC'");
            details += $"\n\n  Observation Time: {formattedDate}";
        }

        if (report.TryGetProperty("temperature", out var temp))
        {
            double tempCelsius = temp.GetProperty("celsius").GetDouble();
            double tempFahrenheit = tempCelsius * 9 / 5 + 32;
            details += $"\n  Temperature: {tempCelsius}°C ({tempFahrenheit:F1}°F)";
        }

        if (report.TryGetProperty("dewpoint", out var dewpoint))
        {
            double dewCelsius = dewpoint.GetProperty("celsius").GetDouble();
            double dewFahrenheit = dewCelsius * 9 / 5 + 32;
            details += $"\n  Dewpoint: {dewCelsius}°C ({dewFahrenheit:F1}°F)";
        }

        if (report.TryGetProperty("wind", out var wind))
        {
            details += $"\n  Wind: {wind.GetProperty("degrees").GetInt32():D3}° @ {wind.GetProperty("speed_kts").GetInt32()} knots";
        }

        if (report.TryGetProperty("visibility", out var visibility))
        {
            details += $"\n  Visibility: {visibility.GetProperty("miles_float").GetDouble()} miles";
        }

        if (report.TryGetProperty("barometer", out var barometer) && barometer.ValueKind == JsonValueKind.Object)
        {
            if (barometer.TryGetProperty("hg", out var barometerHg))
            {
                double barometerInHg = barometerHg.GetDouble();
                details += $"\n  Altimeter: {barometerInHg:F2} inHg";
            }
            else
            {
                details += "\n  Altimeter: 'hg' property not found.";
            }
        }
        else
        {
            details += "\n  Altimeter: Data not available.";
        }

        if (report.TryGetProperty("clouds", out var clouds) && clouds.GetArrayLength() > 0)
        {
            foreach (var cloud in clouds.EnumerateArray())
            {
                var coverage = cloud.GetProperty("text").GetString();
                if (cloud.TryGetProperty("base_feet_agl", out var baseFeet))
                {
                    details += $"\n  Ceiling: {coverage} at {baseFeet.GetInt32()}ft AGL";
                }
                else
                {
                    details += $"\n  Ceiling: {coverage} at cloud base height not available.";
                }
            }
        }
        else
        {
            details += "\n  Ceiling: No data available.";
        }

        return details;
    }

    static string DecodeTafDetailsToString(JsonElement report)
    {
        string details = "\n\nDecoded TAF Details:";

        if (report.TryGetProperty("forecast", out var forecasts))
        {
            foreach (var forecast in forecasts.EnumerateArray())
            {
                if (forecast.TryGetProperty("timestamp", out var timestamp))
                {
                    string fromTime = timestamp.GetProperty("from").GetString();
                    string toTime = timestamp.GetProperty("to").GetString();
                    DateTime fromDateTime = DateTime.Parse(fromTime);
                    DateTime toDateTime = DateTime.Parse(toTime);
                    details += $"\n\n  Forecast: From {fromDateTime:MMM dd yyyy HH:mm 'UTC'} to {toDateTime:MMM dd yyyy HH:mm 'UTC'}";
                }

                if (forecast.TryGetProperty("wind", out var wind))
                {
                    int windDirection = wind.GetProperty("degrees").GetInt32();
                    int windSpeed = wind.GetProperty("speed_kts").GetInt32();
                    string formattedWindDirection = windDirection.ToString("D3");
                    details += $"\n\n    Wind: {formattedWindDirection} @ {windSpeed} knots";
                }

                if (forecast.TryGetProperty("visibility", out var visibility))
                {
                    details += $"\n    Visibility: {visibility.GetProperty("miles").GetString()} miles";
                }

                if (forecast.TryGetProperty("clouds", out var clouds) && clouds.GetArrayLength() > 0)
                {
                    foreach (var cloud in clouds.EnumerateArray())
                    {
                        var coverage = cloud.GetProperty("text").GetString();
                        if (cloud.TryGetProperty("base_feet_agl", out var baseFeet))
                        {
                            details += $"\n    Ceiling: {coverage} at {baseFeet.GetInt32()}ft AGL";
                        }
                        else
                        {
                            details += $"\n    Ceiling: {coverage} at cloud base height not available.";
                        }
                    }
                }
                else
                {
                    details += "\n    Ceiling: No data available.";
                }
            }
        }

        return details;
    }

    static async Task SaveDataToFile(string fileName, string data)
    {
        // Ensure the filename doesn't contain any invalid characters
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_'); // Replace invalid characters with an underscore
        }

        string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName + ".txt");

        using (StreamWriter writer = new StreamWriter(filePath, false))
        {
            await writer.WriteAsync(data);
        }
    }
}












