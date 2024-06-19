namespace Mammon.Extensions;

public static class ResourceIdentifierExtensions
{
	public static string GetDevBoxProjectName(this ResourceIdentifier resourceIdentifier)
	{
		if (!resourceIdentifier.IsDevBoxPool())
			throw new InvalidOperationException("GetDevBoxPoolName can only be called for devbox resource");

		const string token = "/microsoft.devcenter/projects/";
		var fullRID = resourceIdentifier.ToString();

		var index = fullRID.IndexOf(token);
		var subsequentDivider = fullRID.IndexOf('/', index+token.Length);

		return fullRID[(index + token.Length)..subsequentDivider];
	}

	public static bool IsLogAnalyticsWorkspace(this ResourceIdentifier value)
	{
		return value.ResourceType == "microsoft.operationalinsights/workspaces";
	}


	public static bool IsDevBoxPool(this ResourceIdentifier resourceIdentifier)
	{
		return resourceIdentifier.ResourceType.Type == "projects/pools" && resourceIdentifier.ResourceType.Namespace == "microsoft.devcenter";
	}

	public static ResourceIdentifier GetResourceGroupIdentifier(this ResourceIdentifier resourceIdentifier)
	{
		var rIDString = resourceIdentifier.ToString();
		int providerIndex = rIDString.IndexOf("/providers");
		
		if(providerIndex == -1)
			throw new InvalidOperationException($"Unexpected resource identifier{rIDString}");

		return new ResourceIdentifier(rIDString.Substring(0, providerIndex));

	}
}
