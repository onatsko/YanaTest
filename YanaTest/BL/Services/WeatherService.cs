using Serilog;
using YanaTest.BL.Models;
using YanaTest.BL.Models.OpenWeatherMapModels;

namespace YanaTest.BL.Services;

public interface IWeatherService
{
    Task<List<WeatherViewModel>> GetForecastWeatherOfPoltavaAsync(DateTime from, DateTime to);
}

public class WeatherService : IWeatherService
{
    private const string WeatherApiKey = "1420cf64dca72ede8e1443e734ae5682";
    private const string WeatherApiPrefix = "data/2.5/";
    private const int CityPoltavaId = 696643;
    
    private readonly HttpClient _httpClient;
    private readonly HttpClient _httpClientPro;

    public WeatherService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("weather_api");
        _httpClientPro = httpClientFactory.CreateClient("weather_pro");
    }

    private string CreateCurrentUriForCityId(int cityId)
    {
        var finalUri = $"{_httpClient.BaseAddress}{WeatherApiPrefix}weather?appid={WeatherApiKey}&id={cityId}&units=metric&lang=ua";
        return finalUri;
    }

    private string CreateForecastUriForCityId(int cityId)
    {
        var finalUri = $"{_httpClient.BaseAddress}{WeatherApiPrefix}forecast?appid={WeatherApiKey}&id={cityId}&units=metric&lang=ua";
        return finalUri;
    }
    
    private string CreateProForecastUriForCityId(int cityId)
    {
        var finalUri = $"{_httpClientPro.BaseAddress}{WeatherApiPrefix}forecast/climate?appid={WeatherApiKey}&id={cityId}&units=metric&lang=ua";
        return finalUri;
    }

    private string CreateCurrentUriForIcon(string icon)
    {
        var finalUri = _httpClient.BaseAddress!.ToString().Replace(@"https://api.", @"http://") + $"img/wn/{icon}.png";
        return finalUri;
    }

    private async Task<T?> SendRequest<T>(HttpRequestMessage request)
    {
        using var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }

        return await response.Content.ReadFromJsonAsync<T>();
    }
    
    
    private async Task<T?> SendProRequest<T>(HttpRequestMessage request)
    {
        using var response = await _httpClientPro.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }

        return await response.Content.ReadFromJsonAsync<T>();
    }

    private async Task<byte[]> GetIconAsync(string iconName)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, CreateCurrentUriForIcon(iconName));
            using var response = await _httpClient.SendAsync(request);

            // throw exception on error response
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception(error);
            }

            byte[] content = await response.Content.ReadAsByteArrayAsync();

            return content;
        }
        catch (Exception ex)
        {
            Log.Warning($"Weather GetIcon: {ex.Message}");
            return [];
        }
    }

    public async Task<List<WeatherViewModel>> GetForecastWeatherOfPoltavaAsync(DateTime from, DateTime to)
    {
        try
        {
            var dateList = new List<string>();
            var daysCount = (to - from).Days;
            for (int i = 0; i <= daysCount; i++)
            {
                var date = from.AddDays(i).Date;

                dateList.Add(date.ToString("yyyy-MM-dd 09:00:00")); //6 ранку +3 від UTC буде 12:00 дня
            }


            var request = new HttpRequestMessage(HttpMethod.Get, CreateForecastUriForCityId(CityPoltavaId));
            var weather = await SendRequest<ForecastAnswer>(request);

            if (weather is null)
            {
                return new List<WeatherViewModel>();
            }
            
            var result = new List<WeatherViewModel>();

            foreach (var date in dateList)
            {
                if (weather.list.ToList().Select(x => x.dt_txt).Contains(date))
                {
                    var element = weather.list.ToList().Find(x => x.dt_txt == date);
                    if (element is null)
                        continue;
                    
                    var iconBase64 = "";
                    var weatherIconName = element.weather[0].icon;
                    if (!string.IsNullOrWhiteSpace(weatherIconName))
                    {
                        var iconArray = await GetIconAsync(weatherIconName);

                        var base64 = Convert.ToBase64String(iconArray);
                        iconBase64 = $"data:image/png;base64,{base64}";
                    }

                    var dayForecast = new WeatherViewModel()
                    {
                        Date = UnixTimeStampToDateTime(element.dt),
                        Temp = (decimal)element.main.temp,
                        Description = element.weather[0].description,
                        ImageBase64 = iconBase64
                    };

                    result.Add(dayForecast);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            Log.Warning($"Forecast Weather Of Poltava: {ex.Message}");
            return new List<WeatherViewModel>();
        }
    }

    private DateTime UnixTimeStampToDateTime(double unixTimeStamp)
    {
        // Unix timestamp is seconds past epoch
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
        return dateTime;
    }
}