using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/reports")]
public class ReportController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/reports                                  -> HTML πίνακας για όλες τις χώρες
    // GET /api/reports?countryCodes=GR&countryCodes=IT   -> HTML πίνακας για πολλές συγκεκριμένες χώρες
    // GET /api/reports/GR                                -> HTML πίνακας για μία μόνο χώρα (route param)
    //
    // Το route param είναι για το common case "θέλω μία χώρα" χωρίς query string.
    // Αν δοθεί και τα δύο, το route param κερδίζει (πιο συγκεκριμένο).
    [HttpGet]
    [HttpGet("{countryCode}")]
    public async Task<ContentResult> Get(
        string countryCode,
        [FromQuery] string[] countryCodes,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> codes = !string.IsNullOrWhiteSpace(countryCode)
            ? new[] { countryCode }
            : countryCodes;

        var items = await _mediator.Send(new GetReportQuery(codes), cancellationToken);
        var html = BuildHtmlTable(items);
        return Content(html, "text/html");
    }

    private static string BuildHtmlTable(List<ReportItem> items)
    {
        var sb = new StringBuilder();
        sb.Append("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\">");
        sb.Append("<tr><th>CountryName</th><th>AddressesCount</th><th>LastAddressUpdated</th></tr>");

        foreach (var item in items)
        {
            sb.Append("<tr>");
            sb.Append("<td>").Append(WebUtility.HtmlEncode(item.CountryName)).Append("</td>");
            sb.Append("<td>").Append(item.AddressesCount).Append("</td>");
            sb.Append("<td>").Append(item.LastAddressUpdated.ToString("yyyy-MM-dd HH:mm:ss.fffffff")).Append("</td>");
            sb.Append("</tr>");
        }

        sb.Append("</table>");
        return sb.ToString();
    }
}
