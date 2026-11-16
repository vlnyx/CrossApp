using Core.Domain;
using Core.Dto;
using Core.Reports;
using Xunit;

namespace Core.Tests;

public sealed class AdditionalReportTests
{
    [Fact]
    public void TopBy_SortsByPrimaryAndSecondaryKeys()
    {
        var values = new[]
        {
            new RankedValue("Charlie", 3),
            new RankedValue("Bravo", 3),
            new RankedValue("Alpha", 5)
        };

        IReadOnlyList<RankedValue> result = values
            .TopBy(
                value => value.Score,
                value => value.Name,
                2,
                thenByComparer: StringComparer.Ordinal)
            .ToList();

        Assert.Equal(["Alpha", "Bravo"], result.Select(value => value.Name));
    }

    [Fact]
    public void BooksByGenreQuerySyntax_MatchesMethodSyntaxIncludingCaseInsensitiveGroups()
    {
        var books = new[]
        {
            BookCopy.Create("B1", "ISBN-1", "One", "Fantasy", 3),
            BookCopy.Create("B2", "ISBN-2", "Two", "fantasy", 4),
            BookCopy.Create("B3", "ISBN-3", "Three", "History", 8)
        };

        IReadOnlyList<GroupSummary> methodReport = LibraryReportService.BooksByGenre(books);
        IReadOnlyList<GroupSummary> queryReport = AdvancedReportService.BooksByGenreQuerySyntax(books);

        Assert.Equal(methodReport, queryReport);
        Assert.Equal(new GroupSummary("History", 1, 8), queryReport[0]);
        Assert.Equal(2, queryReport[1].Count);
        Assert.Equal(7, queryReport[1].TotalQuantity);
    }

    [Fact]
    public void GetLoanDetails_JoinsBooksAndLoansIntoSummaryDto()
    {
        var book = BookCopy.Create("B1", "ISBN-1", "The Book", "Fiction");
        var loan = Loan.FromDto(new LoanDto(
            "L1",
            book.Id,
            "Reader-1",
            new DateTime(2026, 1, 1),
            null,
            new DateTime(2026, 1, 15)));

        IReadOnlyList<BookLoanDetails> details =
            AdvancedReportService.GetLoanDetails([book], [loan]);

        Assert.Equal(
            [new BookLoanDetails("The Book", "Reader-1", new DateTime(2026, 1, 15))],
            details);
    }

    private sealed record RankedValue(string Name, int Score);
}
