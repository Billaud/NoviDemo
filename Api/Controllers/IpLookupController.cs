using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/ip")]
public class IpLookupController : ControllerBase
{
    private readonly IMediator _mediator;

    public IpLookupController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Τα exceptions (Ip2cUnavailableException, IpValidationException, IpCountryUnknownException κ.λπ.)
    // δεν πιάνονται εδώ - τα μεταφράζει σε HTTP status codes ο GlobalExceptionHandler.
    [HttpGet("{ip}")]
    public async Task<IActionResult> Get(string ip, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetIpInfoQuery(ip), cancellationToken);
        return Ok(result);
    }
}
