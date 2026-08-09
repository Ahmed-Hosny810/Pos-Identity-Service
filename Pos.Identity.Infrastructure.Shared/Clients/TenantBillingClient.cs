using Pos.Identity.Application.Dtos;
using Pos.Identity.Application.Exceptions;
using Pos.Identity.Application.Interfaces.Clients;
using Pos.Identity.Application.Interfaces.Services;
using Pos.Identity.Application.Wrappers;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace Pos.Identity.Infrastructure.Shared.Clients
{
    public class TenantBillingClient : ITenantBillingClient
    {
        private readonly HttpClient _httpClient;
        private readonly ICurrentUserService _currentUserService;

        public TenantBillingClient(HttpClient httpClient,ICurrentUserService currentUserService)
        {
            _httpClient = httpClient;
            _currentUserService = currentUserService;
        }

        public async Task<CreateTenantResult> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken)
        {
            var accessToken=_currentUserService.AccessToken;

            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ApiException("Access token is missing.");

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/tenants");

            httpRequest.Headers.Authorization=new AuthenticationHeaderValue("Bearer", accessToken);

            httpRequest.Content = JsonContent.Create(request);

            var response = await _httpClient.SendAsync(
                httpRequest,
                cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new ApiException(
                    $"Tenant Billing request failed. StatusCode: {(int)response.StatusCode}. Response: {responseBody}");
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<Response<Guid>>(
                cancellationToken: cancellationToken);

            if (apiResponse == null)
                throw new ApiException("Tenant Billing returned an empty response.");

            if (apiResponse.Data == Guid.Empty)
                throw new ApiException("Tenant Billing returned an invalid TenantId.");

            return new CreateTenantResult
            {
                TenantId = apiResponse.Data
            }; ;
        }
    }
}
