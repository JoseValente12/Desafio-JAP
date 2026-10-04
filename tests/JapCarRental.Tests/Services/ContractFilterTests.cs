using JapCarRental.Web.Services;

namespace JapCarRental.Tests.Services;

public class ContractFilterTests
{
    private static DateOnly Oct(int day) => new(2026, 10, day);

    private static ContractListItem Item(
        int id, int startDay, int endDay, ContractStatus status = ContractStatus.Upcoming,
        string customer = "Ana Teste", string vehicle = "Renault Clio", string plate = "AA11BB") =>
        new(id, 1, customer, 1, vehicle, plate, Oct(startDay), Oct(endDay), 1000, null, status);

    private static List<int> Ids(IEnumerable<ContractListItem> items) => items.Select(i => i.Id).ToList();

    [Fact]
    public void EmptyFilter_ReturnsEverything()
    {
        var all = new[] { Item(1, 10, 15), Item(2, 20, 25) };

        Assert.Equal([1, 2], Ids(new ContractFilter().Apply(all)));
    }

    [Fact]
    public void Status_KeepsOnlyThatStatus()
    {
        var all = new[]
        {
            Item(1, 10, 15, ContractStatus.Upcoming),
            Item(2, 1, 5, ContractStatus.Finished),
            Item(3, 10, 15, ContractStatus.Cancelled)
        };

        var result = new ContractFilter(Status: ContractStatus.Cancelled).Apply(all);

        Assert.Equal([3], Ids(result));
    }

    // The contract runs from 10 to 15 October (both days inclusive).
    // The period matches when it shares at least one day with the contract.
    [Theory]
    [InlineData(1, 9, false)]    // ends the day before the contract starts
    [InlineData(1, 10, true)]    // touches the first day
    [InlineData(12, 13, true)]   // inside
    [InlineData(5, 25, true)]    // contains the whole contract
    [InlineData(15, 20, true)]   // touches the last day
    [InlineData(16, 20, false)]  // starts the day after the contract ends
    public void Period_MatchesContractsSharingAtLeastOneDay(int fromDay, int toDay, bool expected)
    {
        var all = new[] { Item(1, 10, 15) };

        var result = new ContractFilter(From: Oct(fromDay), To: Oct(toDay)).Apply(all);

        Assert.Equal(expected, result.Any());
    }

    [Fact]
    public void Period_WithOnlyFrom_KeepsContractsEndingOnOrAfterIt()
    {
        var all = new[] { Item(1, 1, 5), Item(2, 10, 15) };

        Assert.Equal([2], Ids(new ContractFilter(From: Oct(6)).Apply(all)));
    }

    [Fact]
    public void Period_WithOnlyTo_KeepsContractsStartingOnOrBeforeIt()
    {
        var all = new[] { Item(1, 1, 5), Item(2, 10, 15) };

        Assert.Equal([1], Ids(new ContractFilter(To: Oct(6)).Apply(all)));
    }

    [Fact]
    public void Period_WithDatesTheWrongWayRound_IsSwapped()
    {
        var all = new[] { Item(1, 10, 15) };

        var result = new ContractFilter(From: Oct(20), To: Oct(5)).Apply(all);

        Assert.Equal([1], Ids(result));
    }

    [Theory]
    [InlineData("ana")]        // customer name, any case
    [InlineData("CLIO")]       // vehicle name
    [InlineData("aa-11-bb")]   // plate typed with separators
    public void Search_MatchesCustomerVehicleAndPlate(string term)
    {
        var all = new[] { Item(1, 10, 15), Item(2, 10, 15, customer: "Rui", vehicle: "Peugeot 208", plate: "CC22DD") };

        Assert.Equal([1], Ids(new ContractFilter(Search: term).Apply(all)));
    }

    [Fact]
    public void Filters_AreCombined()
    {
        var all = new[]
        {
            Item(1, 10, 15, ContractStatus.Upcoming, customer: "Ana"),
            Item(2, 10, 15, ContractStatus.Upcoming, customer: "Rui"),
            Item(3, 10, 15, ContractStatus.Cancelled, customer: "Ana")
        };

        var result = new ContractFilter("ana", ContractStatus.Upcoming, Oct(12), Oct(13)).Apply(all);

        Assert.Equal([1], Ids(result));
    }
}
