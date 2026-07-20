using System.Threading.Tasks;

public interface IIp2cService
{
    Task<Ip2cResult> GetCountryAsync(string ip);
}
