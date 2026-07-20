using System.Threading.Tasks;

namespace IpLookupApi.Application;

public interface IIp2cService
{
    Task<Ip2cResult> GetCountryAsync(string ip);
}
