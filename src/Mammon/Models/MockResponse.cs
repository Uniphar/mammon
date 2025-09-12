namespace Mammon.Models;

internal class MockResponse(int status) : Response
{
    public override int Status => status;
    public override string ReasonPhrase => "OK";
    public override Stream? ContentStream { get; set; }
    public override string ClientRequestId { get; set; } = Guid.NewGuid().ToString();

    public override void Dispose()
    {
    }

    protected override bool TryGetHeader(string name, out string value)
    {
        value = string.Empty;
        return false;
    }

    protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values)
    {
        values = [];
        return false;
    }

    protected override IEnumerable<HttpHeader> EnumerateHeaders() => [];

    protected override bool ContainsHeader(string name)
    {
        throw new NotImplementedException();
    }
}