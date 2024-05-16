﻿namespace Mammon.Extensions;

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

		return fullRID.Substring(index+token.Length, subsequentDivider-(index + token.Length));
	}

	public static bool IsDevBoxPool(this ResourceIdentifier resourceIdentifier)
	{
		return resourceIdentifier.ResourceType.Type == "projects/pools" && resourceIdentifier.ResourceType.Namespace == "microsoft.devcenter";
	}
}