using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net.Http.Headers;
using System.Runtime.Caching;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using JonathanWalton720.TokenValidatorTests;

namespace JonathanWalton720.TokenValidator
{
    [TestClass]
    public class TokenValidatorTests
    {
        private readonly string authenticationToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJ1bmlxdWVfbmFtZSI6InVzZXIiLCJwcmltYXJ5c2lkIjoiMSIsImh0dHBzOi8vd3d3LmdpdGh1Yi5jb20vam9uYXRoYW53YWx0b243MjAvYWNjZXNzX291dF90aW1lIjoiMS8xLzAwMDEgMTI6MDA6MDAgQU0iLCJpc3MiOiJodHRwczovL3d3dy5saW5rZWRpbi5jb20vaW4vam9uYXRoYW53YWx0b243MjAvIiwiYXVkIjoiZEdOdmNtVlRVMDlCZFhSb1UyVnlkbVZ5IiwiZXhwIjoxNjI5ODg2MjkyLCJuYmYiOjE2Mjk4NDMwOTJ9.v5sSHEnoc04ll6bhpGTQ_2cosULucNmmL2yXNXPdEFI";

        [TestMethod]
        public void Constructor_LogIsNull_DoesNotThrowArgumentNullException()
        {
            var result = new TokenValidator("http://some-endpoint/url/togoto", null);

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Constructor_AuthorizationServerEndpointIsEmpty_PropertiesAreNotNull()
        {
            var log = new FakeTokenLogger();

            Assert.Throws<ArgumentException>(() => new TokenValidator("", log));
        }

        [TestMethod]
        public void Constructor_WithValidArguments_IsNotNull()
        {
            var log = new FakeTokenLogger();
            var validator = new TokenValidator("http://some-endpoint/url/togoto", log);

            Assert.IsNotNull(validator);
        }

        [TestMethod]
        public void Validate_AuthenticationHeader_NoError()
        {
            var authenticationHeaderValue = new AuthenticationHeaderValue("Bearer", authenticationToken);
            var TokenValidator = CreateTokenValidator();

            var isValid = true;

            try
            {
                TokenValidator.Validate(authenticationHeaderValue);
            }
            catch (Exception)
            {
                isValid = false;
            }

            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void Validate_AuthenticationHeaderValueIsNull_ThrowsArgumentNullException()
        {
            var validator = CreateTokenValidator();

            Assert.Throws<ArgumentNullException>(() => validator.Validate(null));
        }

        [TestMethod]
        public void Validate_AuthenticationHeaderNotBearer_ThrowsArgumentException()
        {
            var authenticationHeaderValue = new AuthenticationHeaderValue("NotBearer", authenticationToken);
            var validator = CreateTokenValidator();

            Assert.Throws<ArgumentException>(() =>
            {
                try
                {
                    validator.Validate(authenticationHeaderValue);
                }
                catch (Exception ex)
                {
                    Assert.AreEqual("must use bearer authentication", ex.Message);
                    throw;
                }
            });
        }

        [TestMethod]
        public void Validate_AuthenticationHeaderNoBearerValue_ThrowsTokenException()
        {
            var authenticationHeaderValue = new AuthenticationHeaderValue("Bearer");
            var validator = CreateTokenValidator();

            Assert.Throws<ArgumentException>(() =>
             {
                 try
                 {
                     validator.Validate(authenticationHeaderValue);
                 }
                 catch (Exception ex)
                 {
                     Assert.AreEqual("bearer authentication is missing", ex.Message);
                     throw;
                 }
             });
        }

        [TestMethod]
        public void Validate_CachedAuthenticationIsTrue_NoError()
        {
            var authenticationHeaderValue = new AuthenticationHeaderValue("Bearer", authenticationToken);

            var validator = CreateTokenValidator();

            TokenValidator.Cache[authenticationToken] = true;
            validator.Validate(authenticationHeaderValue);

            Assert.IsTrue(true);
        }

        [TestMethod]
        public void Validate_CachedAuthenticationIsFalse_ThrowsTokenException()
        {
            var authenticationHeaderValue = new AuthenticationHeaderValue("Bearer", authenticationToken);

            var validator = CreateTokenValidator();
            Assert.Throws<TokenException>(() =>
             {
                 try
                 {
                     TokenValidator.Cache[authenticationToken] = false;
                     validator.Validate(authenticationHeaderValue);
                 }
                 catch (Exception ex)
                 {
                     Assert.AreEqual("you are not authorized", ex.Message);
                     throw;
                 }
             });
        }

        [TestMethod]
        [Ignore("This test is inconsistent due to async method calls")]
        public async Task Validate_MultipleUnauthorizedRequests_ThrowsErrors()
        {
            var numberOfRequests = 5;
            var requests = new List<AuthenticationHeaderValue>();
            for (int i = 0; i < numberOfRequests; i++)
            {
                requests.Add(new AuthenticationHeaderValue("Bearer", "some unique key value"));
            }

            var handler = new FakeMessageHandler(HttpStatusCode.BadRequest);
            var validator = CreateTokenValidator(handler);
            var tasks = new List<Task>();
            var errors = new List<Exception>();
            foreach (var item in requests)
            {
                var task = Task.Run(() =>
                {
                    try
                    {
                        validator.Validate(item);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                    }
                });
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
            Thread.Sleep(100); // NOTE: sometimes the last exception when all tasks are done is still being run
            Assert.AreEqual(numberOfRequests, errors.Count);
        }

        private static TokenValidator CreateTokenValidator(HttpMessageHandler httpMessageHandler = null)
        {
            httpMessageHandler = httpMessageHandler ?? new FakeMessageHandler(HttpStatusCode.OK);
            var logger = new FakeTokenLogger();
            TokenValidator.Cache = new MemoryCache("testing"); // should use new cache for each test
            return new TokenValidator("http://some-endpoint/url/togoto", logger, httpMessageHandler);
        }
    }
}