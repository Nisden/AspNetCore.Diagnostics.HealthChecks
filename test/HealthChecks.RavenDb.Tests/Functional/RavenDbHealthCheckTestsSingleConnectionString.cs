﻿using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;


namespace HealthChecks.RavenDb.Tests.Functional
{
    public class ravendb_healthcheck_should_single_connection_string
    {
        private const string ConnectionString = "http://localhost:9030";

        public ravendb_healthcheck_should_single_connection_string()
        {
            try
            {
                using (var store = new DocumentStore
                {
                    Urls = new string[] { ConnectionString },
                })
                {
                    store.Initialize();


                    store.Maintenance.Server.Send(
                        new CreateDatabaseOperation(new DatabaseRecord("Demo")));
                }

            }
            catch { }
        }

        [Fact]
        public async Task be_healthy_if_ravendb_is_available()
        {
            var webHostBuilder = new WebHostBuilder()
                .UseStartup<DefaultStartup>()
                .ConfigureServices(services =>
                {
                    services
                    .AddHealthChecks()
                    .AddRavenDB(setup => setup.Urls = new[] { ConnectionString }, tags: new string[] { "ravendb" });
                })
                .Configure(app =>
                {
                    app.UseHealthChecks("/health", new HealthCheckOptions()
                    {
                        Predicate = r => r.Tags.Contains("ravendb")
                    });
                });

            var server = new TestServer(webHostBuilder);

            var response = await server.CreateRequest($"/health")
                .GetAsync();

            response.StatusCode
                .Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task be_healthy_if_ravendb_is_available_and_contains_specific_database()
        {
            var webHostBuilder = new WebHostBuilder()
                .UseStartup<DefaultStartup>()
                .ConfigureServices(services =>
                {
                    services
                    .AddHealthChecks()
                    .AddRavenDB(setup => setup.Urls = new[] { ConnectionString }, "Demo", tags: new string[] { "ravendb" });
                })
                .Configure(app =>
                {
                    app.UseHealthChecks("/health", new HealthCheckOptions()
                    {
                        Predicate = r => r.Tags.Contains("ravendb")
                    });
                });

            var server = new TestServer(webHostBuilder);

            var response = await server.CreateRequest($"/health")
                .GetAsync();

            response.StatusCode
                .Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task be_unhealthy_if_ravendb_is_not_available()
        {
            var connectionString = "http://localhost:9999";

            var webHostBuilder = new WebHostBuilder()
                .UseStartup<DefaultStartup>()
                .ConfigureServices(services =>
                {
                    services
                    .AddHealthChecks()
                    .AddRavenDB(setup => setup.Urls = new[] { connectionString }, tags: new string[] { "ravendb" });
                })
                .Configure(app =>
                {
                    app.UseHealthChecks("/health", new HealthCheckOptions()
                    {
                        Predicate = r => r.Tags.Contains("ravendb")
                    });
                });

            var server = new TestServer(webHostBuilder);

            var response = await server.CreateRequest($"/health")
                .GetAsync();

            response.StatusCode
                .Should().Be(HttpStatusCode.ServiceUnavailable);
        }

        [Fact]
        public async Task be_unhealthy_if_ravendb_is_available_but_database_doesnot_exist()
        {
            var webHostBuilder = new WebHostBuilder()
                .UseStartup<DefaultStartup>()
                .ConfigureServices(services =>
                {
                    services
                    .AddHealthChecks()
                    .AddRavenDB(setup =>
                    {
                        setup.Urls = new[] { ConnectionString };
                        setup.Database = "ThisDatabaseReallyDoesnExist";
                    }, "ThisDatabaseReallyDoesnExist", tags: new string[] { "ravendb" });
                })
                .Configure(app =>
                {
                    app.UseHealthChecks("/health", new HealthCheckOptions()
                    {
                        Predicate = r => r.Tags.Contains("ravendb")
                    });
                });

            var server = new TestServer(webHostBuilder);

            var response = await server.CreateRequest($"/health")
                .GetAsync();

            response.StatusCode
                .Should().Be(HttpStatusCode.ServiceUnavailable);
        }
    }
}
