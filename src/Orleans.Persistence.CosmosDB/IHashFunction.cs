namespace Orleans.Persistence.CosmosDB
{
    public interface IHashFunction
    {
        ulong CalculateHash(string input);
    }
}