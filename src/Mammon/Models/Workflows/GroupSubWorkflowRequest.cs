﻿namespace Mammon.Models.Workflows;

public record GroupSubWorkflowRequest
{
    public required string ReportId { get; set; }
    public required IEnumerable<ResourceCostResponse> Resources { get; set; }
}
