using System;
using System.Collections.Generic;
using Fluxzy.Cli;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Container
{
    public class ContainerEnvironmentHelperTests
    {
        [Theory]
        [InlineData("FLUXZY_CONTAINERIZED", "1", true)]
        [InlineData("FLUXZY_CONTAINERIZED", "true", true)]
        [InlineData("FLUXZY_CONTAINERIZED", "", false)]
        [InlineData("DUMMY_VAR", "", false)]
        public void IsInContainer(string envName, string envValue, bool expected)
        {
            // Arrange
            var dictionary = new Dictionary<string, string>() {
                [envName] = envValue
            };

            var environmentProvider = new DictionaryEnvironmentProvider(dictionary);

            // Act
            var result = ContainerEnvironmentHelper.IsInContainer(environmentProvider);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void CreateArgsFromEnvironment_DefaultValues()
        {
            // Arrange
            var dictionary = new Dictionary<string, string>(); 
            var environmentProvider = new DictionaryEnvironmentProvider(dictionary);

            // Act
            var result = ContainerEnvironmentHelper.CreateArgsFromEnvironment(new[] { "start" }, 
                environmentProvider);

            var fullArgs = string.Join(" ", result); 

            // Assert
            Assert.NotNull(result);
            Assert.Equal("start --listen-interface 0.0.0.0/44344", fullArgs);
        }

        [Theory]
        [InlineData("FLUXZY_ENABLE_DUMP_FOLDER", "true", "--dump-folder /var/fluxzy/dump")]
        [InlineData("FLUXZY_ENABLE_DUMP_FOLDER", "1", "--dump-folder /var/fluxzy/dump")]
        [InlineData("FLUXZY_ENABLE_OUTPUT_FILE", "1", "--output-file /var/fluxzy/out.fxzy")]
        [InlineData("FLUXZY_ENABLE_PCAP", "1", "--include-dump")]
        [InlineData("FLUXZY_USE_BOUNCY_CASTLE", "1", "--bouncy-castle")]
        [InlineData("FLUXZY_CUSTOM_CA_PATH", "path", "--cert-file path")]
        [InlineData("FLUXZY_CUSTOM_CA_PASSWORD", "password", "--cert-password password")]
        [InlineData("FLUXZY_MODE", "ReverseSecure", "--mode ReverseSecure")]
        [InlineData("FLUXZY_MODE_REVERSE_PORT", "8080", "--mode-reverse-port 8080")]
        [InlineData("FLUXZY_EXTRA_ARGS", "--extra-args value1 --extra-args value2 --nice-option", "--extra-args value1 --extra-args value2 --nice-option")]
        public void CreateArgsFromEnvironment(string envName, string envValue, string expectArgs)
        {
            // Arrange
            var dictionary = new Dictionary<string, string>() {
                [envName] = envValue
            }; 
            var environmentProvider = new DictionaryEnvironmentProvider(dictionary);

            // Act
            var result = ContainerEnvironmentHelper.CreateArgsFromEnvironment(new[] { "start" }, 
                environmentProvider);

            var fullArgs = string.Join(" ", result); 

            // Assert
            Assert.NotNull(result);
            Assert.Contains(expectArgs, fullArgs);
        }
    }
}
