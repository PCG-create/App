namespace CoachPadWpf;

public sealed class DataRetentionService
{
    private readonly SummaryStore _summaryStore = new();
    private readonly LoggingService _logging = new();

    public void DeleteAll()
    {
        _summaryStore.DeleteAll();
        _logging.DeleteAll();
    }
}
