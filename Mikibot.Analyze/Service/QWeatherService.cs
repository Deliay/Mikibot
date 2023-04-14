using QWeatherAPI.Result.GeoAPI.CityLookup;
using QWeatherAPI.Result.WeatherDailyForecast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static QWeatherAPI.WeatherDailyForecastAPI;

namespace Mikibot.Analyze.Service
{
    public class QWeatherService
    {
        private readonly string _apiKey;
        public QWeatherService()
        {
            _apiKey = Environment.GetEnvironmentVariable("QWEATHER_API_KEY") ?? throw new Exception("Missing QWeather API key");
        }

        public async Task<GeoResult> SearchGeo(string keyword)
        {
            return await QWeatherAPI.GeoAPI.GetGeoAsync(keyword, _apiKey);
        }

        public async Task<Daily> GetTodayForecast(string locationId)
        {
            var forecasts = await GetWeatherDailyForecastAsync(locationId, _apiKey, dailyCount: DailyCount._3Day);
            return forecasts.Daily[^1];
        }

        public async Task<(Location, Daily)> SearchTodayForecast(string keyword)
        {
            try
            {
                var locations = (await SearchGeo(keyword)).Locations;
                if (locations is not null && locations.Length > 0)
                {
                    var geo = locations[^1];
                    var forecasts = await GetWeatherDailyForecastAsync(geo.ID, _apiKey, dailyCount: DailyCount._3Day);
                    return (geo, forecasts.Daily[^1]);
                }
                return (null!, null!);
            }
            catch
            {
                return (null!, null!);
            }
        }
    }
}
