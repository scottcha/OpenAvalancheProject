namespace GetData
{
    public interface IParser
    {
        AvalancheRegionForecast Parse(TextReader reader);
    }
}