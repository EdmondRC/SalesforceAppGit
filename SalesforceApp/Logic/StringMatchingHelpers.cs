using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesforceApp.Logic
{
    public static class StringMatchingHelpers
    {

        public static string NormalizeWebsite(string website)
        {
            if (string.IsNullOrWhiteSpace(website))
                return string.Empty;

            website = website.Trim().ToLowerInvariant();

            if (!website.StartsWith("http://") &&
                !website.StartsWith("https://"))
            {
                website = "https://" + website;
            }

            if (Uri.TryCreate(website, UriKind.Absolute, out var uri))
            {
                var host = uri.Host;

                if (host.StartsWith("www."))
                    host = host.Substring(4);

                return host;
            }

            return website;
        }

        public static string NormalizePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return string.Empty;

            string digits = new string(
                phone.Where(char.IsDigit).ToArray());

            if (digits.Length == 11 && digits.StartsWith("1"))
            {
                digits = digits[1..];
            }

            return digits;
        }
        public static string NormalizeString(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            value = value.ToUpperInvariant();

            value = value.Replace(".", "");
            value = value.Replace(",", "");
            value = value.Replace("-", " ");
            value = value.Replace("#", " ");
            value = value.Replace("&", " AND ");
            value = value.Replace("'", "");

            while (value.Contains("  "))
            {
                value = value.Replace("  ", " ");
            }

            return value.Trim();
        }
    }
    public static class AddressNormalizer
    {
        private static readonly Dictionary<string, string> Replacements = new()
        {
            // Street suffixes
            ["ST"] = "STREET",
            ["RD"] = "ROAD",
            ["DR"] = "DRIVE",
            ["AVE"] = "AVENUE",
            ["BLVD"] = "BOULEVARD",
            ["LN"] = "LANE",
            ["CT"] = "COURT",
            ["CIR"] = "CIRCLE",
            ["PKWY"] = "PARKWAY",
            ["PL"] = "PLACE",
            ["PLZ"] = "PLAZA",
            ["TER"] = "TERRACE",
            ["TRL"] = "TRAIL",
            ["HWY"] = "HIGHWAY",
            ["SQ"] = "SQUARE",
            ["EXPY"] = "EXPRESSWAY",
            ["FWY"] = "FREEWAY",
            ["JCT"] = "JUNCTION",

            // Directions
            ["N"] = "NORTH",
            ["S"] = "SOUTH",
            ["E"] = "EAST",
            ["W"] = "WEST",
            ["NE"] = "NORTHEAST",
            ["NW"] = "NORTHWEST",
            ["SE"] = "SOUTHEAST",
            ["SW"] = "SOUTHWEST",

            // Unit identifiers
            ["APT"] = "APARTMENT",
            ["STE"] = "SUITE",
            ["BLDG"] = "BUILDING",
            ["FL"] = "FLOOR",
            ["RM"] = "ROOM",
            ["DEPT"] = "DEPARTMENT"
        };

        public static string NormalizeStreetAddress(string? address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return string.Empty;

            string cleaned = StringMatchingHelpers.NormalizeString(address);

            var words = cleaned
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < words.Length; i++)
            {
                if (Replacements.TryGetValue(words[i], out var replacement))
                {
                    words[i] = replacement;
                }
            }

            return string.Join(" ", words);
        }


    }
    public static class StateNormalizer
    {
        private static readonly Dictionary<string, string> StateMap =
            new(StringComparer.OrdinalIgnoreCase)
        {
            { "ALABAMA", "AL" }, { "AL", "AL" },
            { "ALASKA", "AK" }, { "AK", "AK" },
            { "ARIZONA", "AZ" }, { "AZ", "AZ" },
            { "ARKANSAS", "AR" }, { "AR", "AR" },
            { "CALIFORNIA", "CA" }, { "CA", "CA" },
            { "COLORADO", "CO" }, { "CO", "CO" },
            { "CONNECTICUT", "CT" }, { "CT", "CT" },
            { "DELAWARE", "DE" }, { "DE", "DE" },
            { "FLORIDA", "FL" }, { "FL", "FL" },
            { "GEORGIA", "GA" }, { "GA", "GA" },
            { "HAWAII", "HI" }, { "HI", "HI" },
            { "IDAHO", "ID" }, { "ID", "ID" },
            { "ILLINOIS", "IL" }, { "IL", "IL" },
            { "INDIANA", "IN" }, { "IN", "IN" },
            { "IOWA", "IA" }, { "IA", "IA" },
            { "KANSAS", "KS" }, { "KS", "KS" },
            { "KENTUCKY", "KY" }, { "KY", "KY" },
            { "LOUISIANA", "LA" }, { "LA", "LA" },
            { "MAINE", "ME" }, { "ME", "ME" },
            { "MARYLAND", "MD" }, { "MD", "MD" },
            { "MASSACHUSETTS", "MA" }, { "MA", "MA" },
            { "MICHIGAN", "MI" }, { "MI", "MI" },
            { "MINNESOTA", "MN" }, { "MN", "MN" },
            { "MISSISSIPPI", "MS" }, { "MS", "MS" },
            { "MISSOURI", "MO" }, { "MO", "MO" },
            { "MONTANA", "MT" }, { "MT", "MT" },
            { "NEBRASKA", "NE" }, { "NE", "NE" },
            { "NEVADA", "NV" }, { "NV", "NV" },
            { "NEW HAMPSHIRE", "NH" }, { "NH", "NH" },
            { "NEW JERSEY", "NJ" }, { "NJ", "NJ" },
            { "NEW MEXICO", "NM" }, { "NM", "NM" },
            { "NEW YORK", "NY" }, { "NY", "NY" },
            { "NORTH CAROLINA", "NC" }, { "NC", "NC" },
            { "NORTH DAKOTA", "ND" }, { "ND", "ND" },
            { "OHIO", "OH" }, { "OH", "OH" },
            { "OKLAHOMA", "OK" }, { "OK", "OK" },
            { "OREGON", "OR" }, { "OR", "OR" },
            { "PENNSYLVANIA", "PA" }, { "PA", "PA" },
            { "RHODE ISLAND", "RI" }, { "RI", "RI" },
            { "SOUTH CAROLINA", "SC" }, { "SC", "SC" },
            { "SOUTH DAKOTA", "SD" }, { "SD", "SD" },
            { "TENNESSEE", "TN" }, { "TN", "TN" },
            { "TEXAS", "TX" }, { "TX", "TX" },
            { "UTAH", "UT" }, { "UT", "UT" },
            { "VERMONT", "VT" }, { "VT", "VT" },
            { "VIRGINIA", "VA" }, { "VA", "VA" },
            { "WASHINGTON", "WA" }, { "WA", "WA" },
            { "WEST VIRGINIA", "WV" }, { "WV", "WV" },
            { "WISCONSIN", "WI" }, { "WI", "WI" },
            { "WYOMING", "WY" }, { "WY", "WY" }
        };

        public static string NormalizeState(string state)
        {
            if (string.IsNullOrWhiteSpace(state))
                return string.Empty;

            state = state.Trim().ToUpperInvariant();

            return StateMap.TryGetValue(state, out var normalized)
                ? normalized
                : state;
        }

    } 
}


