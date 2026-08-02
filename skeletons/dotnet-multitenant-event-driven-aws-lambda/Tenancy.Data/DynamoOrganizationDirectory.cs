using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Common.Lib.Tenancy;
using Tenancy.Application.Contracts;

namespace Tenancy.Data;

/// <summary>The global tenant directory in DynamoDB — the one table addressable
/// before tenant scope exists (adrs/database/database-per-tenant.md). Reads are
/// GetItem by key; never a scan (adrs/database/dynamodb-hot-path.md).</summary>
public sealed class DynamoOrganizationDirectory(IAmazonDynamoDB dynamo) : IOrganizationDirectory
{
    public const string TableName = "Organization";

    public async Task PutAsync(OrganizationRecord organization, CancellationToken cancellationToken)
    {
        await dynamo.PutItemAsync(new PutItemRequest
        {
            TableName = TableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["orgid"] = new(organization.OrgId),
                ["name"] = new(organization.Name),
                ["defaultworkspaceid"] = new(organization.DefaultWorkspaceId),
                ["enabled"] = new() { BOOL = organization.Enabled },
            },
        }, cancellationToken);
    }

    public async Task<OrganizationRecord?> GetAsync(OrgId orgId, CancellationToken cancellationToken)
    {
        var response = await dynamo.GetItemAsync(new GetItemRequest
        {
            TableName = TableName,
            Key = new Dictionary<string, AttributeValue> { ["orgid"] = new(orgId.Value) },
        }, cancellationToken);

        if (!response.IsItemSet)
        {
            return null;
        }

        var item = response.Item;
        return new OrganizationRecord(
            item["orgid"].S,
            item["name"].S,
            item["defaultworkspaceid"].S,
            item["enabled"].BOOL ?? false);
    }
}
