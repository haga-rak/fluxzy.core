// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Fluxzy.Misc;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Util
{
    public class CapabilityHelperTests
    {
        [Theory]
        [InlineData("2263: cap_chown,cap_dac_override,cap_fowner,cap_setfcap=ep\n", "cap_chown,cap_dac_override,cap_fowner,cap_setfcap=ep")]
        [InlineData("2263: cap_chown,cap_dac_override,cap_fowner,cap_setfcap=ep", "cap_chown,cap_dac_override,cap_fowner,cap_setfcap=ep")]
        [InlineData("2263: =ep", "=ep")]
        [InlineData("2263: cap_chown,cap_daC_override,cap_fowner,cap_setfcap=ep\n", "cap_choWn,cap_dac_override,cap_fowner,cap_setfcap=ep")]
        [InlineData("2263: cap_chown,cap_chown,cap_chown,", "cap_chown")]
        [InlineData("2263: ", "")]
        [InlineData(": cap_chown,cap_daC_override,cap_fowner,cap_setfcap=ep\n", null)]
        [InlineData("", null)]
        public void TryParseOutput(string outputMessage, string? expected)
        {
            var success = expected != null; 
            var expectedHashSet = expected == null ? null : new HashSet<string>(expected.Split(',', StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase);
            var pid = 2263;
            
            var result = CapabilityHelper.TryParseOutput(pid, outputMessage, out var resultHashSet);
            
            Assert.Equal(success, result);
            
            if (success) {
                Assert.NotNull(resultHashSet);    
                Assert.NotNull(expectedHashSet);    
                
                Assert.Equal(expectedHashSet, resultHashSet, (a, b) => a!.SetEquals(b!));
            }
            else {
                Assert.Null(resultHashSet);
            }
        }

        [Fact]
        public async Task GetCapabilities()
        {
            var processId = Process.GetCurrentProcess().Id; 
            
            var capabilities = await CapabilityHelper.GetCapabilities(processId);

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                Assert.Null(capabilities);
            }
            else {
                Assert.NotNull(capabilities);
                Assert.NotEmpty(capabilities);
            }
        }
    }
}
