using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace NzbDrone.Core.Books.Services
{
    /// <summary>
    /// Matches catalogue books against explicitly requested provider IDs ("hc:123", "gr:456", ...).
    /// Shared by the add path (AuthorLibraryService) and the refresh path (RefreshAuthorService)
    /// so "requested books only" authors keep exactly the same books in both.
    /// </summary>
    public static class RequestedBookMatcher
    {
        public static bool Matches(Book book, string providerId, Logger logger = null)
        {
            if (book == null || string.IsNullOrWhiteSpace(providerId))
            {
                return false;
            }

            // Provider IDs are in format "prefix:id" (e.g., "hc:495645", "gr:12345")
            var parts = providerId.Split(':');
            if (parts.Length != 2)
            {
                return false;
            }

            var prefix = parts[0].Trim().ToLowerInvariant();
            var id = parts[1].Trim();
            var normalizedProviderId = $"{prefix}:{id}";

            if (BookIdentity.GetProviderIdentityTokens(book).Contains(normalizedProviderId))
            {
                return true;
            }

            // Book fields may or may not carry their prefix.
            return prefix switch
            {
                "hc" => ExtractRawId(book.HardcoverBookId) == id,
                "gr" => BookEditionIdentity.GetCanonicalWorkProviderIds(book)
                    .Concat(BookEditionIdentity.GetCanonicalEditionProviderIds(book, logger, "RequestedBookMatcher"))
                    .Any(p => ExtractRawId(p) == id),
                "ol" => ExtractRawId(book.OpenLibraryWorkId) == id,
                "gb" => BookEditionIdentity.GetCanonicalEditionProviderIds(book, logger, "RequestedBookMatcher")
                    .Any(p => ExtractRawId(p) == id),
                "az" => BookEditionIdentity.GetCanonicalEditionProviderIds(book, logger, "RequestedBookMatcher")
                    .Any(p => ExtractRawId(p).Equals(id, StringComparison.OrdinalIgnoreCase)),
                _ => false
            };
        }

        public static bool MatchesAny(Book book, IReadOnlyDictionary<BookMediaType, HashSet<string>> requestedByMediaType, Logger logger = null)
        {
            if (book == null || requestedByMediaType == null)
            {
                return false;
            }

            return requestedByMediaType.TryGetValue(book.MediaType, out var providerIds) &&
                   providerIds != null &&
                   providerIds.Any(providerId => Matches(book, providerId, logger));
        }

        private static string ExtractRawId(string providerId)
        {
            if (string.IsNullOrEmpty(providerId))
            {
                return string.Empty;
            }

            var colonIndex = providerId.IndexOf(':');
            return colonIndex > 0 ? providerId.Substring(colonIndex + 1) : providerId;
        }
    }
}
