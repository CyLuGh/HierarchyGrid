using System;

namespace HierarchyGrid.Definitions;

public class InvalidThemeColorCodeException : Exception
{
    public InvalidThemeColorCodeException() : base()
    {
    }

    public InvalidThemeColorCodeException( string? message ) : base( message )
    {
    }

    public InvalidThemeColorCodeException( string? message , Exception? innerException ) : base( message , innerException )
    {
    }
}
