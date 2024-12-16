using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace WeatherApp
{
    // Структура для хранения данных о погоде
    public struct Weather
    {
        public string Country { get; set; }
        public string Name { get; set; }
        public double Temp { get; set; }
        public string Description { get; set; }
    }

    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string apiKey = "3b7a51f32fe5d4163b1e1a04d899e5f5";

        static async Task Main(string[] args)
        {
            List<Weather> weatherData = new List<Weather>();

            // Получение данных о погоде для 50 случайных координат
            while (weatherData.Count < 50)
            {
                double lat = RandomDouble(-90, 90);
                double lon = RandomDouble(-180, 180);

                bool success = await FetchWeatherData(lat, lon, weatherData);
                if (!success)
                {
                    Console.WriteLine("Не удалось получить данные для текущих координат. Повторная попытка...");
                }
            }

            // Вывод данных на консоль
            Console.WriteLine("Страна с максимальной температурой: " + weatherData.OrderByDescending(w => w.Temp).First().Country);
            Console.WriteLine("Страна с минимальной температурой: " + weatherData.OrderBy(w => w.Temp).First().Country);
            Console.WriteLine("Средняя температура в мире: " + weatherData.Average(w => w.Temp));
            Console.WriteLine("Количество стран в коллекции: " + weatherData.Select(w => w.Country).Distinct().Count());

            var specificDescriptions = new[] { "clear sky", "rain", "few clouds" };
            var firstMatch = weatherData.FirstOrDefault(w => specificDescriptions.Contains(w.Description));
            if (firstMatch.Country != null)
            {
                Console.WriteLine($"Первая найденная страна и местность с описанием '{firstMatch.Description}': {firstMatch.Country}, {firstMatch.Name}");
            }
            else
            {
                Console.WriteLine("Не найдено данных с указанными описаниями погоды.");
            }
        }

        // Метод для генерации случайных дробных чисел в заданном диапазоне
        private static double RandomDouble(double min, double max)
        {
            Random random = new Random();
            return random.NextDouble() * (max - min) + min;
        }

        // Метод для получения данных о погоде по координатам
        private static async Task<bool> FetchWeatherData(double lat, double lon, List<Weather> weatherData)
        {
            string url = $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid={apiKey}&units=metric";

            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                // Логирование ответа API
                Console.WriteLine($"Ответ API для координат (lat: {lat}, lon: {lon}): {responseBody}");

                using JsonDocument doc = JsonDocument.Parse(responseBody);
                JsonElement root = doc.RootElement;

                // Проверка наличия ключей в JSON
                if (!root.TryGetProperty("sys", out JsonElement sys) ||
                    !sys.TryGetProperty("country", out JsonElement countryElement) ||
                    !root.TryGetProperty("name", out JsonElement nameElement) ||
                    !root.TryGetProperty("main", out JsonElement main) ||
                    !main.TryGetProperty("temp", out JsonElement tempElement) ||
                    !root.TryGetProperty("weather", out JsonElement weatherArray) ||
                    weatherArray.GetArrayLength() == 0 ||
                    !weatherArray[0].TryGetProperty("description", out JsonElement descriptionElement))
                {
                    Console.WriteLine("Некоторые ключи отсутствуют в ответе API. Повторная попытка...");
                    return false;
                }

                string country = countryElement.GetString();
                string name = nameElement.GetString();
                double temp = tempElement.GetDouble();
                string description = descriptionElement.GetString();

                if (!string.IsNullOrEmpty(country) && !string.IsNullOrEmpty(name))
                {
                    weatherData.Add(new Weather
                    {
                        Country = country,
                        Name = name,
                        Temp = temp,
                        Description = description
                    });
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении данных: {ex.Message}");
            }

            return false;
        }
    }
}