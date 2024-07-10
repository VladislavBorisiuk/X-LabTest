using System;
using System.Collections.Generic;
using System.Threading;
using XLabApp.Models;

namespace XLabApp.Services.Interfaces
{
    internal interface IDataService
    {
        public TokenResponse CurrentToken { get; set; }
        public async Task<List<PersonDTO>> GetUsersAsync(string? ODataCode) { throw new NotImplementedException(); }

        public async Task<string> RegisterUserAsync(PersonDTO model) {  throw new NotImplementedException(); }

        public async Task<TokenResponse> AuthorizeUserAsync(PersonDTO model) { throw new NotImplementedException(); }

        public async Task<TokenResponse> RefreshAccessTokenAsync(string refreshToken) { throw new NotImplementedException(); }

        private async Task EnsureTokenAsync() { throw new NotImplementedException(); }
    }
}
