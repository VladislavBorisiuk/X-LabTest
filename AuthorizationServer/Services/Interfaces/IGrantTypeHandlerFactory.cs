namespace AuthorizationServer.Services.Interfaces
{
    public interface IGrantTypeHandlerFactory
    {
        IGrantTypeHandler GetHandler(string grantType);
    }

}
