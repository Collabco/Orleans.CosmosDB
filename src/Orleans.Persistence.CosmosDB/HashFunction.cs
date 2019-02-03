using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.Persistence.CosmosDB
{
    public class HashFunction : IHashFunction
    {
        // Taken from here:
        // https://stackoverflow.com/a/9545731
        // Will do more research later
        public ulong CalculateHash(string input)
        {
            var hashedValue = 3074457345618258791ul;

            foreach (var c in input)
            {
                hashedValue += c;
                hashedValue *= 3074457345618258799ul;
            }

            return hashedValue;
        }
    }
}
