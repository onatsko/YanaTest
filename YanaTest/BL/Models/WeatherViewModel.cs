namespace YanaTest.BL.Models;

public class WeatherViewModel
{
    public static readonly WeatherViewModel NotFound = new WeatherViewModel() { Date = new DateTime(1900, 1, 1) };
    
    public DateTime Date { get; set; }
    public string Description { get; set; } = "";
    public decimal Temp { get; set; }
    public string ImageBase64 { get; set; } = "";

    
    protected bool Equals(WeatherViewModel other)
    {
        return Date.Equals(other.Date) && Description == other.Description && Temp == other.Temp && ImageBase64 == other.ImageBase64;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((WeatherViewModel)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Date, Description, Temp, ImageBase64);
    }
}