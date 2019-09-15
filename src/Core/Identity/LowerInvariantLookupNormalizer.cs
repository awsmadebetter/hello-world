﻿using Microsoft.AspNetCore.Identity;

namespace Bit.Core.Identity
{
    public class LowerInvariantLookupNormalizer : ILookupNormalizer
    {
        public string Normalize(string key)
        {
            return key?.Normalize().ToLowerInvariant();
        }
    }
}
