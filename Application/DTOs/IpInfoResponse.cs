// DTO προς τον caller - σειρά ορισμάτων: TwoLetterCode, ThreeLetterCode, CountryName (consistent παντού)
public record IpInfoResponse(string TwoLetterCode, string ThreeLetterCode, string CountryName);
