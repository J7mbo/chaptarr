using System;
using System.Collections.Generic;
using NLog;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.Books.Services;

namespace Chaptarr.Core.Test.Books
{
    [TestFixture]
    public class RefreshAuthorServiceRequestedBooksOnlyFixture
    {
        private sealed class TestableRefreshAuthorService : RefreshAuthorService
        {
            public TestableRefreshAuthorService(Logger logger)
                : base(authorInfo: null,
                       authorService: null,
                       bookService: null,
                       editionService: null,
                       metadataProfileService: null,
                       refreshBookService: null,
                       refreshSeriesService: null,
                       eventAggregator: null,
                       commandQueueManager: null,
                       mediaFileService: null,
                       historyService: null,
                       rootFolderService: null,
                       checkIfAuthorShouldBeRefreshed: null,
                       monitorNewBookService: null,
                       configService: null,
                       importListExclusionService: null,
                       syncMetadataService: null,
                       syncQueueService: null,
                       rootFolderSettingsResolver: null,
                       logger: logger)
            {
            }
        }

        private TestableRefreshAuthorService _service;

        [SetUp]
        public void Setup()
        {
            _service = new TestableRefreshAuthorService(LogManager.GetCurrentClassLogger());
        }

        private static Book NewBook(string title, string hardcoverId, BookMediaType mediaType = BookMediaType.Audiobook)
        {
            return new Book
            {
                Title = title,
                HardcoverBookId = hardcoverId,
                MediaType = mediaType,
                Editions = new List<Edition> { new Edition { Title = title, Monitored = true } }
            };
        }

        private static RefreshAuthorService.SortedChildren Added(params Book[] books)
        {
            var children = new RefreshAuthorService.SortedChildren();
            children.Added.AddRange(books);
            return children;
        }

        [Test]
        public void should_keep_all_new_books_for_full_catalogue_author()
        {
            var children = Added(NewBook("A", "1"), NewBook("B", "2"));

            _service.DropUnrequestedCatalogueBooks(new Author { Name = "Full" }, children, null);

            Assert.That(children.Added, Has.Count.EqualTo(2));
        }

        [Test]
        public void should_drop_all_new_books_for_requested_only_author_without_allow_list()
        {
            var children = Added(NewBook("A", "1"), NewBook("B", "2"));

            _service.DropUnrequestedCatalogueBooks(new Author { Name = "Picky", RequestedBooksOnly = true }, children, null);

            Assert.That(children.Added, Is.Empty);
        }

        [Test]
        public void should_keep_only_allow_listed_book_of_the_requested_media_type()
        {
            var requestedAudio = NewBook("Wanted", "1", BookMediaType.Audiobook);
            var requestedEbookSibling = NewBook("Wanted", "1", BookMediaType.Ebook);
            var other = NewBook("Other", "2", BookMediaType.Audiobook);
            var children = Added(requestedAudio, requestedEbookSibling, other);

            var allowList = new Dictionary<BookMediaType, HashSet<string>>
            {
                [BookMediaType.Audiobook] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "hc:1" }
            };

            _service.DropUnrequestedCatalogueBooks(new Author { Name = "Picky", RequestedBooksOnly = true }, children, allowList);

            Assert.That(children.Added, Is.EquivalentTo(new[] { requestedAudio }));
        }

        [Test]
        public void should_keep_new_book_that_absorbs_existing_local_rows()
        {
            var mergeTarget = NewBook("Absorbs Local", "3");
            var other = NewBook("Other", "4");
            var children = Added(mergeTarget, other);
            children.Merged.Add(Tuple.Create(new Book { Id = 7, Title = "Old Local Row" }, mergeTarget));

            _service.DropUnrequestedCatalogueBooks(new Author { Name = "Picky", RequestedBooksOnly = true }, children, null);

            Assert.That(children.Added, Is.EquivalentTo(new[] { mergeTarget }));
        }

        [TestCase("hc:42", true)]
        [TestCase("hc:43", false)]
        [TestCase("garbage", false)]
        public void requested_book_matcher_should_match_hardcover_ids(string providerId, bool expected)
        {
            var book = NewBook("Matched", "42");

            Assert.That(RequestedBookMatcher.Matches(book, providerId), Is.EqualTo(expected));
        }
    }
}
