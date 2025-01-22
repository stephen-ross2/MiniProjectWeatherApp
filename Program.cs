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
            Console.WriteLine("======= AVIATION WEATHER PLANNING APP =======");
            Console.ResetColor(); // Reset the color to default after the title
            Console.WriteLine();
            Console.WriteLine("1. Check METARs and TAFs for an airport");
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

        foreach (string code in airportCodes.Split(','))
        {
            // Loop through METAR and TAF separately
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

                            // Extract the airport name and location
                            var airportData = jsonDocument.RootElement.GetProperty("data")[0];
                            var station = airportData.GetProperty("station");
                            var airportName = station.GetProperty("name").GetString();
                            var location = station.GetProperty("location").GetString();

                            // Set font color to Cyan for airport and location
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            if (type == "metar")
                            {
                                Console.WriteLine($"\n\n\nAirport: {airportName} - Location: {location}");
                                Console.WriteLine();
                            }
                            // Reset the color back to default
                            Console.ResetColor();

                            // Fetch and decode METAR/TAF details based on the type
                            ExtractWeatherDetails(jsonDocument, type);

                            // Add separator after TAF section
                            if (type == "taf")
                            {
                                Console.WriteLine("___________________________________________________________________________________________________________________________________________________________________________");
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
    }


    static void ExtractWeatherDetails(JsonDocument jsonDocument, string type)
    {


        var data = jsonDocument.RootElement.GetProperty("data");
        foreach (var report in data.EnumerateArray())
        {
            Console.WriteLine($"\n\n========= {type.ToUpper()} =========");
            Console.WriteLine($"\n\nRaw: {report.GetProperty("raw_text").GetString()}");

            if (type == "metar")
            {
                DecodeMetarDetails(report);
            }
            else if (type == "taf")
            {
                DecodeTafDetails(report);
            }

        }
    }

    static void DecodeMetarDetails(JsonElement report)
    {
        Console.WriteLine("\nDecoded METAR Details:");

        // Observation time
        if (report.TryGetProperty("observed", out var observedTime))
        {
            DateTime observationDateTime = DateTime.Parse(observedTime.GetString());
            string formattedDate = observationDateTime.ToString("MMM dd yyyy HH:mm 'UTC'");
            Console.WriteLine($"  Observation Time: {formattedDate}");
        }
        else
        {
            Console.WriteLine("  Observation Time: Not available.");
        }

        // Temperature
        if (report.TryGetProperty("temperature", out var temp))
        {
            double tempCelsius = temp.GetProperty("celsius").GetDouble();
            double tempFahrenheit = tempCelsius * 9 / 5 + 32;
            Console.WriteLine($"  Temperature: {tempCelsius}°C ({tempFahrenheit:F1}°F)");
        }

        // Dewpoint
        if (report.TryGetProperty("dewpoint", out var dewpoint))
        {
            double dewCelsius = dewpoint.GetProperty("celsius").GetDouble();
            double dewFahrenheit = dewCelsius * 9 / 5 + 32;
            Console.WriteLine($"  Dewpoint: {dewCelsius}°C ({dewFahrenheit:F1}°F)");
        }

        // Wind
        if (report.TryGetProperty("wind", out var wind))
        {
            Console.WriteLine($"  Wind: {wind.GetProperty("degrees").GetInt32():D3}° @ {wind.GetProperty("speed_kts").GetInt32()} knots");
        }

        // Visibility
        if (report.TryGetProperty("visibility", out var visibility))
        {
            Console.WriteLine($"  Visibility: {visibility.GetProperty("miles_float").GetDouble()} miles");
        }

        // Barometer (Altimeter)
        if (report.TryGetProperty("barometer", out var barometer) && barometer.ValueKind == JsonValueKind.Object)
        {
            if (barometer.TryGetProperty("hg", out var barometerHg))
            {
                double barometerInHg = barometerHg.GetDouble();
                Console.WriteLine($"  Altimeter: {barometerInHg:F2} inHg");
            }
            else
            {
                Console.WriteLine("  Altimeter: 'hg' property not found.");
            }
        }
        else
        {
            Console.WriteLine("  Altimeter: Data not available.");
        }

        // Ceiling
        if (report.TryGetProperty("clouds", out var clouds) && clouds.GetArrayLength() > 0)
        {
            foreach (var cloud in clouds.EnumerateArray())
            {
                var coverage = cloud.GetProperty("text").GetString();
                if (cloud.TryGetProperty("base_feet_agl", out var baseFeet))
                {
                    Console.WriteLine($"  Ceiling: {coverage} at {baseFeet.GetInt32()}ft AGL");
                }
                else
                {
                    Console.WriteLine($"  Ceiling: {coverage} at cloud base height not available.");
                }
            }
        }
        else
        {
            Console.WriteLine("  Ceiling: No data available.");
        }
    }

    static void DecodeTafDetails(JsonElement report)
    {
        Console.WriteLine("\nDecoded TAF Details:");

        if (report.TryGetProperty("forecast", out var forecasts))
        {
            foreach (var forecast in forecasts.EnumerateArray())
            {
                // Timestamp
                if (forecast.TryGetProperty("timestamp", out var timestamp))
                {
                    string fromTime = timestamp.GetProperty("from").GetString();
                    string toTime = timestamp.GetProperty("to").GetString();
                    DateTime fromDateTime = DateTime.Parse(fromTime);
                    DateTime toDateTime = DateTime.Parse(toTime);
                    string formattedFromTime = fromDateTime.ToString("MMM dd yyyy HH:mm 'UTC'");
                    string formattedToTime = toDateTime.ToString("MMM dd yyyy HH:mm 'UTC'");

                    // Set the forecast period to yellow color
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  \nForecast: From {formattedFromTime} to {formattedToTime}");
                    Console.ResetColor(); // Reset to default color for subsequent text
                }

                // Wind
                if (forecast.TryGetProperty("wind", out var wind))
                {
                    int windDirection = wind.GetProperty("degrees").GetInt32();
                    int windSpeed = wind.GetProperty("speed_kts").GetInt32();
                    string formattedWindDirection = windDirection.ToString("D3");
                    Console.WriteLine($"\n    Wind: {formattedWindDirection} @ {windSpeed} knots");
                }

                // Visibility
                if (forecast.TryGetProperty("visibility", out var visibility))
                {
                    Console.WriteLine($"    Visibility: {visibility.GetProperty("miles").GetString()} miles");
                }

                // Ceiling
                if (forecast.TryGetProperty("clouds", out var clouds) && clouds.GetArrayLength() > 0)
                {
                    foreach (var cloud in clouds.EnumerateArray())
                    {
                        var coverage = cloud.GetProperty("text").GetString();
                        if (cloud.TryGetProperty("base_feet_agl", out var baseFeet))
                        {
                            Console.WriteLine($"    Ceiling: {coverage} at {baseFeet.GetInt32()}ft AGL");
                        }
                        else
                        {
                            Console.WriteLine($"    Ceiling: {coverage} at cloud base height not available.");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("    Ceiling: No data available.");
                }
            }
        }
    }
}










