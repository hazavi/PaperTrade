using Xunit;

// API factories share the same scratch database, including its migration history.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
