namespace IntegrationTests;

[CollectionDefinition(nameof(IntegrationFixture))]
public sealed class IntegrationFixture : ICollectionFixture<CustomWebApplicationFactory>;
