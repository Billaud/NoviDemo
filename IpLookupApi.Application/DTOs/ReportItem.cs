using System;

namespace IpLookupApi.Application;

// Contract της εκφώνησης: { CountryName, AddressesCount, LastAddressUpdated }
public record ReportItem(string CountryName, int AddressesCount, DateTime LastAddressUpdated);
