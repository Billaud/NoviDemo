using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace IpLookupApi.Application;

// CQRS query: "δώσε μου info για αυτό το IP". Ίδια ακριβώς λογική με το παλιό
// IpLookupService (cache -> db -> IP2C -> persist -> cache), μόνο που τώρα το
// entry point είναι ένα MediatR request αντί για custom service interface.
public sealed record GetIpInfoQuery(string Ip) : IRequest<IpInfoResponse>;

public sealed class GetIpInfoQueryHandler : IRequestHandler<GetIpInfoQuery, IpInfoResponse>
{
    private readonly ILogger<GetIpInfoQueryHandler> _logger;
    private readonly IIpRepository _ipRepository;
    private readonly ICountryRepository _countryRepository;
    private readonly IIp2cService _ip2cService;
    private readonly ICacheService _cacheService;

    public GetIpInfoQueryHandler(
        ILogger<GetIpInfoQueryHandler> logger,
        IIpRepository ipRepository,
        ICountryRepository countryRepository,
        IIp2cService ip2cService,
        ICacheService cacheService)
    {
        _logger = logger;
        _ipRepository = ipRepository;
        _countryRepository = countryRepository;
        _ip2cService = ip2cService;
        _cacheService = cacheService;
    }

    public async Task<IpInfoResponse> Handle(GetIpInfoQuery request, CancellationToken cancellationToken)
    {
        // Το "IP δεν είναι κενό" ελέγχεται ήδη νωρίτερα από το ValidationBehavior
        // (GetIpInfoQueryValidator) πριν καν φτάσει εδώ. Το IpAddress παρακάτω κάνει
        // δικό του, deeper validation (σωστό IPv4 format) - domain invariant, ισχύει
        // πάντα, όχι μόνο μέσω MediatR pipeline.
        var ip = request.Ip;
        var ipAddress = new IpAddress(ip);
        var cacheKey = ipAddress.NumericValue.ToString();

        // 1. Cache
        var cached = await _cacheService.GetAsync<IpInfoResponse>(cacheKey);
        if (cached != null)
        {
            _logger.LogInformation("IP {Ip} found in cache", ip);
            return cached;
        }

        // 2. Database
        var existingIp = await _ipRepository.GetByAddressAsync(ipAddress);
        if (existingIp != null)
        {
            var country = await _countryRepository.GetByCodeAsync(existingIp.CountryTwoLetterCode);
            if (country != null)
            {
                var response = new IpInfoResponse(
                    country.TwoLetterCode,
                    country.ThreeLetterCode,
                    country.CountryName);

                await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromHours(24));
                return response;
            }

            _logger.LogWarning(
                "IP {Ip} found in DB but referenced country {Code} is missing. Falling back to IP2C.",
                ip, existingIp.CountryTwoLetterCode);
        }

        // 3. IP2C (external call)
        _logger.LogInformation("Calling IP2C for {Ip}", ip);

        Ip2cResult ip2cResult;
        try
        {
            ip2cResult = await _ip2cService.GetCountryAsync(ip);
        }
        catch (Ip2cUnavailableException)
        {
            // Circuit ανοιχτό / timeout / δίκτυο κάτω.
            // ΔΕΝ το τυλίγουμε σε γενικό IpLookupException - το αφήνουμε να ανέβει ως έχει
            // ώστε ο GlobalExceptionHandler να το ξεχωρίσει και να απαντήσει 503 αντί για 500/400.
            _logger.LogWarning("IP2C unavailable for {Ip}, propagating to caller.", ip);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IP2C lookup failed for {Ip}", ip);
            throw new IpLookupException($"Could not resolve country for IP '{ip}'.", ex);
        }

        // Status 0/2 δεν είναι τεχνικό error - είναι valid απάντηση του IP2C που λέει
        // "δεν μπορώ να σου δώσω χώρα για αυτό το IP", οπότε γίνεται μετάφραση σε
        // συγκεκριμένο domain exception αντί για γενικό fail.
        switch (ip2cResult.Status)
        {
            case Ip2cStatus.InvalidInput:
                throw new IpValidationException($"IP2C rejected '{ip}' as invalid input.");
            case Ip2cStatus.UnknownIp:
                throw new IpCountryUnknownException($"IP2C has no country information for '{ip}'.");
        }

        // 4. Ensure country exists
        var countryEntity = await _countryRepository.GetByCodeAsync(ip2cResult.TwoLetterCode);
        if (countryEntity == null)
        {
            countryEntity = new Country(
                ip2cResult.TwoLetterCode,
                ip2cResult.ThreeLetterCode,
                ip2cResult.CountryName);

            try
            {
                await _countryRepository.AddAsync(countryEntity);
            }
            catch (Exception ex) // π.χ. unique constraint αν προλάβει άλλο request
            {
                _logger.LogWarning(ex, "Country {Code} may already exist, re-fetching.", ip2cResult.TwoLetterCode);
                countryEntity = await _countryRepository.GetByCodeAsync(ip2cResult.TwoLetterCode)
                    ?? throw new IpLookupException($"Could not persist or fetch country '{ip2cResult.TwoLetterCode}'.", ex);
            }
        }

        // 5. Persist IP -> country mapping
        var ipEntity = new Ip(ipAddress, ip2cResult.TwoLetterCode);

        try
        {
            await _ipRepository.AddAsync(ipEntity);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "IP {Ip} may already exist, continuing.", ip);
        }

        var finalResponse = new IpInfoResponse(
            countryEntity.TwoLetterCode,
            countryEntity.ThreeLetterCode,
            countryEntity.CountryName);

        // 6. Cache
        await _cacheService.SetAsync(cacheKey, finalResponse, TimeSpan.FromHours(24));

        return finalResponse;
    }
}
