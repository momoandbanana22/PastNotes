namespace PastNotes;

public class ApiException : Exception
{
    public ApiException(string message) : base(message)
    {
    }

    public ApiException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class NotFoundException : ApiException
{
    public NotFoundException(string message) : base(message)
    {
    }
}

public class RateLimitExceededException : ApiException
{
    public RateLimitExceededException(string message) : base(message)
    {
    }
}

public class UnauthorizedException : ApiException
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}

public class ServerErrorException : ApiException
{
    public ServerErrorException(string message) : base(message)
    {
    }
}
