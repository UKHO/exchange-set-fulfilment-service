﻿using UKHO.ADDS.EFS.FunctionalTests.Services;
using UKHO.ADDS.EFS.Infrastructure.Configuration.Namespaces;

namespace UKHO.ADDS.EFS.FunctionalTests
{
    public class UpdateSinceFunctionalTests : TestBase
    {
        //PBI 242767 - Input validation for the ESS API - Update Since Endpoint
        [Theory(Skip = "Temporarily disable")]
        [InlineData("https://valid.com/callback", "s101", HttpStatusCode.Accepted, "")] // Test Case 244582 - Valid Format
        public async Task ValidateUpdateSinceEndpointWithValidDates(string callbackUri, string productIdentifier, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            var sinceDateTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            await OrchestratorCommands.VerifyUpdateSinceEndpointResponse(sinceDateTime, callbackUri,
                productIdentifier, httpClient, expectedStatusCode, expectedErrorMessage);
        }

        [Theory(Skip = "Temporarily disable")]
        [InlineData("2025-09-29", "https://valid.com/callback", "s102", HttpStatusCode.BadRequest, "Provided updatesSince is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2025-09-29T00:00:00Z')")] // Test Case 244583 - Invalid Format when time part is missing
        [InlineData("2025-08-25L07:8:00.00Z", "https://valid.com/callback", "s102", HttpStatusCode.BadRequest, "Provided updatesSince is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2025-09-29T00:00:00Z')")] // Test Case 244583 - Invalid Format
        [InlineData("", "https://valid.com/callback", "s101", HttpStatusCode.BadRequest, "No UpdateSince date time provided")] // Test Case 246905 - Empty sinceDateTime
        [InlineData("null", "https://valid.com/callback", "s101", HttpStatusCode.BadRequest, "Provided updatesSince is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2025-09-29T00:00:00Z')")] // Test Case 246905 - Null sinceDateTime
        public async Task ValidateUpdateSinceEndPointWithInvalidDates(string sinceDateTime, string callbackUri, string productIdentifier, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            await OrchestratorCommands.VerifyUpdateSinceEndpointResponse(sinceDateTime, callbackUri,
                productIdentifier, httpClient, expectedStatusCode, expectedErrorMessage);
        }

        [Theory(Skip = "Temporarily disable")]
        [InlineData("http://invalid.com/callback", "s101", HttpStatusCode.BadRequest, "URI is malformed or does not use HTTPS")] // Test Case 244586 -  Invalid CallBack Uri Format
        [InlineData("", "s102", HttpStatusCode.Accepted, "")] // Test Case 244585 -  Valid input with blank CallBack Uri
        public async Task ValidateUpdateSinceEndpointWithInvalidAndBlankCallBackUri(string callbackUri, string productIdentifier, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            var sinceDateTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            await OrchestratorCommands.VerifyUpdateSinceEndpointResponse(sinceDateTime, callbackUri,
                productIdentifier, httpClient, expectedStatusCode, expectedErrorMessage);
        }

        [Theory(Skip = "Temporarily disable")]
        [InlineData("https://valid.com/callback", "SABC", HttpStatusCode.BadRequest, "productIdentifier must be exactly 4 characters: start with 'S' or 's' followed by three digits, with no spaces or extra characters")] // Test Case 244907 - Invalid Product Identifier Format
        public async Task ValidateUpdateSinceEndpointWithInvalidProductIdentifier(string callbackUri, string productIdentifier, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            var sinceDateTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            await OrchestratorCommands.VerifyUpdateSinceEndpointResponse(sinceDateTime, callbackUri,
                productIdentifier, httpClient, expectedStatusCode, expectedErrorMessage);
        }

        [Theory(Skip = "Temporarily disable")]
        [InlineData(-28, "https://valid.com/callback", "s101", HttpStatusCode.BadRequest, "Date time provided is more than 28 days in the past")] // Test Case 244584 - Date is of 28th day in the past
        [InlineData(-14, "https://valid.com/callback", "s102", HttpStatusCode.Accepted, "")] // Test Case 245730 - Past Date less than 28 days
        [InlineData(-35, "https://valid.com/callback", "s104", HttpStatusCode.BadRequest, "Date time provided is more than 28 days in the past")] // Test Case 245720 - Date more than 28 days in the past
        [InlineData(1, "https://valid.com/callback", "s111", HttpStatusCode.BadRequest, "UpdateSince date cannot be a future date")] // Test Case 245121 - Future Date
        public async Task ValidateUpdateSinceEndpointWithPastAndFutureDates(int days, string callbackUri, string productIdentifier, HttpStatusCode expectedStatusCode, string expectedErrorMessage)
        {
            var sinceDateTime = DateTime.UtcNow.AddDays(days).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            await OrchestratorCommands.VerifyUpdateSinceEndpointResponse(sinceDateTime, callbackUri,
                productIdentifier, httpClient, expectedStatusCode, expectedErrorMessage);
        }
    }
}
