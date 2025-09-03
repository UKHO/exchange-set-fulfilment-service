using UKHO.ADDS.EFS.Orchestrator.UnitTests.Infrastructure.Tables.Implementation.Entities;
using UKHO.ADDS.Infrastructure.Results;
using Xunit;
using Xunit.Abstractions;
using Assert = Xunit.Assert;

namespace UKHO.ADDS.EFS.Orchestrator.UnitTests.Infrastructure.Tables.Implementation
{
    public class FakeRepositoryTests : GivenWhenThenTest
    {
        public FakeRepositoryTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact]
        public async Task AddAsync_UniqueEntity_Works()
        {
            Result result = null;

            await Given("A fresh repository", () => new FakeRepository<UniqueKeyTestEntity>("Unique", e => e.Id, e => e.Id))

                .When("I add a new entity", async t =>
                {
                    result = await t.AddAsync(new UniqueKeyTestEntity
                    {
                        Id = "123"
                    });
                })

                .Then("It should contain the entity", t =>
                {
                    Assert.True(result.IsSuccess());

                    Assert.Single(t.Items);
                    Assert.Equal("123", t.Items.First().Id);
                });
        }

        [Fact]
        public async Task AddAsync_UniqueEntity_Fails_WhenExists()
        {
            Result result = null;

            await Given("A repository with existing entity", () =>
                {
                    var table = new FakeRepository<UniqueKeyTestEntity>("Unique", e => e.Id, e => e.Id);

                    table.Seed([
                        new UniqueKeyTestEntity
                        {
                            Id = "123"
                        }
                    ]);

                    return table;
                })

                .When("I add a duplicate", async t =>
                {
                    result = await t.AddAsync(new UniqueKeyTestEntity
                    {
                        Id = "123"
                    });
                })

                .Then("It should fail to add", t =>
                {
                    Assert.True(result.IsFailure());

                    Assert.Single(t.Items);
                });
        }

        [Fact]
        public async Task GetUniqueAsync_ByKey_FindsEntity()
        {
            Result<UniqueKeyTestEntity>? result = null;

            await Given("A repository with an entity", () =>
                {
                    var table = new FakeRepository<UniqueKeyTestEntity>("Unique", e => e.Id, e => e.Id);

                    table.Seed([
                        new UniqueKeyTestEntity
                        {
                            Id = "abc"
                        }
                    ]);

                    return table;
                })

                .When("I get it by key", async t =>
                {
                    result = await t.GetUniqueAsync("abc");
                })

                .Then("It should return the entity", t =>
                {
                    Assert.True(result!.Value.IsSuccess(out var value));
                    Assert.Equal("abc", value.Id);
                });
        }

        [Fact]
        public async Task GetUniqueAsync_ByKey_Fails_WhenNotFound()
        {
            Result<UniqueKeyTestEntity>? result = null;

            await Given("A fresh repository", () => new FakeRepository<UniqueKeyTestEntity>("Unique", e => e.Id, e => e.Id))

                .When("I try to get missing entity", async t =>
                {
                    result = await t.GetUniqueAsync("missing");
                })

                .Then("It should fail", t =>
                {
                    Assert.True(result!.Value.IsFailure());
                });
        }

        [Fact]
        public async Task GetUniqueAsync_ByPartitionRowKey_Works()
        {
            Result<UniqueKeyTestEntity>? result = null;

            await Given("A repository with entity", () =>
                {
                    var table = new FakeRepository<UniqueKeyTestEntity>("Unique", e => e.Id, e => e.Id);

                    table.Seed([
                        new UniqueKeyTestEntity
                        {
                            Id = "xyz"
                        }
                    ]);

                    return table;
                })

                .When("I get by partition and row key", async t =>
                {
                    result = await t.GetUniqueAsync("xyz", "xyz");
                })

                .Then("It should return the entity", t =>
                {
                    Assert.True(result!.Value.IsSuccess(out var value));
                    Assert.Equal("xyz", value.Id);
                });
        }

        [Fact]
        public async Task GetUniqueAsync_ByPartitionRowKey_Fails_WhenNotFound()
        {
            Result<UniqueKeyTestEntity>? result = null;

            await Given("An empty repository", () => new FakeRepository<UniqueKeyTestEntity>("Unique", e => e.Id, e => e.Id))

                .When("I try to get missing by partition/row key", async t =>
                {
                    result = await t.GetUniqueAsync("a", "b");
                })

                .Then("It should fail", t =>
                {
                    Assert.True(result!.Value.IsFailure());
                });
        }

        [Fact]
        public async Task GetListAsync_Returns_MatchingPartition()
        {
            IEnumerable<UniqueKeyTestEntity> list = null!;

            await Given("A repository with entities", () =>
                {
                    var table = new FakeRepository<UniqueKeyTestEntity>("Unique", e => e.Id, e => e.Id);

                    table.Seed([
                        new UniqueKeyTestEntity
                        {
                            Id = "A"
                        },
                        new UniqueKeyTestEntity
                        {
                            Id = "B"
                        }
                    ]);

                    return table;
                })

                .When("I get list by partition", async t =>
                {
                    list = await t.GetListAsync("A");
                })

                .Then("It should return matching", t =>
                {
                    Assert.Single(list);
                    Assert.Equal("A", list.First().Id);
                });
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAll()
        {
            IEnumerable<UniqueKeyTestEntity> all = null!;

            await Given("A seeded repository", () =>
                {
                    var table = new FakeRepository<UniqueKeyTestEntity>("Unique", e => e.Id, e => e.Id);
                    table.Seed(new[]
                    {
                        new UniqueKeyTestEntity
                        {
                            Id = "1"
                        },
                        new UniqueKeyTestEntity
                        {
                            Id = "2"
                        }
                    });
                    return table;
                })

                .When("I get all", async t =>
                {
                    all = await t.GetAllAsync();
                })

                .Then("It should return all entities", t =>
                {
                    Assert.Equal(2, all.Count());
                });
        }

        [Fact]
        public async Task UpdateAsync_Works_WhenExists()
        {
            Result result = null;

            await Given("A repository with existing entity", () =>
                {
                    var table = new FakeRepository<UniqueKeyTestEntity>("Unique", e => e.Id, e => e.Id);

                    table.Seed([
                        new UniqueKeyTestEntity
                        {
                            Id = "X"
                        }
                    ]);

                    return table;
                })
                .When("I update it", async t =>
                {
                    result = await t.UpdateAsync(new UniqueKeyTestEntity
                    {
                        Id = "X"
                    });
                })
                .Then("It should succeed", t =>
                {
                    Assert.True(result.IsSuccess());
                    Assert.Single(t.Items);
                });
        }

        [Fact]
        public async Task UpdateAsync_Fails_WhenNotExists()
        {
            Result result = null;

            await Given("An empty repository", () => new FakeRepository<UniqueKeyTestEntity>("Unique", e => e.Id, e => e.Id))

                .When("I try to update missing entity", async t =>
                {
                    result = await t.UpdateAsync(new UniqueKeyTestEntity
                    {
                        Id = "Y"
                    });
                })

                .Then("It should fail", t =>
                {
                    Assert.True(result.IsFailure());
                    Assert.Empty(t.Items);
                });
        }

        [Fact]
        public async Task UpsertAsync_InsertsOrUpdates()
        {
            Result result = null;

            await Given("An empty repository", () => new FakeRepository<UniqueKeyTestEntity>("Unique", e => e.Id, e => e.Id))

                .When("I upsert new entity", async t =>
                {
                    result = await t.UpsertAsync(new UniqueKeyTestEntity
                    {
                        Id = "U1"
                    });
                })

                .Then("It should add it", t =>
                {
                    Assert.True(result.IsSuccess());
                    Assert.Single(t.Items);
                });
        }

        [Fact]
        public async Task DeleteAsync_ByKey_Works()
        {
            Result result = null;

            await Given("A repository with entity", () =>
                {
                    var table = new FakeRepository<UniqueKeyTestEntity>("Unique", e => e.Id, e => e.Id);

                    table.Seed([
                        new UniqueKeyTestEntity
                        {
                            Id = "Del"
                        }
                    ]);

                    return table;
                })

                .When("I delete it", async t =>
                {
                    result = await t.DeleteAsync("Del", "Del");
                })

                .Then("It should remove it", t =>
                {
                    Assert.True(result.IsSuccess());
                    Assert.Empty(t.Items);
                });
        }

        [Fact]
        public async Task DeleteAsync_ByPartition_Works()
        {
            Result result = null;

            await Given("A repository with multiple entities", () =>
                {
                    var table = new FakeRepository<UniqueKeyTestEntity>("Unique", e => e.Id, e => e.Id);

                    table.Seed([
                        new UniqueKeyTestEntity
                        {
                            Id = "Part"
                        },
                        new UniqueKeyTestEntity
                        {
                            Id = "Other"
                        }
                    ]);

                    return table;
                })

                .When("I delete by partition", async t =>
                {
                    result = await t.DeleteAsync("Part");
                })

                .Then("It should remove matching partition", t =>
                {
                    Assert.True(result.IsSuccess());
                    Assert.Single(t.Items);
                    Assert.Equal("Other", t.Items.First().Id);
                });
        }

        [Fact]
        public async Task CreateIfNotExistsAsync_AlwaysSucceeds()
        {
            Result result = null;

            await Given("A fresh repository", () => new FakeRepository<UniqueKeyTestEntity>("Unique", e => e.Id, e => e.Id))

                .When("I call CreateIfNotExistsAsync", async t =>
                {
                    result = await t.CreateIfNotExistsAsync(CancellationToken.None);
                })

                .Then("It should succeed", t =>
                {
                    Assert.True(result.IsSuccess());
                    Assert.Empty(t.Items);
                });
        }

        [Fact]
        public async Task AddAsync_CompoundEntity_Works()
        {
            Result result = null;

            await Given("A fresh compound repository", () => new FakeRepository<CompoundKeyTestEntity>("Compound", e => e.PartitionKey, e => e.RowKey))

                .When("I add new entity", async t =>
                {
                    result = await t.AddAsync(new CompoundKeyTestEntity
                    {
                        PartitionKey = "P", RowKey = "R"
                    });
                })

                .Then("It should succeed", t =>
                {
                    Assert.True(result.IsSuccess());
                    Assert.Single(t.Items);
                });
        }

        [Fact]
        public async Task GetListAsync_Compound_ReturnsMatching()
        {
            IEnumerable<CompoundKeyTestEntity> list = null!;

            await Given("A seeded compound repository", () =>
                {
                    var table = new FakeRepository<CompoundKeyTestEntity>("Compound", e => e.PartitionKey, e => e.RowKey);

                    table.Seed([
                        new CompoundKeyTestEntity
                        {
                            PartitionKey = "P", RowKey = "1"
                        },
                        new CompoundKeyTestEntity
                        {
                            PartitionKey = "P", RowKey = "2"
                        },
                        new CompoundKeyTestEntity
                        {
                            PartitionKey = "Q", RowKey = "3"
                        }
                    ]);

                    return table;
                })

                .When("I get by partition", async t =>
                {
                    list = await t.GetListAsync("P");
                })

                .Then("It should return correct items", t =>
                {
                    Assert.Equal(2, list.Count());
                    Assert.All(list, e => Assert.Equal("P", e.PartitionKey));
                });
        }

        [Fact]
        public async Task DeleteAsync_Compound_ByPartition_Works()
        {
            Result result = null;

            await Given("A seeded repository", () =>
                {
                    var table = new FakeRepository<CompoundKeyTestEntity>("Compound", e => e.PartitionKey, e => e.RowKey);

                    table.Seed([
                        new CompoundKeyTestEntity
                        {
                            PartitionKey = "P", RowKey = "1"
                        },
                        new CompoundKeyTestEntity
                        {
                            PartitionKey = "P", RowKey = "2"
                        },
                        new CompoundKeyTestEntity
                        {
                            PartitionKey = "Q", RowKey = "3"
                        }
                    ]);

                    return table;
                })

                .When("I delete partition P", async t =>
                {
                    result = await t.DeleteAsync("P");
                })

                .Then("It should remove matching items", t =>
                {
                    Assert.True(result.IsSuccess());
                    Assert.Single(t.Items);
                    Assert.Equal("Q", t.Items.First().PartitionKey);
                });
        }
    }
}
