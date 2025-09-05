namespace Mammon.Models;

class MockResponse : Response
{
    private readonly int _status;
    public MockResponse(int status) => _status = status;

    public override int Status => _status;
    public override string ReasonPhrase => "OK";
    public override Stream? ContentStream { get; set; }
    public override string ClientRequestId { get; set; } = Guid.NewGuid().ToString();
    public override void Dispose() { }
    protected override bool TryGetHeader(string name, out string value) { value = string.Empty; return false; }
    protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values) { values = Array.Empty<string>(); return false; }
    protected override IEnumerable<HttpHeader> EnumerateHeaders() => Array.Empty<HttpHeader>();

    protected override bool ContainsHeader(string name)
    {
        throw new NotImplementedException();
    }
}

