using Pos.Identity.Application.Dtos;
using Pos.Identity.Application.Exceptions;
using Pos.Identity.Application.Interfaces.Clients;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace Pos.Identity.Infrastructure.Shared.Clients
{
    public class TenantBillingClient : ITenantBillingClient
    {
        private readonly HttpClient _httpClient;

        public TenantBillingClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<CreateTenantResult> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/tenants", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);

                throw new ApiException(
                    $"Tenant Billing request failed. StatusCode: {(int)response.StatusCode}. Response: {error}");
            }
            var result = await response.Content.ReadFromJsonAsync<CreateTenantResult>(
           cancellationToken: cancellationToken);

            if (result == null)
                throw new ApiException("Tenant Billing returned an empty response.");

            if (result.TenantId == Guid.Empty)
                throw new ApiException("Tenant Billing returned an invalid TenantId.");

            return result;
        }
    }
}
